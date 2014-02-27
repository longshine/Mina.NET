using System;
using Mina.Core.Session;

namespace Mina.Core.Future
{
    /// <summary>
    /// A default implementation of <see cref="IConnectFuture"/>.
    /// </summary>
    public class DefaultConnectFuture : DefaultIoFuture, IConnectFuture
    {
        private static readonly Object CANCELED = new Object();

        public DefaultConnectFuture()
            : base(null)
        { }

        public Boolean Connected
        {
            get { return Value is IoSession; }
        }

        public Boolean Canceled
        {
            get { return Object.ReferenceEquals(Value, CANCELED); }
        }

        public Exception Exception
        {
            get
            {
                if (Done)
                {
                    return Value as Exception;
                }
                return null;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                Value = value;
            }
        }

        public override IoSession Session
        {
            get
            {
                Object val = Value;
                Exception ex = val as Exception;
                if (ex != null)
                    throw ex;
                else
                    return val as IoSession;
            }
        }

        public void SetSession(IoSession session)
        {
            if (session == null)
                throw new ArgumentNullException("session");
            Value = session;
        }

        public void Cancel()
        {
            Value = CANCELED;
        }

        public new IConnectFuture Await()
        {
            return (IConnectFuture)base.Await();
        }
    }
}
