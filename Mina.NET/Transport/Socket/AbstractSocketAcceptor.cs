using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Mina.Core.Buffer;
using Mina.Core.Filterchain;
using Mina.Core.Future;
using Mina.Core.Service;
using Mina.Core.Session;
using Mina.Core.Write;
using Mina.Util;

namespace Mina.Transport.Socket
{
    public abstract class AbstractSocketAcceptor : AbstractIoAcceptor, ISocketAcceptor, IoProcessor<SocketSession>
    {
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

        public AbstractSocketAcceptor()
            : this(1024)
        { }

        public AbstractSocketAcceptor(Int32 maxConnections)
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
            Exception exception = null;
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
            catch (Exception ex)
            {
                exception = ex;
            }
            finally
            {
                // Roll back if failed to bind all addresses
                if (exception != null)
                {
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

                    throw exception;
                }
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

        private void OnSessionDestroyed(IoSession session)
        {
            if (_connectionPool != null)
                _connectionPool.Release();
        }

        protected abstract void BeginAccept(ListenerContext listener);

        protected void EndAccept(IoSession session, ListenerContext listener)
        {
            if (session != null)
            {
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
                    base.Dispose(disposing);
                    _disposed = true;
                }
            }
        }

        #region IoProcessor

        public void Add(SocketSession session)
        {
            // Build the filter chain of this session.
            IoFilterChainBuilder chainBuilder = session.Service.FilterChainBuilder;
            chainBuilder.BuildFilterChain(session.FilterChain);

            // Propagate the SESSION_CREATED event up to the chain
            IoServiceSupport serviceSupport = session.Service as IoServiceSupport;
            if (serviceSupport != null)
                serviceSupport.FireSessionCreated(session);

            session.Start();
        }

        public void Remove(SocketSession session)
        {
            ClearWriteRequestQueue(session);
            session.Socket.Close();
            IoServiceSupport support = session.Service as IoServiceSupport;
            if (support != null)
                support.FireSessionDestroyed(session);
        }

        public void Write(SocketSession session, IWriteRequest writeRequest)
        {
            IWriteRequestQueue writeRequestQueue = session.WriteRequestQueue;
            writeRequestQueue.Offer(session, writeRequest);
            if (!session.WriteSuspended)
                Flush(session);
        }

        public void Flush(SocketSession session)
        {
            // TODO send data
            session.Flush();
        }

        private void ClearWriteRequestQueue(SocketSession session)
        {
            IWriteRequestQueue writeRequestQueue = session.WriteRequestQueue;
            IWriteRequest req;
            List<IWriteRequest> failedRequests = new List<IWriteRequest>();

            if ((req = writeRequestQueue.Poll(session)) != null)
            {
                IoBuffer buf = req.Message as IoBuffer;
                if (buf != null)
                {
                    // The first unwritten empty buffer must be
                    // forwarded to the filter chain.
                    if (buf.HasRemaining)
                    {
                        buf.Reset();
                        failedRequests.Add(req);
                    }
                    else
                    {
                        session.FilterChain.FireMessageSent(req);
                    }
                }
                else
                {
                    failedRequests.Add(req);
                }

                // Discard others.
                while ((req = writeRequestQueue.Poll(session)) != null)
                {
                    failedRequests.Add(req);
                }
            }

            // Create an exception and notify.
            if (failedRequests.Count > 0)
            {
                WriteToClosedSessionException cause = new WriteToClosedSessionException(failedRequests);

                foreach (IWriteRequest r in failedRequests)
                {
                    //session.DecreaseScheduledBytesAndMessages(r);
                    r.Future.Exception = cause;
                }

                session.FilterChain.FireExceptionCaught(cause);
            }
        }

        void IoProcessor.Write(IoSession session, IWriteRequest writeRequest)
        {
            Write((SocketSession)session, writeRequest);
        }

        void IoProcessor.Flush(IoSession session)
        {
            Flush((SocketSession)session);
        }

        void IoProcessor.Add(IoSession session)
        {
            Add((SocketSession)session);
        }

        void IoProcessor.Remove(IoSession session)
        {
            Remove((SocketSession)session);
        }

        #endregion

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
