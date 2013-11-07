using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Common.Logging;
using Mina.Core.Buffer;
using Mina.Core.Session;
using Mina.Core.Write;
using Mina.Core.Future;
using System.Text;

namespace Mina.Core.Filterchain
{
    /// <summary>
    /// A default implementation of <see cref="IoFilterChain"/> that provides
    /// all operations for developers who want to implement their own
    /// transport layer once used with <see cref="AbstractIoSession"/>.
    /// </summary>
    public class DefaultIoFilterChain : IoFilterChain
    {
        public readonly static AttributeKey SessionCreatedFuture = new AttributeKey(typeof(DefaultIoFilterChain), "connectFuture");
        private readonly static ILog log = LogManager.GetLogger(typeof(DefaultIoFilterChain));

        private readonly AbstractIoSession _session;
        private readonly IDictionary<String, IEntry> _name2entry = new ConcurrentDictionary<String, IEntry>();
        private readonly EntryImpl _head;
        private readonly EntryImpl _tail;

        public DefaultIoFilterChain(AbstractIoSession session)
        {
            if (session == null)
                throw new ArgumentNullException("session");

            _session = session;
            _head = new EntryImpl(this, null, null, "head", new HeadFilter());
            _tail = new EntryImpl(this, _head, null, "tail", new TailFilter());
            _head._nextEntry = _tail;
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
            // TODO update future

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

            // TODO set future

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

        private void CallNext(IEntry entry, Action<IoFilter, INextFilter> act, Action<Exception> error = null)
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

        private void CallPrevious(IEntry entry, Action<IoFilter, INextFilter> act, Action<Exception> error = null)
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

        private void CallNextSessionCreated(IEntry entry, IoSession session)
        {
            CallNext(entry, (filter, next) => filter.SessionCreated(next, session));
        }

        private void CallNextSessionOpened(IEntry entry, IoSession session)
        {
            CallNext(entry, (filter, next) => filter.SessionOpened(next, session));
        }

        private void CallNextSessionClosed(IEntry entry, IoSession session)
        {
            CallNext(entry, (filter, next) => filter.SessionClosed(next, session));
        }

        private void CallNextSessionIdle(IEntry entry, IoSession session, IdleStatus status)
        {
            CallNext(entry, (filter, next) => filter.SessionIdle(next, session, status));
        }

        private void CallNextExceptionCaught(IEntry entry, IoSession session, Exception cause)
        {
            // TODO Notify the related future.
            CallNext(entry, (filter, next) => filter.ExceptionCaught(next, _session, cause),
                e => log.Warn("Unexpected exception from exceptionCaught handler.", e));
        }

        private void CallNextMessageReceived(IEntry entry, IoSession session, Object message)
        {
            CallNext(entry, (filter, next) => filter.MessageReceived(next, session, message));
        }

        private void CallNextMessageSent(IEntry entry, IoSession session, IWriteRequest writeRequest)
        {
            CallNext(entry, (filter, next) => filter.MessageSent(next, session, writeRequest));
        }

        private void CallPreviousFilterClose(IEntry entry, IoSession session)
        {
            CallPrevious(entry, (filter, next) => filter.FilterClose(next, session));
        }

        private void CallPreviousFilterWrite(IEntry entry, IoSession session, IWriteRequest writeRequest)
        {
            CallPrevious(entry, (filter, next) => filter.FilterWrite(next, _session, writeRequest),
                e =>
                {
                    writeRequest.Future.Exception = e;
                    FireExceptionCaught(e);
                });
        }

        public IEntry GetEntry(String name)
        {
            IEntry e;
            _name2entry.TryGetValue(name, out e);
            return e;
        }

        public IoFilter Get(String name)
        {
            IEntry e = GetEntry(name);
            return e == null ? null : e.Filter;
        }

        public IEntry GetEntry(IoFilter filter)
        {
            EntryImpl e = _head._nextEntry;
            while (e != _tail)
            {
                if (e.Filter == filter)
                    return e;
                e = e._nextEntry;
            }
            return null;
        }

        public IEnumerable<IEntry> GetAll()
        {
            List<IEntry> list = new List<IEntry>();
            EntryImpl e = _head._nextEntry;
            while (e != _tail)
            {
                list.Add(e);
                e = e._nextEntry;
            }
            return list;
        }

        public Boolean Contains(String name)
        {
            return GetEntry(name) != null;
        }

        public Boolean Contains(IoFilter filter)
        {
            return GetEntry(filter) != null;
        }

        public void AddFirst(String name, IoFilter filter)
        {
            CheckAddable(name);
            Register(_head, name, filter);
        }

        public void AddLast(String name, IoFilter filter)
        {
            CheckAddable(name);
            Register(_tail._prevEntry, name, filter);
        }

        public void AddBefore(String baseName, String name, IoFilter filter)
        {
            EntryImpl baseEntry = CheckOldName(baseName);
            CheckAddable(name);
            Register(baseEntry._prevEntry, name, filter);
        }

        public void AddAfter(String baseName, String name, IoFilter filter)
        {
            EntryImpl baseEntry = CheckOldName(baseName);
            CheckAddable(name);
            Register(baseEntry, name, filter);
        }

        public IoFilter Replace(String name, IoFilter newFilter)
        {
            EntryImpl entry = CheckOldName(name);
            IoFilter oldFilter = entry.Filter;
            entry.Filter = newFilter;
            return oldFilter;
        }

        public void Replace(IoFilter oldFilter, IoFilter newFilter)
        {
            EntryImpl e = _head._nextEntry;
            while (e != _tail)
            {
                if (e.Filter == oldFilter)
                {
                    e.Filter = newFilter;
                    return;
                }
                e = e._nextEntry;
            }
            throw new ArgumentException("Filter not found: " + oldFilter.GetType().Name);
        }

        public IoFilter Remove(String name)
        {
            EntryImpl entry = CheckOldName(name);
            Deregister(entry);
            return entry.Filter;
        }

        public void Remove(IoFilter filter)
        {
            EntryImpl e = _head._nextEntry;
            while (e != _tail)
            {
                if (e.Filter == filter)
                {
                    Deregister(e);
                    return;
                }
                e = e._nextEntry;
            }
            throw new ArgumentException("Filter not found: " + filter.GetType().Name);
        }

        public void Clear()
        {
            foreach (var entry in _name2entry.Values)
            {
                try
                {
                    Deregister((EntryImpl)entry);
                }
                catch (Exception e)
                {
                    throw new IoFilterLifeCycleException("Clear(): " + entry.Name + " in " + Session, e);
                }
            }
        }

        private void CheckAddable(String name)
        {
            if (_name2entry.ContainsKey(name))
                throw new ArgumentException("Other filter is using the same name '" + name + "'");
        }

        private EntryImpl CheckOldName(String baseName)
        {
            return (EntryImpl)_name2entry[baseName];
        }

        private void Register(EntryImpl prevEntry, String name, IoFilter filter)
        {
            EntryImpl newEntry = new EntryImpl(this, prevEntry, prevEntry._nextEntry, name, filter);

            try
            {
                filter.OnPreAdd(this, name, newEntry.NextFilter);
            }
            catch (Exception e)
            {
                throw new IoFilterLifeCycleException("OnPreAdd(): " + name + ':' + filter + " in " + Session, e);
            }

            prevEntry._nextEntry._prevEntry = newEntry;
            prevEntry._nextEntry = newEntry;
            _name2entry.Add(name, newEntry);

            try
            {
                filter.OnPostAdd(this, name, newEntry.NextFilter);
            }
            catch (Exception e)
            {
                Deregister0(newEntry);
                throw new IoFilterLifeCycleException("OnPostAdd(): " + name + ':' + filter + " in " + Session, e);
            }
        }

        private void Deregister(EntryImpl entry)
        {
            IoFilter filter = entry.Filter;

            try
            {
                filter.OnPreRemove(this, entry.Name, entry.NextFilter);
            }
            catch (Exception e)
            {
                throw new IoFilterLifeCycleException("OnPreRemove(): " + entry.Name + ':' + filter + " in "
                        + Session, e);
            }

            Deregister0(entry);

            try
            {
                filter.OnPostRemove(this, entry.Name, entry.NextFilter);
            }
            catch (Exception e)
            {
                throw new IoFilterLifeCycleException("OnPostRemove(): " + entry.Name + ':' + filter + " in "
                        + Session, e);
            }
        }

        private void Deregister0(EntryImpl entry)
        {
            EntryImpl prevEntry = entry._prevEntry;
            EntryImpl nextEntry = entry._nextEntry;
            prevEntry._nextEntry = nextEntry;
            nextEntry._prevEntry = prevEntry;

            _name2entry.Remove(entry.Name);
        }

        class EntryImpl : IEntry
        {
            private readonly DefaultIoFilterChain _chain;
            private readonly String _name;
            internal EntryImpl _prevEntry;
            internal EntryImpl _nextEntry;
            private IoFilter _filter;
            private readonly INextFilter _nextFilter;

            public EntryImpl(DefaultIoFilterChain chain, EntryImpl prevEntry, EntryImpl nextEntry, String name, IoFilter filter)
            {
                if (filter == null)
                    throw new ArgumentNullException("filter");
                if (name == null)
                    throw new ArgumentNullException("name");

                _chain = chain;
                _prevEntry = prevEntry;
                _nextEntry = nextEntry;
                _name = name;
                _filter = filter;
                _nextFilter = new NextFilter(chain, this);
            }

            public String Name
            {
                get { return _name; }
            }

            public IoFilter Filter
            {
                get { return _filter; }
                set
                {
                    if (value == null)
                        throw new ArgumentNullException();
                    _filter = value;
                }
            }

            public INextFilter NextFilter
            {
                get { return _nextFilter; }
            }

            public void AddBefore(String name, IoFilter filter)
            {
                _chain.AddBefore(Name, name, filter);
            }

            public void AddAfter(String name, IoFilter filter)
            {
                _chain.AddAfter(Name, name, filter);
            }

            public void Replace(IoFilter newFilter)
            {
                _chain.Replace(Name, newFilter);
            }

            public void Remove()
            {
                _chain.Remove(Name);
            }

            public override String ToString()
            {
                StringBuilder sb = new StringBuilder();

                // Add the current filter
                sb.Append("('").Append(Name).Append('\'');

                // Add the previous filter
                sb.Append(", prev: '");

                if (_prevEntry != null)
                {
                    sb.Append(_prevEntry.Name);
                    sb.Append(':');
                    sb.Append(_prevEntry.Filter.GetType().Name);
                }
                else
                {
                    sb.Append("null");
                }

                // Add the next filter
                sb.Append("', next: '");

                if (_nextEntry != null)
                {
                    sb.Append(_nextEntry.Name);
                    sb.Append(':');
                    sb.Append(_nextEntry.Filter.GetType().Name);
                }
                else
                {
                    sb.Append("null");
                }

                sb.Append("')");
                return sb.ToString();
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

            public override void MessageReceived(INextFilter nextFilter, IoSession session, object message)
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
            DefaultIoFilterChain _chain;
            EntryImpl _entry;

            public NextFilter(DefaultIoFilterChain chain, EntryImpl entry)
            {
                _chain = chain;
                _entry = entry;
            }

            public void SessionCreated(IoSession session)
            {
                _chain.CallNextSessionCreated(_entry._nextEntry, session);
            }

            public void SessionOpened(IoSession session)
            {
                _chain.CallNextSessionOpened(_entry._nextEntry, session);
            }

            public void SessionClosed(IoSession session)
            {
                _chain.CallNextSessionClosed(_entry._nextEntry, session);
            }

            public void SessionIdle(IoSession session, IdleStatus status)
            {
                _chain.CallNextSessionIdle(_entry._nextEntry, session, status);
            }

            public void ExceptionCaught(IoSession session, Exception cause)
            {
                _chain.CallNextExceptionCaught(_entry._nextEntry, session, cause);
            }

            public void MessageReceived(IoSession session, Object message)
            {
                _chain.CallNextMessageReceived(_entry._nextEntry, session, message);
            }

            public void MessageSent(IoSession session, IWriteRequest writeRequest)
            {
                _chain.CallNextMessageSent(_entry._nextEntry, session, writeRequest);
            }

            public void FilterWrite(IoSession session, IWriteRequest writeRequest)
            {
                _chain.CallPreviousFilterWrite(_entry._prevEntry, session, writeRequest);
            }

            public void FilterClose(IoSession session)
            {
                _chain.CallPreviousFilterClose(_entry._prevEntry, session);
            }

            public override String ToString()
            {
                return _entry._nextEntry == null ? "null" : _entry._nextEntry.Name;
            }
        }
    }
}
