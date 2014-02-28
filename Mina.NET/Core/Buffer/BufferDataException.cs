using System;

namespace Mina.Core.Buffer
{
    /// <summary>
    /// An exception which is thrown when the data the <see cref="IoBuffer"/> contains is corrupt.
    /// </summary>
    [Serializable]
    public class BufferDataException : Exception
    {
        /// <summary>
        /// </summary>
        public BufferDataException() { }

        /// <summary>
        /// </summary>
        public BufferDataException(String message)
            : base(message) { }

        /// <summary>
        /// </summary>
        public BufferDataException(String message, Exception inner)
            : base(message, inner) { }

        /// <summary>
        /// </summary>
        protected BufferDataException(
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
