using System;

namespace Mina.Filter.KeepAlive
{
    /// <summary>
    /// An exception which is thrown when a keep-alive response
    /// message was not received within a certain timeout.
    /// </summary>
    [Serializable]
    public class KeepAliveRequestTimeoutException : Exception
    {
        public KeepAliveRequestTimeoutException() { }

        public KeepAliveRequestTimeoutException(String message) : base(message) { }

        public KeepAliveRequestTimeoutException(String message, Exception inner) : base(message, inner) { }
        
        protected KeepAliveRequestTimeoutException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
