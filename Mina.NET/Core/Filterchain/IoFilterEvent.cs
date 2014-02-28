using System;
using Common.Logging;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Core.Filterchain
{
    /// <summary>
    /// An I/O event or an I/O request that MINA provides for <see cref="IoFilter"/>s.
    /// It is usually used by internal components to store I/O events.
    /// </summary>
    public class IoFilterEvent : IoEvent
    {
        static readonly ILog log = LogManager.GetLogger(typeof(IoFilterEvent));

        private readonly INextFilter _nextFilter;

        /// <summary>
        /// </summary>
        public IoFilterEvent(INextFilter nextFilter, IoEventType eventType, IoSession session, Object parameter)
            : base(eventType, session, parameter)
        {
            if (nextFilter == null)
                throw new ArgumentNullException("nextFilter");
            _nextFilter = nextFilter;
        }

        /// <summary>
        /// Gets the next filter.
        /// </summary>
        public INextFilter NextFilter
        {
            get { return _nextFilter; }
        }

        /// <inheritdoc/>
        public override void Fire()
        {
            if (log.IsDebugEnabled)
                log.DebugFormat("Firing a {0} event for session {1}", EventType, Session.Id);

            switch (EventType)
            {
                case IoEventType.MessageReceived:
                    _nextFilter.MessageReceived(Session, Parameter);
                    break;
                case IoEventType.MessageSent:
                    _nextFilter.MessageSent(Session, (IWriteRequest)Parameter);
                    break;
                case IoEventType.Write:
                    _nextFilter.FilterWrite(Session, (IWriteRequest)Parameter);
                    break;
                case IoEventType.Close:
                    _nextFilter.FilterClose(Session);
                    break;
                case IoEventType.ExceptionCaught:
                    _nextFilter.ExceptionCaught(Session, (Exception)Parameter);
                    break;
                case IoEventType.SessionIdle:
                    _nextFilter.SessionIdle(Session, (IdleStatus)Parameter);
                    break;
                case IoEventType.SessionCreated:
                    _nextFilter.SessionCreated(Session);
                    break;
                case IoEventType.SessionOpened:
                    _nextFilter.SessionOpened(Session);
                    break;
                case IoEventType.SessionClosed:
                    _nextFilter.SessionClosed(Session);
                    break;
                default:
                    throw new InvalidOperationException("Unknown event type: " + EventType);
            }

            if (log.IsDebugEnabled)
                log.DebugFormat("Event {0} has been fired for session {1}", EventType, Session.Id);
        }
    }
}
