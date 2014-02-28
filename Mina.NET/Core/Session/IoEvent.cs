using System;
using Mina.Core.Write;

namespace Mina.Core.Session
{
    /// <summary>
    /// An I/O event or an I/O request that MINA provides.
    /// It is usually used by internal components to store I/O events.
    /// </summary>
    public class IoEvent
    {
        private readonly IoEventType _eventType;
        private readonly IoSession _session;
        private readonly Object _parameter;

        /// <summary>
        /// </summary>
        public IoEvent(IoEventType eventType, IoSession session, Object parameter)
        {
            if (session == null)
                throw new ArgumentNullException("session");
            _eventType = eventType;
            _session = session;
            _parameter = parameter;
        }

        /// <summary>
        /// Gets the <see cref="IoEventType"/> of this event.
        /// </summary>
        public IoEventType EventType
        {
            get { return _eventType; }
        }

        /// <summary>
        /// Gets the <see cref="IoSession"/> of this event.
        /// </summary>
        public IoSession Session
        {
            get { return _session; }
        }

        /// <summary>
        /// Gets the parameter of this event.
        /// </summary>
        public Object Parameter
        {
            get { return _parameter; }
        }

        /// <summary>
        /// Fires this event.
        /// </summary>
        public virtual void Fire()
        {
            switch (_eventType)
            {
                case IoEventType.MessageReceived:
                    _session.FilterChain.FireMessageReceived(_parameter);
                    break;
                case IoEventType.MessageSent:
                    _session.FilterChain.FireMessageSent((IWriteRequest)_parameter);
                    break;
                case IoEventType.Write:
                    _session.FilterChain.FireFilterWrite((IWriteRequest)_parameter);
                    break;
                case IoEventType.Close:
                    _session.FilterChain.FireFilterClose();
                    break;
                case IoEventType.ExceptionCaught:
                    _session.FilterChain.FireExceptionCaught((Exception)_parameter);
                    break;
                case IoEventType.SessionIdle:
                    _session.FilterChain.FireSessionIdle((IdleStatus)_parameter);
                    break;
                case IoEventType.SessionCreated:
                    _session.FilterChain.FireSessionCreated();
                    break;
                case IoEventType.SessionOpened:
                    _session.FilterChain.FireSessionOpened();
                    break;
                case IoEventType.SessionClosed:
                    _session.FilterChain.FireSessionClosed();
                    break;
                default:
                    throw new InvalidOperationException("Unknown event type: " + _eventType);
            }
        }

        /// <inheritdoc/>
        public override String ToString()
        {
            if (_parameter == null)
                return "[" + _session + "] " + _eventType;
            else
                return "[" + _session + "] " + _eventType + ": " + _parameter;
        }
    }
}
