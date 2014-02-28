using System;
using System.Collections.Generic;

namespace Mina.Core.Write
{
    /// <summary>
    /// An exception which is thrown when one or more write operations were
    /// attempted on a closed session.
    /// </summary>
    [Serializable]
    public class WriteToClosedSessionException : WriteException
    {
        /// <summary>
        /// </summary>
        public WriteToClosedSessionException(IWriteRequest request)
            : base(request)
        { }

        /// <summary>
        /// </summary>
        public WriteToClosedSessionException(IEnumerable<IWriteRequest> requests)
            : base(requests)
        { }

        /// <summary>
        /// </summary>
        protected WriteToClosedSessionException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
