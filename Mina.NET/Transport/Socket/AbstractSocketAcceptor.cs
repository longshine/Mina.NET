using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Mina.Core.Future;
using Mina.Core.Service;
using Mina.Core.Session;
using Mina.Util;

namespace Mina.Transport.Socket
{
    /// <summary>
    /// Base class of socket acceptor.
    /// </summary>
    public abstract class AbstractSocketAcceptor : AbstractIoAcceptor, ISocketAcceptor
    {
        private readonly AsyncSocketProcessor _processor;
        private Int32 _backlog;
        private Int32 _maxConnections;
        private Semaphore _connectionPool;
#if NET20
        private readonly WaitCallback _startAccept;
#else
        private readonly Action<Object> _startAccept;
#endif
        private Boolean _disposed;
        private readonly Dictionary<EndPoint, System.Net.Sockets.Socket> _listenSockets = new Dictionary<EndPoint, System.Net.Sockets.Socket>();

        /// <summary>
        /// Instantiates with default max connections of 1024.
        /// </summary>
        protected AbstractSocketAcceptor()
            : this(1024)
        { }

        /// <summary>
        /// Instantiates.
        /// </summary>
        /// <param name="maxConnections">the max connections allowed</param>
        protected AbstractSocketAcceptor(Int32 maxConnections)
            : base(new DefaultSocketSessionConfig())
        {
            _maxConnections = maxConnections;
            _processor = new AsyncSocketProcessor(() => ManagedSessions.Values);
            this.SessionDestroyed += OnSessionDestroyed;
            _startAccept = StartAccept0;
            ReuseBuffer = true;
        }

        /// <inheritdoc/>
        public new ISocketSessionConfig SessionConfig
        {
            get { return (ISocketSessionConfig)base.SessionConfig; }
        }

        /// <inheritdoc/>
        public new IPEndPoint LocalEndPoint
        {
            get { return (IPEndPoint)base.LocalEndPoint; }
        }

        /// <inheritdoc/>
        public new IPEndPoint DefaultLocalEndPoint
        {
            get { return (IPEndPoint)base.DefaultLocalEndPoint; }
            set { base.DefaultLocalEndPoint = value; }
        }

        /// <inheritdoc/>
        public override ITransportMetadata TransportMetadata
        {
            get { return AsyncSocketSession.Metadata; }
        }

        /// <inheritdoc/>
        public Boolean ReuseAddress { get; set; }

