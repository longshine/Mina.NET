using System;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Core.Filterchain
{
    /// <summary>
    /// A filter which intercepts <see cref="Core.Service.IoHandler"/> events like Servlet filters.
    /// </summary>
    public interface IoFilter
    {
        /// <summary>
        /// Invoked by <see cref="Filter.Util.ReferenceCountingFilter"/> when this filter
        /// is added to a <see cref="IoFilterChain"/> at the first time, so you can
        /// initialize shared resources.  Please note that this method is never
        /// called if you don't wrap a filter with <see cref="Filter.Util.ReferenceCountingFilter"/>.
        /// </summary>
        void Init();
        /// <summary>
        /// Invoked by <see cref="Filter.Util.ReferenceCountingFilter"/> when this filter
        /// is not used by any <see cref="IoFilterChain"/> anymore, so you can destroy
        /// shared resources.  Please note that this method is never called if
        /// you don't wrap a filter with <see cref="Filter.Util.ReferenceCountingFilter"/>.
        /// </summary>
        void Destroy();

        /// <summary>
        /// Invoked before this filter is added to the specified <paramref name="parent"/>.
        /// </summary>
        /// <remarks>
        /// Please note that this method can be invoked more than once if
        /// this filter is added to more than one parents. This method is not
        /// invoked before <see cref="Init()"/> is invoked.
        /// </remarks>
        /// <param name="parent">the parent who called this method</param>
        /// <param name="name">the name assigned to this filter</param>
        /// <param name="nextFilter">the <see cref="INextFilter"/> for this filter</param>
        void OnPreAdd(IoFilterChain parent, String name, INextFilter nextFilter);
        /// <summary>
        /// Invoked after this filter is added to the specified <paramref name="parent"/>.
        /// </summary>
        /// <remarks>
        /// Please note that this method can be invoked more than once if
        /// this filter is added to more than one parents. This method is not
        /// invoked before <see cref="Init()"/> is invoked.
        /// </remarks>
        /// <param name="parent">the parent who called this method</param>
        /// <param name="name">the name assigned to this filter</param>
        /// <param name="nextFilter">the <see cref="INextFilter"/> for this filter</param>
        void OnPostAdd(IoFilterChain parent, String name, INextFilter nextFilter);
        /// <summary>
        /// Invoked before this filter is removed from the specified <paramref name="parent"/>.
        /// </summary>
        /// <remarks>
        /// Please note that this method can be invoked more than once if
        /// this filter is removed from more than one parents.
        /// This method is always invoked before <see cref="Destroy()"/> is invoked.
        /// </remarks>
        /// <param name="parent">the parent who called this method</param>
        /// <param name="name">the name assigned to this filter</param>
        /// <param name="nextFilter">the <see cref="INextFilter"/> for this filter</param>
        void OnPreRemove(IoFilterChain parent, String name, INextFilter nextFilter);
        /// <summary>
        /// Invoked after this filter is removed from the specified <paramref name="parent"/>.
        /// </summary>
        /// <remarks>
        /// Please note that this method can be invoked more than once if
        /// this filter is removed from more than one parents.
        /// This method is always invoked before <see cref="Destroy()"/> is invoked.
        /// </remarks>
        /// <param name="parent">the parent who called this method</param>
        /// <param name="name">the name assigned to this filter</param>
        /// <param name="nextFilter">the <see cref="INextFilter"/> for this filter</param>
        void OnPostRemove(IoFilterChain parent, String name, INextFilter nextFilter);

        /// <summary>
        /// Filters <see cref="Core.Service.IoHandler.SessionCreated(IoSession)"/> event.
        /// </summary>
        void SessionCreated(INextFilter nextFilter, IoSession session);
        /// <summary>
        /// Filters <see cref="Core.Service.IoHandler.SessionOpened(IoSession)"/> event.
        /// </summary>
        void SessionOpened(INextFilter nextFilter, IoSession session);
        /// <summary>
        /// Filters <see cref="Core.Service.IoHandler.SessionClosed(IoSession)"/> event.
        /// </summary>
        void SessionClosed(INextFilter nextFilter, IoSession session);
        /// <summary>
        /// Filters <see cref="Core.Service.IoHandler.SessionIdle(IoSession, IdleStatus)"/> event.
        /// </summary>
        void SessionIdle(INextFilter nextFilter, IoSession session, IdleStatus status);
        /// <summary>
        /// Filters <see cref="Core.Service.IoHandler.ExceptionCaught(IoSession, Exception)"/> event.
        /// </summary>
        void ExceptionCaught(INextFilter nextFilter, IoSession session, Exception cause);
        /// <summary>
        /// Filters <see cref="Core.Service.IoHandler.InputClosed(IoSession)"/> event.
        /// </summary>
        /// <param name="nextFilter">
        /// The <see cref="INextFilter"/> for this filter.
        /// You can reuse this object until this filter is removed from the chain.
        /// </param>
        /// <param name="session">The <see cref="IoSession"/> which has received this event.</param>
        void InputClosed(INextFilter nextFilter, IoSession session);
        /// <summary>
        /// Filters <see cref="Core.Service.IoHandler.MessageReceived(IoSession, Object)"/> event.
        /// </summary>
        void MessageReceived(INextFilter nextFilter, IoSession session, Object message);
        /// <summary>
        /// Filters <see cref="Core.Service.IoHandler.MessageSent(IoSession, Object)"/> event.
        /// </summary>
        void MessageSent(INextFilter nextFilter, IoSession session, IWriteRequest writeRequest);
        /// <summary>
        /// Filters <see cref="IoSession.Close(Boolean)"/> event.
        /// </summary>
        void FilterClose(INextFilter nextFilter, IoSession session);
        /// <summary>
        /// Filters <see cref="IoSession.Write(Object)"/> event.
        /// </summary>
        void FilterWrite(INextFilter nextFilter, IoSession session, IWriteRequest writeRequest);
    }
}
