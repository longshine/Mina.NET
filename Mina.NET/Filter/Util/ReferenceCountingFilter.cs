using System;
using Mina.Core.Filterchain;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Filter.Util
{
    /// <summary>
    /// An <see cref="IoFilter"/> wrapper that keeps track of the number of usages of this filter and will call init/destroy
    /// when the filter is not in use.
    /// </summary>
    public class ReferenceCountingFilter : IoFilterAdapter
    {
        private readonly IoFilter _filter;
        private Int32 _count = 0;

        /// <summary>
        /// </summary>
        public ReferenceCountingFilter(IoFilter filter)
        {
            _filter = filter;
        }

        /// <inheritdoc/>
        public override void Init()
        {
            // no-op, will init on-demand in pre-add if count == 0
        }

        /// <inheritdoc/>
        public override void Destroy()
        {
            // no-op, will destroy on-demand in post-remove if count == 0
        }

        /// <inheritdoc/>
        public override void OnPreAdd(IoFilterChain parent, String name, INextFilter nextFilter)
        {
            if (_count == 0)
                _filter.Init();
            _count++;
            _filter.OnPreAdd(parent, name, nextFilter);
        }

        /// <inheritdoc/>
        public override void OnPostRemove(IoFilterChain parent, String name, INextFilter nextFilter)
        {
            _filter.OnPostRemove(parent, name, nextFilter);
            _count--;
            if (_count == 0)
                _filter.Destroy();
        }

        /// <inheritdoc/>
        public override void OnPostAdd(IoFilterChain parent, String name, INextFilter nextFilter)
        {
            _filter.OnPostAdd(parent, name, nextFilter);
        }

        /// <inheritdoc/>
        public override void OnPreRemove(IoFilterChain parent, String name, INextFilter nextFilter)
        {
            _filter.OnPreRemove(parent, name, nextFilter);
        }

        /// <inheritdoc/>
        public override void SessionCreated(INextFilter nextFilter, IoSession session)
        {
            _filter.SessionCreated(nextFilter, session);
        }

        /// <inheritdoc/>
        public override void SessionOpened(INextFilter nextFilter, IoSession session)
        {
            _filter.SessionOpened(nextFilter, session);
        }

        /// <inheritdoc/>
        public override void SessionClosed(INextFilter nextFilter, IoSession session)
        {
            _filter.SessionClosed(nextFilter, session);
        }

        /// <inheritdoc/>
        public override void SessionIdle(INextFilter nextFilter, IoSession session, IdleStatus status)
        {
            _filter.SessionIdle(nextFilter, session, status);
        }

        /// <inheritdoc/>
        public override void ExceptionCaught(INextFilter nextFilter, IoSession session, Exception cause)
        {
            _filter.ExceptionCaught(nextFilter, session, cause);
        }

        /// <inheritdoc/>
        public override void MessageReceived(INextFilter nextFilter, IoSession session, Object message)
        {
            _filter.MessageReceived(nextFilter, session, message);
        }

        /// <inheritdoc/>
        public override void MessageSent(INextFilter nextFilter, IoSession session, IWriteRequest writeRequest)
        {
            _filter.MessageSent(nextFilter, session, writeRequest);
        }

        /// <inheritdoc/>
        public override void FilterClose(INextFilter nextFilter, IoSession session)
        {
            _filter.FilterClose(nextFilter, session);
        }

        /// <inheritdoc/>
        public override void FilterWrite(INextFilter nextFilter, IoSession session, IWriteRequest writeRequest)
        {
            _filter.FilterWrite(nextFilter, session, writeRequest);
        }
    }
}
