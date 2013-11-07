using System.Collections.Generic;

namespace Mina.Core.Write
{
    /// <summary>
    /// An exception which is thrown when one or more write operations were
    /// attempted on a closed session.
    /// </summary>
    public class WriteToClosedSessionException : WriteException
    {
        public WriteToClosedSessionException(IWriteRequest request)
            : base(request)
        { }

        public WriteToClosedSessionException(IEnumerable<IWriteRequest> requests)
            : base(requests)
        { }
    }
}
