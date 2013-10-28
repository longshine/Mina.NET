using System;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Core.Filterchain
{
    /// <summary>
    /// A filter which intercepts <see cref="IoHandler"/> events like Servlet filters.
    /// </summary>
    public interface IoFilter
    {
        void Init();
        void Destroy();

        void OnPreAdd(IoFilterChain parent, String name, INextFilter nextFilter);
        void OnPostAdd(IoFilterChain parent, String name, INextFilter nextFilter);
        void OnPreRemove(IoFilterChain parent, String name, INextFilter nextFilter);
        void OnPostRemove(IoFilterChain parent, String name, INextFilter nextFilter);

        void SessionCreated(INextFilter nextFilter, IoSession session);
        void SessionOpened(INextFilter nextFilter, IoSession session);
        void SessionClosed(INextFilter nextFilter, IoSession session);
        void SessionIdle(INextFilter nextFilter, IoSession session, IdleStatus status);
        void ExceptionCaught(INextFilter nextFilter, IoSession session, Exception cause);
        void MessageReceived(INextFilter nextFilter, IoSession session, Object message);
        void MessageSent(INextFilter nextFilter, IoSession session, IWriteRequest writeRequest);
        void FilterClose(INextFilter nextFilter, IoSession session);
        void FilterWrite(INextFilter nextFilter, IoSession session, IWriteRequest writeRequest);
    }
}
