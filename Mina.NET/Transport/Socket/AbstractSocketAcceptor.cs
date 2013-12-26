using System;
using System.Collections.Generic;
using Mina.Core.Buffer;
using Mina.Core.Filterchain;
using Mina.Core.Service;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Transport.Socket
{
    public abstract class AbstractSocketAcceptor : AbstractIoAcceptor, ISocketAcceptor, IoProcessor<SocketSession>
    {
        private Int32 _backlog = 100;
        private Int32 _maxConnections;
        protected IdleStatusChecker _idleStatusChecker;

        public AbstractSocketAcceptor()
            : this(1024)
        { }

        public AbstractSocketAcceptor(Int32 maxConnections)
            : base(new DefaultSocketSessionConfig())
        {
            _maxConnections = maxConnections;
            _idleStatusChecker = new IdleStatusChecker(() => ManagedSessions.Values);
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
