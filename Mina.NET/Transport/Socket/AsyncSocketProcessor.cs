using System;
using System.Collections.Generic;
using Mina.Core.Buffer;
using Mina.Core.Filterchain;
using Mina.Core.Service;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Transport.Socket
{
    class AsyncSocketProcessor : IoProcessor<SocketSession>, IDisposable
    {
        private readonly IdleStatusChecker _idleStatusChecker;

        public AsyncSocketProcessor(Func<IEnumerable<IoSession>> getSessionsFunc)
        {
            _idleStatusChecker = new IdleStatusChecker(getSessionsFunc);
        }

        public IdleStatusChecker IdleStatusChecker
        {
            get { return _idleStatusChecker; }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(Boolean disposing)
        {
            if (disposing)
            {
                _idleStatusChecker.Dispose();
            }
        }

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

            if (session.Socket.Connected)
            {
                try
                {
                    session.Socket.Shutdown(System.Net.Sockets.SocketShutdown.Send);
                }
                catch { /* the session has already closed */ }
            }
            session.Socket.Close();

            IoServiceSupport support = session.Service as IoServiceSupport;
            if (support != null)
            {
                try
                {
                    support.FireSessionDestroyed(session);
                }
                catch (Exception e)
                {
                    // The session was either destroyed or not at this point.
                    // We do not want any exception thrown from this "cleanup" code.
                    session.FilterChain.FireExceptionCaught(e);
                }
            }
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
            session.Flush();
        }

        public void UpdateTrafficControl(SocketSession session)
        {
            if (!session.ReadSuspended)
                session.Start();

            if (!session.WriteSuspended)
                Flush(session);
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

        void IoProcessor.UpdateTrafficControl(IoSession session)
        {
            UpdateTrafficControl((SocketSession)session);
        }
    }
}
