using System;
using Mina.Core.Session;

namespace Mina.Core.Future
{
    /// <summary>
    /// A default implementation of <see cref="IReadFuture"/>.
    /// </summary>
    public class DefaultReadFuture : DefaultIoFuture, IReadFuture
    {
        private static readonly Object CLOSED = new Object();

        /// <summary>
        /// </summary>
        public DefaultReadFuture(IoSession session)
            : base(session)
        { }

        /// <inheritdoc/>
        public Object Message
        {
            get
            {
                if (Done)
                {
                    Object val = Value;
                    if (Object.ReferenceEquals(val, CLOSED))
                        return null;
                    Exception ex = val as Exception;
                    if (ex != null)
                        throw ex;
                    return val;
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
        public Boolean Read
        {
            get
            {
                if (Done)
                {
                    Object val = Value;
                    return !Object.ReferenceEquals(val, CLOSED) && !(val is Exception);
                }
                return false;
            }
        }

        /// <inheritdoc/>
        public Boolean Closed
        {
            get { return Done && Object.ReferenceEquals(Value, CLOSED); }
            set { Value = CLOSED; }
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
        public new IReadFuture Await()
        {
            return (IReadFuture)base.Await();
        }
    }
}
