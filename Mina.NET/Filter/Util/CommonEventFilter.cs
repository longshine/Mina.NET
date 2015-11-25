using System;
using Mina.Core.Filterchain;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Filter.Util
{
    /// <summary>
    /// Extend this class when you want to create a filter that
    /// wraps the same logic around all 9 IoEvents
    /// </summary>
    public abstract class CommonEventFilter : IoFilterAdapter
    {
        /// <inheritdoc/>
        public override void SessionCreated(INextFilter nextFilter, IoSession session)
        {
            Filter(new IoFilterEvent(nextFilter, IoEventType.SessionCreated, session, null));
        }

        /// <inheritdoc/>
        public override void SessionOpened(INextFilter nextFilter, IoSession session)
        {
            Filter(new IoFilterEvent(nextFilter, IoEventType.SessionOpened, session, null));
        }

        /// <inheritdoc/>
        public override void SessionIdle(INextFilter nextFilter, IoSession session, IdleStatus status)
        {
            Filter(new IoFilterEvent(nextFilter, IoEventType.SessionIdle, session, status));
        }

        /// <inheritdoc/>
        public override void SessionClosed(INextFilter nextFilter, IoSession session)
        {
            Filter(new IoFilterEvent(nextFilter, IoEventType.SessionClosed, session, null));
        }

        /// <inheritdoc/>
        public override void ExceptionCaught(INextFilter nextFilter, IoSession session, Exception cause)
        {
            Filter(new IoFilterEvent(nextFilter, IoEventType.ExceptionCaught, session, cause));
        }

        /// <inheritdoc/>
        public override void MessageReceived(INextFilter nextFilter, IoSession session, Object message)
        {
            Filter(new IoFilterEvent(nextFilter, IoEventType.MessageReceived, session, message));
        }

        /// <inheritdoc/>
        public override void MessageSent(INextFilter nextFilter, IoSession session, IWriteRequest writeRequest)
        {
            Filter(new IoFilterEvent(nextFilter, IoEventType.MessageSent, session, writeRequest));
        }

        /// <inheritdoc/>
        public override void FilterWrite(INextFilter nextFilter, IoSession session, IWriteRequest writeRequest)
        {
            Filter(new IoFilterEvent(nextFilter, IoEventType.Write, session, writeRequest));
        }

        /// <inheritdoc/>
        public override void FilterClose(INextFilter nextFilter, IoSession session)
        {
            Filter(new IoFilterEvent(nextFilter, IoEventType.Close, session, null));
        }

        /// <summary>
        /// Filters an <see cref="IoFilterEvent"/>.
        /// </summary>
        /// <param name="ioe">the event</param>
        protected abstract void Filter(IoFilterEvent ioe);
    }
}
