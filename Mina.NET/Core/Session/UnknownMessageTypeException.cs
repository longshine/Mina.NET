using System;

namespace Mina.Core.Session
{
    /// <summary>
    /// An exception that is thrown when the type of the message cannot be determined.
    /// </summary>
    [Serializable]
    public class UnknownMessageTypeException : Exception
    {
        public UnknownMessageTypeException() { }

        public UnknownMessageTypeException(String message) : base(message) { }

        public UnknownMessageTypeException(String message, Exception inner) : base(message, inner) { }

        protected UnknownMessageTypeException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
