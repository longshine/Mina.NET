using Mina.Core.Write;

namespace Mina.Core.Session
{
    /// <summary>
    /// Provides data structures to a newly created session.
    /// </summary>
    public interface IoSessionDataStructureFactory
    {
        IoSessionAttributeMap GetAttributeMap(IoSession session);
        IWriteRequestQueue GetWriteRequestQueue(IoSession session);
    }
}
