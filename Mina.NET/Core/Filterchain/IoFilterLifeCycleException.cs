using System;

namespace Mina.Core.Filterchain
{
    /// <summary>
    /// An exception thrown when <see cref="IoFilter.Init()"/> or
    /// <see cref="IoFilter.OnPostAdd(IoFilterChain, String, INextFilter)"/> failed.
    /// </summary>
    [Serializable]
    public class IoFilterLifeCycleException : Exception
    {
        /// <summary>
        /// </summary>
        public IoFilterLifeCycleException()
        { }

        /// <summary>
        /// </summary>
        public IoFilterLifeCycleException(String message)
            : base(message)
        { }

        /// <summary>
        /// </summary>
        public IoFilterLifeCycleException(String message, Exception innerException)
            : base(message, innerException)
        { }

        /// <summary>
        /// </summary>
        protected IoFilterLifeCycleException(
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
