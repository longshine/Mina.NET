using Mina.Core.Write;

namespace Mina.Core.Session
{
    class DefaultIoSessionDataStructureFactory : IoSessionDataStructureFactory
    {
        public IoSessionAttributeMap GetAttributeMap(IoSession session)
        {
            return new DefaultIoSessionAttributeMap();
        }

        public IWriteRequestQueue GetWriteRequestQueue(IoSession session)
        {
            return new DefaultWriteRequestQueue();
        }
    }
}
