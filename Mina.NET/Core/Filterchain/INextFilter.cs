using System;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Core.Filterchain
{
    /// <summary>
    /// Represents the next <see cref="IoFilter"/> in <see cref="IoFilterChain"/>.
    /// </summary>
    public interface INextFilter
    {
        void SessionCreated(IoSession session);
        void SessionOpened(IoSession session);
        void SessionClosed(IoSession session);
        void SessionIdle(IoSession session, IdleStatus status);
        void ExceptionCaught(IoSession session, Exception cause);
        void MessageReceived(IoSession session, Object message);
        void MessageSent(IoSession session, IWriteRequest writeRequest);
        void FilterClose(IoSession session);
        void FilterWrite(IoSession session, IWriteRequest writeRequest);
    }
}
