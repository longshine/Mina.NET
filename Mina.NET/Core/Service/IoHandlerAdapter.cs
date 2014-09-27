using System;
using Common.Logging;
using Mina.Core.Session;

namespace Mina.Core.Service
{
    /// <summary>
    /// An adapter class for <see cref="IoHandler"/>.  You can extend this
    /// class and selectively override required event handler methods only.  All
    /// methods do nothing by default.
    /// </summary>
    public class IoHandlerAdapter : IoHandler
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(IoHandlerAdapter));

        /// <inheritdoc/>
        public virtual void SessionCreated(IoSession session)
        {
            // Empty handler
        }

        /// <inheritdoc/>
        public virtual void SessionOpened(IoSession session)
        {
            // Empty handler
        }

        /// <inheritdoc/>
        public virtual void SessionClosed(IoSession session)
        {
            // Empty handler
        }

        /// <inheritdoc/>
        public virtual void SessionIdle(IoSession session, IdleStatus status)
        {
            // Empty handler
        }

        /// <inheritdoc/>
        public virtual void ExceptionCaught(IoSession session, Exception cause)
        {
            if (log.IsWarnEnabled)
            {
                log.WarnFormat("EXCEPTION, please implement {0}.ExceptionCaught() for proper handling: {1}", GetType().Name, cause);
            }
        }

        /// <inheritdoc/>
        public virtual void MessageReceived(IoSession session, Object message)
        {
            // Empty handler
        }

        /// <inheritdoc/>
        public virtual void MessageSent(IoSession session, Object message)
        {
            // Empty handler
        }

        /// <inheritdoc/>
        public void InputClosed(IoSession session)
        {
            session.Close(true);
        }
    }
}
