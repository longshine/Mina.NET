using System;
using Mina.Core.Session;

namespace Mina.Core.Service
{
    /// <summary>
    /// Handles all I/O events fired by MINA.
    /// </summary>
    public interface IoHandler
    {
        void SessionCreated(IoSession session);
        void SessionOpened(IoSession session);
        void SessionClosed(IoSession session);
        void SessionIdle(IoSession session, IdleStatus status);
        void ExceptionCaught(IoSession session, Exception cause);
        void MessageReceived(IoSession session, Object message);
        void MessageSent(IoSession session, Object message);
    }
}