        /// <inheritdoc/>
        public Int32 Backlog
        {
            get { return _backlog; }
            set
            {
                lock (_bindLock)
                {
                    if (Active)
                        throw new InvalidOperationException("Backlog can't be set while the acceptor is bound.");
                    _backlog = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the number of max connections.
        /// </summary>
        public Int32 MaxConnections
        {
            get { return _maxConnections; }
            set
            {
                lock (_bindLock)
                {
                    if (Active)
                        throw new InvalidOperationException("MaxConnections can't be set while the acceptor is bound.");
                    _maxConnections = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to reuse the read buffer
        /// sent to <see cref="SocketSession.FilterChain"/> by
        /// <see cref="Core.Filterchain.IoFilterChain.FireMessageReceived(Object)"/>.
        /// </summary>
        /// <remarks>
        /// If any thread model, i.e. an <see cref="Filter.Executor.ExecutorFilter"/>,
        /// is added before filters that process the incoming <see cref="Core.Buffer.IoBuffer"/>
        /// in <see cref="Core.Filterchain.IoFilter.MessageReceived(Core.Filterchain.INextFilter, IoSession, Object)"/>,
        /// this must be set to <code>false</code> to avoid undetermined state
        /// of the read buffer. The default value is <code>true</code>.
        /// </remarks>
        public Boolean ReuseBuffer { get; set; }

        /// <inheritdoc/>
        protected override IEnumerable<EndPoint> BindInternal(IEnumerable<EndPoint> localEndPoints)
        {
            Dictionary<EndPoint, System.Net.Sockets.Socket> newListeners = new Dictionary<EndPoint, System.Net.Sockets.Socket>();
            try
            {
                // Process all the addresses
                foreach (EndPoint localEP in localEndPoints)
                {
                    EndPoint ep = localEP;
                    if (ep == null)
                        ep = new IPEndPoint(IPAddress.Any, 0);
                    System.Net.Sockets.Socket listenSocket = new System.Net.Sockets.Socket(ep.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    listenSocket.Bind(ep);
                    listenSocket.Listen(Backlog);
                    newListeners[listenSocket.LocalEndPoint] = listenSocket;
                }
            }
            catch (Exception)
            {
                // Roll back if failed to bind all addresses
                foreach (System.Net.Sockets.Socket listenSocket in newListeners.Values)
                {
                    try
                    {
                        listenSocket.Close();
                    }
                    catch (Exception ex)
                    {
                        ExceptionMonitor.Instance.ExceptionCaught(ex);
                    }
                }

                throw;
            }

            if (MaxConnections > 0)
                _connectionPool = new Semaphore(MaxConnections, MaxConnections);

            foreach (KeyValuePair<EndPoint, System.Net.Sockets.Socket> pair in newListeners)
            {
                _listenSockets[pair.Key] = pair.Value;
                StartAccept(new ListenerContext(pair.Value));
            }

            _processor.IdleStatusChecker.Start();

            return newListeners.Keys;
        }

        /// <inheritdoc/>
        protected override void UnbindInternal(IEnumerable<EndPoint> localEndPoints)
        {
            foreach (EndPoint ep in localEndPoints)
            {
                System.Net.Sockets.Socket listenSocket;
                if (!_listenSockets.TryGetValue(ep, out listenSocket))
                    continue;
                listenSocket.Close();
                _listenSockets.Remove(ep);
            }

            if (_listenSockets.Count == 0)
            {
                _processor.IdleStatusChecker.Stop();

                if (_connectionPool != null)
                {
                    _connectionPool.Close();
                    _connectionPool = null;
                }
            }
        }

        private void StartAccept(ListenerContext listener)
        {
            if (_connectionPool == null)
            {
                BeginAccept(listener);
            }
            else
            {
#if NET20
                System.Threading.ThreadPool.QueueUserWorkItem(_startAccept, listener);
#else
                System.Threading.Tasks.Task.Factory.StartNew(_startAccept, listener);
#endif
            }
        }

        private void StartAccept0(Object state)
        {
            Semaphore pool = _connectionPool;
            if (pool == null)
                // this might happen if has been unbound
                return;
            try
            {
                pool.WaitOne();
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            BeginAccept((ListenerContext)state);
        }

        private void OnSessionDestroyed(Object sender, IoSessionEventArgs e)
        {
            Semaphore pool = _connectionPool;
            if (pool != null)
                pool.Release();
        }

        /// <inheritdoc/>
        protected abstract IoSession NewSession(IoProcessor<SocketSession> processor, System.Net.Sockets.Socket socket);

        /// <summary>
        /// Begins an accept operation.
        /// </summary>
        /// <param name="listener"></param>
        protected abstract void BeginAccept(ListenerContext listener);

        /// <summary>
        /// Ends an accept operation.
        /// </summary>
        /// <param name="socket">the accepted client socket</param>
        /// <param name="listener">the <see cref="ListenerContext"/></param>
        protected void EndAccept(System.Net.Sockets.Socket socket, ListenerContext listener)
        {
            if (socket != null)
            {
                try
                {
                    IoSession session = NewSession(_processor, socket);
                    InitSession<IoFuture>(session, null, null);
                    session.Processor.Add(session);
                }
                catch (Exception ex)
                {
                    ExceptionMonitor.Instance.ExceptionCaught(ex);
                }
            }

            // Accept the next connection request
            StartAccept(listener);
        }

        /// <inheritdoc/>
        protected override void Dispose(Boolean disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_listenSockets.Count > 0)
                    {
                        foreach (System.Net.Sockets.Socket listenSocket in _listenSockets.Values)
                        {
                            ((IDisposable)listenSocket).Dispose();
                        }
                    }
                    if (_connectionPool != null)
                    {
                        ((IDisposable)_connectionPool).Dispose();
                        _connectionPool = null;
                    }
                    _processor.Dispose();
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Provides context info for a socket acceptor.
        /// </summary>
        protected class ListenerContext
        {
            private readonly System.Net.Sockets.Socket _socket;

            /// <summary>
            /// Instantiates.
            /// </summary>
            /// <param name="socket">the associated socket</param>
            public ListenerContext(System.Net.Sockets.Socket socket)
            {
                _socket = socket;
            }

            /// <summary>
            /// Gets the associated socket.
            /// </summary>
            public System.Net.Sockets.Socket Socket
            {
                get { return _socket; }
            }

            /// <summary>
            /// Gets or sets a tag.
            /// </summary>
            public Object Tag { get; set; }
        }
    }
}
