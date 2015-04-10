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

        /// <summary>
        /// Returns a new <see cref="IConnectFuture"/> which is already marked as 'failed to connect'.
        /// </summary>
        public static IConnectFuture NewFailedFuture(Exception exception)
        {
            DefaultConnectFuture failedFuture = new DefaultConnectFuture();
            failedFuture.Exception = exception;
            return failedFuture;
        }

        /// <summary>
        /// </summary>
        public DefaultConnectFuture()
            : base(null)
        { }

        /// <inheritdoc/>
        public Boolean Connected
        {
            get { return Value is IoSession; }
        }

        /// <inheritdoc/>
        public Boolean Canceled
        {
            get { return Object.ReferenceEquals(Value, CANCELED); }
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public void SetSession(IoSession session)
        {
            if (session == null)
                throw new ArgumentNullException("session");
            Value = session;
        }

        /// <inheritdoc/>
        public virtual Boolean Cancel()
        {
            return SetValue(CANCELED);
        }

        /// <inheritdoc/>
        public new IConnectFuture Await()
        {
            return (IConnectFuture)base.Await();
        }
    }
}
