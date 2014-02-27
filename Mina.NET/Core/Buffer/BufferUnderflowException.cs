using System;

namespace Mina.Core.Buffer
{
    [Serializable]
    public class BufferUnderflowException : Exception
    {
        public BufferUnderflowException() { }

        public BufferUnderflowException(String message)
            : base(message) { }

        public BufferUnderflowException(String message, Exception inner)
            : base(message, inner) { }

        protected BufferUnderflowException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }

        public override void GetObjectData(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }
}
