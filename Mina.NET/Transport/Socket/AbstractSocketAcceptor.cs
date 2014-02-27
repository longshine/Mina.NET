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
    public abstract class AbstractSocketAcceptor : AbstractIoAcceptor, ISocketAcceptor
    {
        private readonly IoProcessor<SocketSession> _processor = new AsyncSocketProcessor();
        private Int32 _backlog;
        private Int32 _maxConnections;
        private IdleStatusChecker _idleStatusChecker;
        private Semaphore _connectionPool;
#if NET20
        private readonly WaitCallback _startAccept;
#else
        private readonly Action<Object> _startAccept;
#endif
        private Boolean _disposed;
        private readonly Dictionary<EndPoint, System.Net.Sockets.Socket> _listenSockets = new Dictionary<EndPoint, System.Net.Sockets.Socket>();

        protected AbstractSocketAcceptor()
            : this(1024)
        { }

        protected AbstractSocketAcceptor(Int32 maxConnections)
            : base(new DefaultSocketSessionConfig())
        {
            _maxConnections = maxConnections;
            _idleStatusChecker = new IdleStatusChecker(() => ManagedSessions.Values);
            this.SessionDestroyed += OnSessionDestroyed;
            _startAccept = StartAccept0;
        }

        public Boolean ReuseAddress { get; set; }

        public Int32 Backlog
        {
            get { return _backlog; }
            set { _backlog = value; }
        }

        public Int32 MaxConnections
        {
            get { return _maxConnections; }
            set { _maxConnections = value; }
        }

        protected override IEnumerable<EndPoint> BindInternal(IEnumerable<EndPoint> localEndPoints)
        {
            Dictionary<EndPoint, System.Net.Sockets.Socket> newListeners = new Dictionary<EndPoint, System.Net.Sockets.Socket>();
            try
            {
                // Process all the addresses
                foreach (EndPoint localEP in localEndPoints)
                {
                    System.Net.Sockets.Socket listenSocket = new System.Net.Sockets.Socket(localEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    listenSocket.Bind(localEP);
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

            _idleStatusChecker.Start();

            return newListeners.Keys;
        }

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
                _idleStatusChecker.Stop();

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
            _connectionPool.WaitOne();
            BeginAccept((ListenerContext)state);
        }

        private void OnSessionDestroyed(Object sender, IoSessionEventArgs e)
        {
            if (_connectionPool != null)
                _connectionPool.Release();
        }

        protected abstract IoSession NewSession(IoProcessor<SocketSession> processor, System.Net.Sockets.Socket socket);

        protected abstract void BeginAccept(ListenerContext listener);

        protected void EndAccept(System.Net.Sockets.Socket socket, ListenerContext listener)
        {
            if (socket != null)
            {
                IoSession session = NewSession(_processor, socket);
                try
                {
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
                    _idleStatusChecker.Dispose();
                    base.Dispose(disposing);
                    _disposed = true;
                }
            }
        }

        protected class ListenerContext
        {
            private readonly System.Net.Sockets.Socket _socket;

            public ListenerContext(System.Net.Sockets.Socket socket)
            {
                _socket = socket;
            }

            public System.Net.Sockets.Socket Socket
            {
                get { return _socket; }
            }

            public Object Tag { get; set; }
        }
    }
}
