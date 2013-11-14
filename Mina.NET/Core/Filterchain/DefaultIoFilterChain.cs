using System;
using Common.Logging;
using Mina.Core.Buffer;
using Mina.Core.Future;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Core.Filterchain
{
    /// <summary>
    /// A default implementation of <see cref="IoFilterChain"/> that provides
    /// all operations for developers who want to implement their own
    /// transport layer once used with <see cref="AbstractIoSession"/>.
    /// </summary>
    public class DefaultIoFilterChain : Chain<DefaultIoFilterChain, IoFilter, INextFilter>, IoFilterChain
    {
        public readonly static AttributeKey SessionCreatedFuture = new AttributeKey(typeof(DefaultIoFilterChain), "connectFuture");
        private readonly static ILog log = LogManager.GetLogger(typeof(DefaultIoFilterChain));

        private readonly AbstractIoSession _session;

        public DefaultIoFilterChain(AbstractIoSession session)
            : base(
            e => new NextFilter(e),
            () => new HeadFilter(), () => new TailFilter()
            )
        {
            if (session == null)
                throw new ArgumentNullException("session");
            _session = session;
        }

        public IoSession Session
        {
            get { return _session; }
        }

        public void FireSessionCreated()
        {
            CallNextSessionCreated(_head, _session);
        }

        public void FireSessionOpened()
        {
            CallNextSessionOpened(_head, _session);
        }

        public void FireSessionClosed()
        {
            // update future
            try
            {
                _session.CloseFuture.Closed = true;
            }
            catch (Exception e)
            {
                FireExceptionCaught(e);
            }

            // And start the chain.
            CallNextSessionClosed(_head, _session);
        }

        public void FireSessionIdle(IdleStatus status)
        {
            _session.IncreaseIdleCount(status, DateTime.Now);
            CallNextSessionIdle(_head, _session, status);
        }

        public void FireMessageReceived(Object message)
        {
            IoBuffer buf = message as IoBuffer;
            if (buf != null)
                _session.IncreaseReadBytes(buf.Remaining, DateTime.Now);

            CallNextMessageReceived(_head, _session, message);
        }

        public void FireMessageSent(IWriteRequest request)
        {
            _session.IncreaseWrittenMessages(request, DateTime.Now);

            try
            {
                request.Future.Written = true;
            }
            catch (Exception e)
            {
                FireExceptionCaught(e);
            }

            if (!request.Encoded)
            {
                CallNextMessageSent(_head, _session, request);
            }
        }

        public void FireExceptionCaught(Exception cause)
        {
            CallNextExceptionCaught(_head, _session, cause);
        }

        public void FireFilterWrite(IWriteRequest writeRequest)
        {
            CallPreviousFilterWrite(_tail, _session, writeRequest);
        }

        public void FireFilterClose()
        {
            CallPreviousFilterClose(_tail, _session);
        }

        private void CallNext(IEntry<IoFilter, INextFilter> entry, Action<IoFilter, INextFilter> act, Action<Exception> error = null)
        {
            try
            {
                IoFilter filter = entry.Filter;
                INextFilter nextFilter = entry.NextFilter;
                act(filter, nextFilter);
            }
            catch (Exception e)
            {
                if (error == null)
                    FireExceptionCaught(e);
                else
                    error(e);
            }
        }

        private void CallPrevious(IEntry<IoFilter, INextFilter> entry, Action<IoFilter, INextFilter> act, Action<Exception> error = null)
        {
            try
            {
                IoFilter filter = entry.Filter;
                INextFilter nextFilter = entry.NextFilter;
                act(filter, nextFilter);
            }
            catch (Exception e)
            {
                if (error == null)
                    FireExceptionCaught(e);
                else
                    error(e);
            }
        }

        private void CallNextSessionCreated(IEntry<IoFilter, INextFilter> entry, IoSession session)
        {
            CallNext(entry, (filter, next) => filter.SessionCreated(next, session));
        }

        private void CallNextSessionOpened(IEntry<IoFilter, INextFilter> entry, IoSession session)
        {
            CallNext(entry, (filter, next) => filter.SessionOpened(next, session));
        }

        private void CallNextSessionClosed(IEntry<IoFilter, INextFilter> entry, IoSession session)
        {
            CallNext(entry, (filter, next) => filter.SessionClosed(next, session));
        }

        private void CallNextSessionIdle(IEntry<IoFilter, INextFilter> entry, IoSession session, IdleStatus status)
        {
            CallNext(entry, (filter, next) => filter.SessionIdle(next, session, status));
        }

        private void CallNextExceptionCaught(IEntry<IoFilter, INextFilter> entry, IoSession session, Exception cause)
        {
            // TODO Notify the related future.
            CallNext(entry, (filter, next) => filter.ExceptionCaught(next, _session, cause),
                e => log.Warn("Unexpected exception from exceptionCaught handler.", e));
        }

        private void CallNextMessageReceived(IEntry<IoFilter, INextFilter> entry, IoSession session, Object message)
        {
            CallNext(entry, (filter, next) => filter.MessageReceived(next, session, message));
        }

        private void CallNextMessageSent(IEntry<IoFilter, INextFilter> entry, IoSession session, IWriteRequest writeRequest)
        {
            CallNext(entry, (filter, next) => filter.MessageSent(next, session, writeRequest));
        }

        private void CallPreviousFilterClose(IEntry<IoFilter, INextFilter> entry, IoSession session)
        {
            CallPrevious(entry, (filter, next) => filter.FilterClose(next, session));
        }

        private void CallPreviousFilterWrite(IEntry<IoFilter, INextFilter> entry, IoSession session, IWriteRequest writeRequest)
        {
            CallPrevious(entry, (filter, next) => filter.FilterWrite(next, _session, writeRequest),
                e =>
                {
                    writeRequest.Future.Exception = e;
                    FireExceptionCaught(e);
                });
        }

        public new void Clear()
        {
            try
            {
                base.Clear();
            }
            catch (Exception e)
            {
                throw new IoFilterLifeCycleException("Clear(): in " + Session, e);
                //throw new IoFilterLifeCycleException("Clear(): " + entry.Name + " in " + Session, e);
            }
        }

        protected override void OnPreAdd(Entry entry)
        {
            try
            {
                entry.Filter.OnPreAdd(this, entry.Name, entry.NextFilter);
            }
            catch (Exception e)
            {
                throw new IoFilterLifeCycleException("OnPreAdd(): " + entry.Name + ':' + entry.Filter + " in " + Session, e);
            }
        }

        protected override void OnPostAdd(Entry entry)
        {
            try
            {
                entry.Filter.OnPostAdd(this, entry.Name, entry.NextFilter);
            }
            catch (Exception e)
            {
                Deregister0(entry);
                throw new IoFilterLifeCycleException("OnPostAdd(): " + entry.Name + ':' + entry.Filter + " in " + Session, e);
            }
        }

        protected override void OnPreRemove(Entry entry)
        {
            try
            {
                entry.Filter.OnPreRemove(this, entry.Name, entry.NextFilter);
            }
            catch (Exception e)
            {
                throw new IoFilterLifeCycleException("OnPreRemove(): " + entry.Name + ':' + entry.Filter + " in "
                        + Session, e);
            }
        }

        protected override void OnPostRemove(Entry entry)
        {
            try
            {
                entry.Filter.OnPostRemove(this, entry.Name, entry.NextFilter);
            }
            catch (Exception e)
            {
                throw new IoFilterLifeCycleException("OnPostRemove(): " + entry.Name + ':' + entry.Filter + " in "
                        + Session, e);
            }
        }

        class HeadFilter : IoFilterAdapter
        {
            public override void FilterWrite(INextFilter nextFilter, IoSession session, IWriteRequest writeRequest)
            {
                AbstractIoSession s = session as AbstractIoSession;
                if (s != null)
                {
                    // Maintain counters.
                    IoBuffer buffer = writeRequest.Message as IoBuffer;
                    if (buffer != null)
                    {
                        // I/O processor implementation will call buffer.Reset()
                        // it after the write operation is finished, because
                        // the buffer will be specified with messageSent event.
                        buffer.Mark();
                        Int32 remaining = buffer.Remaining;
                        if (remaining == 0)
                            // Zero-sized buffer means the internal message delimiter
                            s.IncreaseScheduledWriteMessages();
                        else
                            s.IncreaseScheduledWriteBytes(remaining);
                    }
                    else
                        s.IncreaseScheduledWriteMessages();
                }

                IWriteRequestQueue writeRequestQueue = session.WriteRequestQueue;

                if (session.WriteSuspended)
                {
                    writeRequestQueue.Offer(session, writeRequest);
                }
                else if (writeRequestQueue.Size == 0)
                {
                    // We can write directly the message
                    session.Processor.Write(session, writeRequest);
                }
                else
                {
                    writeRequestQueue.Offer(session, writeRequest);
                    session.Processor.Flush(session);
                }
            }

            public override void FilterClose(INextFilter nextFilter, IoSession session)
            {
                session.Processor.Remove(session);
            }
        }

        class TailFilter : IoFilterAdapter
        {
            public override void SessionCreated(INextFilter nextFilter, IoSession session)
            {
                try
                {
                    session.Handler.SessionCreated(session);
                }
                finally
                {
                    IConnectFuture future = session.RemoveAttribute(SessionCreatedFuture) as IConnectFuture;
                    if (future != null)
                        future.SetSession(session);
                }
            }

            public override void SessionOpened(INextFilter nextFilter, IoSession session)
            {
                session.Handler.SessionOpened(session);
            }

            public override void SessionClosed(INextFilter nextFilter, IoSession session)
            {
                AbstractIoSession s = session as AbstractIoSession;
                try
                {
                    session.Handler.SessionClosed(session);
                }
                finally
                {
                    try { session.WriteRequestQueue.Dispose(session); }
                    finally
                    {
                        try { s.AttributeMap.Dispose(session); }
                        finally
                        {
                            session.FilterChain.Clear();
                            // TODO IsUseReadOperation
                        }
                    }
                }
            }

            public override void SessionIdle(INextFilter nextFilter, IoSession session, IdleStatus status)
            {
                session.Handler.SessionIdle(session, status);
            }

            public override void ExceptionCaught(INextFilter nextFilter, IoSession session, Exception cause)
            {
                session.Handler.ExceptionCaught(session, cause);
                // TODO IsUseReadOperation
            }

            public override void MessageReceived(INextFilter nextFilter, IoSession session, Object message)
            {
                AbstractIoSession s = session as AbstractIoSession;
                if (s != null)
                {
                    IoBuffer buf = message as IoBuffer;
                    if (buf == null || !buf.HasRemaining)
                        s.IncreaseReadMessages(DateTime.Now);
                }

                session.Handler.MessageReceived(session, message);
                // TODO IsUseReadOperation
            }

            public override void MessageSent(INextFilter nextFilter, IoSession session, IWriteRequest writeRequest)
            {
                session.Handler.MessageSent(session, writeRequest.Message);
            }
        }

        class NextFilter : INextFilter
        {
            readonly DefaultIoFilterChain _chain;
            readonly Entry _entry;

            public NextFilter(Entry entry)
            {
                _chain = entry.Chain;
                _entry = entry;
            }

            public void SessionCreated(IoSession session)
            {
                _chain.CallNextSessionCreated(_entry.NextEntry, session);
            }

            public void SessionOpened(IoSession session)
            {
                _chain.CallNextSessionOpened(_entry.NextEntry, session);
            }

            public void SessionClosed(IoSession session)
            {
                _chain.CallNextSessionClosed(_entry.NextEntry, session);
            }

            public void SessionIdle(IoSession session, IdleStatus status)
            {
                _chain.CallNextSessionIdle(_entry.NextEntry, session, status);
            }

            public void ExceptionCaught(IoSession session, Exception cause)
            {
                _chain.CallNextExceptionCaught(_entry.NextEntry, session, cause);
            }

            public void MessageReceived(IoSession session, Object message)
            {
                _chain.CallNextMessageReceived(_entry.NextEntry, session, message);
            }

            public void MessageSent(IoSession session, IWriteRequest writeRequest)
            {
                _chain.CallNextMessageSent(_entry.NextEntry, session, writeRequest);
            }

            public void FilterWrite(IoSession session, IWriteRequest writeRequest)
            {
                _chain.CallPreviousFilterWrite(_entry.PrevEntry, session, writeRequest);
            }

            public void FilterClose(IoSession session)
            {
                _chain.CallPreviousFilterClose(_entry.PrevEntry, session);
            }

            public override String ToString()
            {
                return _entry.NextEntry == null ? "null" : _entry.NextEntry.Name;
            }
        }
    }
}
