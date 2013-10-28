using System;

namespace Mina.Core.Filterchain
{
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
    }
}
