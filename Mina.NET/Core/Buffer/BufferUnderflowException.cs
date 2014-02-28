using System;

namespace Mina.Core.Buffer
{
    /// <summary>
    /// An exception thrown when a <code>Get</code> operation reaches the source buffer's limit.
    /// </summary>
    [Serializable]
    public class BufferUnderflowException : Exception
    {
        /// <summary>
        /// </summary>
        public BufferUnderflowException() { }

        /// <summary>
        /// </summary>
        public BufferUnderflowException(String message)
            : base(message) { }

        /// <summary>
        /// </summary>
        public BufferUnderflowException(String message, Exception inner)
            : base(message, inner) { }

        /// <summary>
        /// </summary>
        protected BufferUnderflowException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }

        /// <summary>
        /// </summary>
        public override void GetObjectData(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }
}
