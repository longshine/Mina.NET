using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Mina.Core.Buffer;
using Mina.Core.Filterchain;
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
        protected System.Net.Sockets.Socket _listenSocket;

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

        public override void Bind(EndPoint localEP)
        {
            _listenSocket = new System.Net.Sockets.Socket(localEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _listenSocket.Bind(localEP);
            _listenSocket.Listen(Backlog);

            if (MaxConnections > 0)
                _connectionPool = new Semaphore(MaxConnections, MaxConnections);

            StartAccept(null);

            _idleStatusChecker.Start();
        }

        public override void Unbind()
        {
            _idleStatusChecker.Stop();

            if (_listenSocket != null)
            {
                _listenSocket.Close();
                _listenSocket = null;
            }

            if (_connectionPool != null)
            {
                _connectionPool.Close();
                _connectionPool = null;
            }
        }

        private void StartAccept(Object state)
        {
            if (_connectionPool == null)
            {
                BeginAccept(state);
            }
            else
            {
#if NET20
                System.Threading.ThreadPool.QueueUserWorkItem(_startAccept, state);
#else
                System.Threading.Tasks.Task.Factory.StartNew(_startAccept, state);
#endif
            }
        }

        private void StartAccept0(Object state)
        {
            _connectionPool.WaitOne();
            BeginAccept(state);
        }

        private void OnSessionDestroyed(IoSession session)
        {
            if (_connectionPool != null)
                _connectionPool.Release();
        }

        protected abstract void BeginAccept(Object state);

        protected void EndAccept(IoSession session, Object state)
        {
            if (session != null)
            {
                try
                {
                    InitSession(session, null, null);
                    session.Processor.Add(session);
                }
                catch (Exception ex)
                {
                    ExceptionMonitor.Instance.ExceptionCaught(ex);
                }
            }

            // Accept the next connection request
            StartAccept(state);
        }

        protected override void Dispose(Boolean disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_listenSocket != null)
                    {
                        ((IDisposable)_listenSocket).Dispose();
                        _listenSocket = null;
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
    }
}
