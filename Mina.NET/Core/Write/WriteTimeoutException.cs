using System;
using System.Collections.Generic;

namespace Mina.Core.Write
{
    /// <summary>
    /// An exception which is thrown when write buffer is not flushed for
    /// <see cref="Core.Session.IoSessionConfig.WriteTimeout"/> seconds.
    /// </summary>
    [Serializable]
    public class WriteTimeoutException : WriteException
    {
        /// <summary>
        /// </summary>
        public WriteTimeoutException(IWriteRequest request)
            : base(request)
        { }

        /// <summary>
        /// </summary>
        public WriteTimeoutException(IEnumerable<IWriteRequest> requests)
            : base(requests)
        { }

        /// <summary>
        /// </summary>
        protected WriteTimeoutException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
