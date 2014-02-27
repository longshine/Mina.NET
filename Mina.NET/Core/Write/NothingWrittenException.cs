using System;

namespace Mina.Core.Write
{
    /// <summary>
    /// An exception which is thrown when one or more write requests resulted
    /// in no actual write operation.
    /// </summary>
    [Serializable]
    public class NothingWrittenException : WriteException
    {
        public NothingWrittenException(IWriteRequest request)
            : base(request)
        { }

        protected NothingWrittenException(
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
