using System;
using Mina.Core.Filterchain;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Filter.Executor
{
    /// <summary>
    /// A filter that forwards I/O events to <see cref="IoEventExecutor"/> to enforce a certain
    /// thread model while allowing the events per session to be processed
    /// simultaneously. You can apply various thread model by inserting this filter
    /// to a <see cref="IoFilterChain"/>.
    /// </summary>
    public class ExecutorFilter : IoFilterAdapter
    {
        private const IoEventType DefaultEventSet = IoEventType.ExceptionCaught |
            IoEventType.MessageReceived | IoEventType.MessageSent | IoEventType.SessionClosed |
            IoEventType.SessionIdle | IoEventType.SessionOpened;

        private readonly IoEventType _eventTypes;
        private readonly IoEventExecutor _executor;

        /// <summary>
        /// Creates an executor filter with default <see cref="IoEventExecutor"/> on default event types.
        /// </summary>
        public ExecutorFilter()
            : this(null, DefaultEventSet)
        { }

        /// <summary>
        /// Creates an executor filter with default <see cref="IoEventExecutor"/>.
        /// </summary>
        /// <param name="eventTypes">the event types interested</param>
        public ExecutorFilter(IoEventType eventTypes)
            : this(null, eventTypes)
        { }

        /// <summary>
        /// Creates an executor filter on default event types.
        /// </summary>
        /// <param name="executor">the <see cref="IoEventExecutor"/> to run events</param>
        public ExecutorFilter(IoEventExecutor executor)
            : this(executor, DefaultEventSet)
        { }

        /// <summary>
        /// Creates an executor filter.
        /// </summary>
        /// <param name="executor">the <see cref="IoEventExecutor"/> to run events</param>
        /// <param name="eventTypes">the event types interested</param>
        public ExecutorFilter(IoEventExecutor executor, IoEventType eventTypes)
        {
            _eventTypes = eventTypes;
            if (executor == null)
                _executor = new OrderedThreadPoolExecutor();
            else
                _executor = executor;
        }

        /// <summary>
        /// Gets the <see cref="IoEventExecutor"/> to run events.
        /// </summary>
        public IoEventExecutor Executor
        {
            get { return _executor; }
        }

        /// <inheritdoc/>
        public override void OnPreAdd(IoFilterChain parent, String name, INextFilter nextFilter)
        {
            if (parent.Contains(this))
                throw new ArgumentException("You can't add the same filter instance more than once. Create another instance and add it.");
        }

        /// <inheritdoc/>
        public override void SessionOpened(INextFilter nextFilter, IoSession session)
        {
            if ((_eventTypes & IoEventType.SessionOpened) == IoEventType.SessionOpened)
            {
                IoFilterEvent ioe = new IoFilterEvent(nextFilter, IoEventType.SessionOpened, session, null);
                FireEvent(ioe);
            }
            else
            {
                base.SessionOpened(nextFilter, session);
            }
        }

        /// <inheritdoc/>
        public override void SessionClosed(INextFilter nextFilter, IoSession session)
        {
            if ((_eventTypes & IoEventType.SessionClosed) == IoEventType.SessionClosed)
            {
                IoFilterEvent ioe = new IoFilterEvent(nextFilter, IoEventType.SessionClosed, session, null);
                FireEvent(ioe);
            }
            else
            {
                base.SessionClosed(nextFilter, session);
            }
        }

        /// <inheritdoc/>
        public override void SessionIdle(INextFilter nextFilter, IoSession session, IdleStatus status)
        {
            if ((_eventTypes & IoEventType.SessionIdle) == IoEventType.SessionIdle)
            {
                IoFilterEvent ioe = new IoFilterEvent(nextFilter, IoEventType.SessionIdle, session, status);
                FireEvent(ioe);
            }
            else
            {
                base.SessionIdle(nextFilter, session, status);
            }
        }

        /// <inheritdoc/>
        public override void ExceptionCaught(INextFilter nextFilter, IoSession session, Exception cause)
        {
            if ((_eventTypes & IoEventType.ExceptionCaught) == IoEventType.ExceptionCaught)
            {
                IoFilterEvent ioe = new IoFilterEvent(nextFilter, IoEventType.ExceptionCaught, session, cause);
                FireEvent(ioe);
            }
            else
            {
                base.ExceptionCaught(nextFilter, session, cause);
            }
        }

        /// <inheritdoc/>
        public override void MessageReceived(INextFilter nextFilter, IoSession session, Object message)
        {
            if ((_eventTypes & IoEventType.MessageReceived) == IoEventType.MessageReceived)
            {
                IoFilterEvent ioe = new IoFilterEvent(nextFilter, IoEventType.MessageReceived, session, message);
                FireEvent(ioe);
            }
            else
            {
                base.MessageReceived(nextFilter, session, message);
            }
        }

        /// <inheritdoc/>
        public override void MessageSent(INextFilter nextFilter, IoSession session, IWriteRequest writeRequest)
        {
            if ((_eventTypes & IoEventType.MessageSent) == IoEventType.MessageSent)
            {
                IoFilterEvent ioe = new IoFilterEvent(nextFilter, IoEventType.MessageSent, session, writeRequest);
                FireEvent(ioe);
            }
            else
            {
                base.MessageSent(nextFilter, session, writeRequest);
            }
        }

        /// <inheritdoc/>
        public override void FilterWrite(INextFilter nextFilter, IoSession session, IWriteRequest writeRequest)
        {
            if ((_eventTypes & IoEventType.Write) == IoEventType.Write)
            {
                IoFilterEvent ioe = new IoFilterEvent(nextFilter, IoEventType.Write, session, writeRequest);
                FireEvent(ioe);
            }
            else
            {
                base.FilterWrite(nextFilter, session, writeRequest);
            }
        }

        /// <inheritdoc/>
        public override void FilterClose(INextFilter nextFilter, IoSession session)
        {
            if ((_eventTypes & IoEventType.Close) == IoEventType.Close)
            {
                IoFilterEvent ioe = new IoFilterEvent(nextFilter, IoEventType.Close, session, null);
                FireEvent(ioe);
            }
            else
            {
                base.FilterClose(nextFilter, session);
            }
        }

        /// <summary>
        /// Fires an event.
        /// </summary>
        /// <param name="ioe"></param>
        protected void FireEvent(IoFilterEvent ioe)
        {
            _executor.Execute(ioe);
        }
    }
}
