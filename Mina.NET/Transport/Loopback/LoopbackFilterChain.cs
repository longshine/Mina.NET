using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Mina.Core.Buffer;
using Mina.Core.Filterchain;
using Mina.Core.Service;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Transport.Loopback
{
    class LoopbackFilterChain : VirtualDefaultIoFilterChain
    {
        private readonly ConcurrentQueue<IoEvent> _eventQueue = new ConcurrentQueue<IoEvent>();
        private readonly IoProcessor<LoopbackSession> _processor;
        private volatile Boolean _flushEnabled;
        private volatile Boolean sessionOpened;

        /// <summary>
        /// </summary>
        public LoopbackFilterChain(AbstractIoSession session)
            : base(session)
        {
            _processor = new LoopbackIoProcessor(this);
        }

        public void Start()
        {
            _flushEnabled = true;
            FlushEvents();
            FlushPendingDataQueues((LoopbackSession)Session);
        }

        internal IoProcessor<LoopbackSession> Processor
        {
            get { return _processor; }
        }

        private void PushEvent(IoEvent e)
        {
            PushEvent(e, _flushEnabled);
        }

        private void PushEvent(IoEvent e, Boolean flushNow)
        {
            _eventQueue.Enqueue(e);
            if (flushNow)
                FlushEvents();
        }

        private void FlushEvents()
        {
            IoEvent e;
            while (_eventQueue.TryDequeue(out e))
            {
                FireEvent(e);
            }
        }

        private void FireEvent(IoEvent e)
        {
            LoopbackSession session = (LoopbackSession)Session;
            Object data = e.Parameter;
            switch (e.EventType)
            {
                case IoEventType.MessageReceived:
                    if (sessionOpened && (!session.ReadSuspended) && Monitor.TryEnter(session.Lock))
                    {
                        try
                        {
                            if (session.ReadSuspended)
                            {
                                session.ReceivedMessageQueue.Enqueue(data);
                            }
                            else
                            {
                                base.FireMessageReceived(data);
                            }
                        }
                        finally
                        {
                            Monitor.Exit(session.Lock);
                        }
                    }
                    else
                    {
                        session.ReceivedMessageQueue.Enqueue(data);
                    }
                    break;
                case IoEventType.Write:
                    base.FireFilterWrite((IWriteRequest)data);
                    break;
                case IoEventType.MessageSent:
                    base.FireMessageSent((IWriteRequest)data);
                    break;
                case IoEventType.ExceptionCaught:
                    base.FireExceptionCaught((Exception)data);
                    break;
                case IoEventType.SessionCreated:
                    Monitor.Enter(session.Lock);
                    try
                    {
                        base.FireSessionCreated();
                    }
                    finally
                    {
                        Monitor.Exit(session.Lock);
                    }
                    break;
                case IoEventType.SessionOpened:
                    base.FireSessionOpened();
                    sessionOpened = true;
                    break;
                case IoEventType.SessionIdle:
                    base.FireSessionIdle((IdleStatus)data);
                    break;
                case IoEventType.SessionClosed:
                    FlushPendingDataQueues(session);
                    base.FireSessionClosed();
                    break;
                case IoEventType.Close:
                    base.FireFilterClose();
                    break;
                default:
                    break;
            }
        }

        private static void FlushPendingDataQueues(LoopbackSession s)
        {
            s.Processor.UpdateTrafficControl(s);
            s.RemoteSession.Processor.UpdateTrafficControl(s);
        }

        public override void FireSessionCreated()
        {
            PushEvent(new IoEvent(IoEventType.SessionCreated, Session, null));
        }

        public override void FireSessionOpened()
        {
            PushEvent(new IoEvent(IoEventType.SessionOpened, Session, null));
        }

        public override void FireSessionClosed()
        {
            PushEvent(new IoEvent(IoEventType.SessionClosed, Session, null));
        }

        public override void FireSessionIdle(IdleStatus status)
        {
            PushEvent(new IoEvent(IoEventType.SessionIdle, Session, status));
        }

        public override void FireMessageReceived(Object message)
        {
            PushEvent(new IoEvent(IoEventType.MessageReceived, Session, message));
        }

        public override void FireMessageSent(IWriteRequest request)
        {
            PushEvent(new IoEvent(IoEventType.MessageSent, Session, request));
        }

        public override void FireExceptionCaught(Exception cause)
        {
            PushEvent(new IoEvent(IoEventType.ExceptionCaught, Session, cause));
        }

        public override void FireFilterWrite(IWriteRequest writeRequest)
        {
            PushEvent(new IoEvent(IoEventType.Write, Session, writeRequest));
        }

        public override void FireFilterClose()
        {
            PushEvent(new IoEvent(IoEventType.Close, Session, null));
        }

        class LoopbackIoProcessor : IoProcessor<LoopbackSession>
        {
            private readonly LoopbackFilterChain _chain;

            public LoopbackIoProcessor(LoopbackFilterChain chain)
            {
                _chain = chain;
            }

            public void Add(LoopbackSession session)
            {
                // do nothing
            }

            public void Write(LoopbackSession session, IWriteRequest writeRequest)
            {
                session.WriteRequestQueue.Offer(session, writeRequest);

                if (!session.WriteSuspended)
                {
                    Flush(session);
                }
            }

            public void Flush(LoopbackSession session)
            {
                IWriteRequestQueue queue = session.WriteRequestQueue;
                if (!session.Closing)
                {
                    lock (session.Lock)
                    {
                        try
                        {
                            if (queue.IsEmpty(session))
                                return;

                            IWriteRequest req;
                            DateTime currentTime = DateTime.Now;
                            while ((req = queue.Poll(session)) != null)
                            {
                                Object m = req.Message;
                                _chain.PushEvent(new IoEvent(IoEventType.MessageSent, session, req), false);
                                session.RemoteSession.FilterChain.FireMessageReceived(GetMessageCopy(m));
                                IoBuffer buf = m as IoBuffer;
                                if (buf != null)
                                    session.IncreaseWrittenBytes(buf.Remaining, currentTime);
                            }
                        }
                        finally
                        {
                            if (_chain._flushEnabled)
                                _chain.FlushEvents();
                        }
                    }

                    FlushPendingDataQueues(session);
                }
                else
                {
                    List<IWriteRequest> failedRequests = new List<IWriteRequest>();
                    IWriteRequest req;
                    while ((req = queue.Poll(session)) != null)
                    {
                        failedRequests.Add(req);
                    }

                    if (failedRequests.Count > 0)
                    {
                        WriteToClosedSessionException cause = new WriteToClosedSessionException(failedRequests);
                        foreach (IWriteRequest r in failedRequests)
                        {
                            r.Future.Exception = cause;
                        }
                        session.FilterChain.FireExceptionCaught(cause);
                    }
                }
            }

            public void Remove(LoopbackSession session)
            {
                lock (session.Lock)
                {
                    if (!session.CloseFuture.Closed)
                    {
                        IoServiceSupport support = session.Service as IoServiceSupport;
                        if (support != null)
                            support.FireSessionDestroyed(session);
                        session.RemoteSession.Close(true);
                    }
                }
            }

            public void UpdateTrafficControl(LoopbackSession session)
            {
                if (!session.ReadSuspended)
                {
                    ConcurrentQueue<Object> queue = session.ReceivedMessageQueue;
                    Object item;
                    while (queue.TryDequeue(out item))
                    {
                        _chain.FireMessageReceived(item);
                    }
                }

                if (!session.WriteSuspended)
                {
                    Flush(session);
                }
            }

            private Object GetMessageCopy(Object message)
            {
                Object messageCopy = message;
                IoBuffer rb = message as IoBuffer;
                if (rb != null)
                {
                    rb.Mark();
                    IoBuffer wb = IoBuffer.Allocate(rb.Remaining);
                    wb.Put(rb);
                    wb.Flip();
                    rb.Reset();
                    messageCopy = wb;
                }
                return messageCopy;
            }

            void IoProcessor.Write(IoSession session, IWriteRequest writeRequest)
            {
                Write((LoopbackSession)session, writeRequest);
            }

            void IoProcessor.Flush(IoSession session)
            {
                Flush((LoopbackSession)session);
            }

            void IoProcessor.Add(IoSession session)
            {
                Add((LoopbackSession)session);
            }

            void IoProcessor.Remove(IoSession session)
            {
                Remove((LoopbackSession)session);
            }

            void IoProcessor.UpdateTrafficControl(IoSession session)
            {
                UpdateTrafficControl((LoopbackSession)session);
            }
        }
    }
}
