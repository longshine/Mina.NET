using System;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Core.Filterchain
{
    /// <summary>
    /// An adapter class for <see cref="IoFilter"/>.  You can extend
    /// this class and selectively override required event filter methods only.  All
    /// methods forwards events to the next filter by default.
    /// </summary>
    public abstract class IoFilterAdapter : IoFilter
    {
        /// <inheritdoc/>
        public virtual void Init()
        { }

        /// <inheritdoc/>
        public virtual void Destroy()
        { }

        /// <inheritdoc/>
        public virtual void OnPreAdd(IoFilterChain parent, String name, INextFilter nextFilter)
        { }

        /// <inheritdoc/>
        public virtual void OnPostAdd(IoFilterChain parent, String name, INextFilter nextFilter)
        { }

        /// <inheritdoc/>
        public virtual void OnPreRemove(IoFilterChain parent, String name, INextFilter nextFilter)
        { }

        /// <inheritdoc/>
        public virtual void OnPostRemove(IoFilterChain parent, String name, INextFilter nextFilter)
        { }

        /// <inheritdoc/>
        public virtual void SessionCreated(INextFilter nextFilter, IoSession session)
        {
            nextFilter.SessionCreated(session);
        }

        /// <inheritdoc/>
        public virtual void SessionOpened(INextFilter nextFilter, IoSession session)
        {
            nextFilter.SessionOpened(session);
        }

        /// <inheritdoc/>
        public virtual void SessionClosed(INextFilter nextFilter, IoSession session)
        {
            nextFilter.SessionClosed(session);
        }

        /// <inheritdoc/>
        public virtual void SessionIdle(INextFilter nextFilter, IoSession session, IdleStatus status)
        {
            nextFilter.SessionIdle(session, status);
        }

        /// <inheritdoc/>
        public virtual void ExceptionCaught(INextFilter nextFilter, IoSession session, Exception cause)
        {
            nextFilter.ExceptionCaught(session, cause);
        }
        
        /// <inheritdoc/>
        public virtual void InputClosed(INextFilter nextFilter, IoSession session)
        {
            nextFilter.InputClosed(session);
        }

        /// <inheritdoc/>
        public virtual void MessageReceived(INextFilter nextFilter, IoSession session, Object message)
        {
            nextFilter.MessageReceived(session, message);
        }

        /// <inheritdoc/>
        public virtual void MessageSent(INextFilter nextFilter, IoSession session, IWriteRequest writeRequest)
        {
            nextFilter.MessageSent(session, writeRequest);
        }

        /// <inheritdoc/>
        public virtual void FilterClose(INextFilter nextFilter, IoSession session)
        {
            nextFilter.FilterClose(session);
        }

        /// <inheritdoc/>
        public virtual void FilterWrite(INextFilter nextFilter, IoSession session, IWriteRequest writeRequest)
        {
            nextFilter.FilterWrite(session, writeRequest);
        }
    }
}
