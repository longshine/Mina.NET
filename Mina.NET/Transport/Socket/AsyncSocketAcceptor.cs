using System;
using System.Net;
using System.Net.Sockets;
using Mina.Core.Service;
using Mina.Core.Buffer;
using System.Collections.Generic;
using Mina.Core.Write;
using Mina.Core.Session;
using System.Collections.Concurrent;
using Mina.Core.Filterchain;
using Mina.Util;
using System.Threading;

namespace Mina.Transport.Socket
{
    public class AsyncSocketAcceptor : AbstractIoAcceptor, ISocketAcceptor, IoProcessor<SocketSession>
    {
        const Int32 IdleCheckingInterval = 1000;

        private System.Net.Sockets.Socket _listenSocket;
        private Int32 _backlog = 100;
        private Int32 _maxConnections;

        private BufferManager _bufferManager;
        private Pool<SocketAsyncEventArgsBuffer> _readWritePool;
        private DateTime _lastIdleCheckTime;
        private Timer _idleTimer;

        private System.Collections.Concurrent.ConcurrentQueue<SocketSession> _newSessions = new System.Collections.Concurrent.ConcurrentQueue<SocketSession>();

        public AsyncSocketAcceptor()
            : this(1024)
        { }

        public AsyncSocketAcceptor(Int32 maxConnections)
            : base(new DefaultSocketSessionConfig())
        {
            _maxConnections = maxConnections;
            _idleTimer = new Timer(NotifyIdleSessions);
        }

        public override void Bind(EndPoint localEP)
        {
            InitBuffer();

            _listenSocket = new System.Net.Sockets.Socket(localEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _listenSocket.Bind(localEP);
            _listenSocket.Listen(_backlog);

            StartAccept(null);

            _idleTimer.Change(0, IdleCheckingInterval);
        }

        public override void Unbind()
        {
            _idleTimer.Change(Timeout.Infinite, Timeout.Infinite);

            _listenSocket.Close();
            _listenSocket = null;
        }

        private void InitBuffer()
        {
            Int32 bufferSize = SessionConfig.ReadBufferSize;
            if (_bufferManager == null || _bufferManager.BufferSize != bufferSize)
            {
                _bufferManager = new BufferManager(bufferSize * _maxConnections, bufferSize);
                _bufferManager.InitBuffer();

                var list = new List<SocketAsyncEventArgsBuffer>(_maxConnections);
                for (Int32 i = 0; i < _maxConnections; i++)
                {
                    SocketAsyncEventArgs readWriteEventArg = new SocketAsyncEventArgs();
                    _bufferManager.SetBuffer(readWriteEventArg);
                    SocketAsyncEventArgsBuffer buf = new SocketAsyncEventArgsBuffer(readWriteEventArg);
                    list.Add(buf);
                }
                _readWritePool = new Pool<SocketAsyncEventArgsBuffer>(list);
            }
        }

        private void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            if (acceptEventArg == null)
            {
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);
            }
            else
            {
                // socket must be cleared since the context object is being reused
                acceptEventArg.AcceptSocket = null;
            }
            
            bool willRaiseEvent = _listenSocket.AcceptAsync(acceptEventArg);
            if (!willRaiseEvent)
            {
                ProcessAccept(acceptEventArg);
            }
        }

        private void AcceptEventArg_Completed(Object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                SocketAsyncEventArgsBuffer readBuffer = _readWritePool.Pop();
                SocketSession session = new SocketSession(this, this, e.AcceptSocket, readBuffer);

                InitSession(session, null, null);
                session.Processor.Add(session);

                // Accept the next connection request
                StartAccept(e);
            }
            else if (e.SocketError != SocketError.OperationAborted
                && e.SocketError != SocketError.Interrupted)
            {
                ExceptionMonitor.Instance.ExceptionCaught(new SocketException((Int32)e.SocketError));
            }
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

        private void NotifyIdleSessions(Object state)
        {
            AbstractIoSession.NotifyIdleness(ManagedSessions.Values, DateTime.Now);
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
