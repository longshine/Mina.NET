using System;

namespace Mina.Core.Filterchain
{
    [Serializable]
    public class IoFilterLifeCycleException : Exception
    {
        public IoFilterLifeCycleException()
        { }

        public IoFilterLifeCycleException(String message)
            : base(message)
        { }

        public IoFilterLifeCycleException(String message, Exception innerException)
            : base(message, innerException)
        { }

        protected IoFilterLifeCycleException(
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
