using System;

namespace Mina.Core.Session
{
    /// <summary>
    /// An exception that is thrown when the type of the message cannot be determined.
    /// </summary>
    [Serializable]
    public class UnknownMessageTypeException : Exception
    {
        /// <summary>
        /// </summary>
        public UnknownMessageTypeException() { }

        /// <summary>
        /// </summary>
        public UnknownMessageTypeException(String message) : base(message) { }

        /// <summary>
        /// </summary>
        public UnknownMessageTypeException(String message, Exception inner) : base(message, inner) { }
        
        /// <summary>
        /// </summary>
        protected UnknownMessageTypeException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
