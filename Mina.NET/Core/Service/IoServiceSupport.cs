using Mina.Core.Session;

namespace Mina.Core.Service
{
    interface IoServiceSupport
    {
        void FireServiceActivated();
        void FireSessionCreated(IoSession session);
    }
}
