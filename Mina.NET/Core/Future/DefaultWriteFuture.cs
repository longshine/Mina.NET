using System;
using Mina.Core.Session;

namespace Mina.Core.Future
{
    /// <summary>
    /// A default implementation of <see cref="IWriteFuture"/>.
    /// </summary>
    public class DefaultWriteFuture : DefaultIoFuture, IWriteFuture
    {
        public static IWriteFuture NewWrittenFuture(IoSession session)
        {
            DefaultWriteFuture writtenFuture = new DefaultWriteFuture(session);
            writtenFuture.Written = true;
            return writtenFuture;
        }

        public static IWriteFuture NewNotWrittenFuture(IoSession session, Exception cause)
        {
            DefaultWriteFuture unwrittenFuture = new DefaultWriteFuture(session);
            unwrittenFuture.Exception = cause;
            return unwrittenFuture;
        }

        public DefaultWriteFuture(IoSession session)
            : base(session)
        { }

        public Boolean Written
        {
            get
            {
                if (Done)
                {
                    Object v = Value;
                    if (v is Boolean)
                        return (Boolean)v;
                }
                return false;
            }
            set { Value = true; }
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

        public new IWriteFuture Await()
        {
            return (IWriteFuture)base.Await();
        }
    }
}
