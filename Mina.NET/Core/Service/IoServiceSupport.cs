using Mina.Core.Session;

namespace Mina.Core.Service
{
    interface IoServiceSupport
    {
        void FireServiceActivated();
        void FireServiceIdle(IdleStatus idleStatus);
        void FireSessionCreated(IoSession session);
        void FireSessionDestroyed(IoSession session);
        void FireServiceDeactivated();
    }
}
