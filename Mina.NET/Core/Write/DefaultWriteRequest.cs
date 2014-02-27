using System;
using Mina.Core.Future;
using Mina.Core.Session;

namespace Mina.Core.Write
{
    public class DefaultWriteRequest : IWriteRequest
    {
        private readonly Object _message;
        private readonly IWriteFuture _future;

        public DefaultWriteRequest(Object message)
            : this(message, null)
        { }

        public DefaultWriteRequest(Object message, IWriteFuture future)
        {
            _message = message;
            _future = future ?? UnusedFuture.Instance;
        }

        public IWriteRequest OriginalRequest
        {
            get { return this; }
        }

        public Object Message
        {
            get { return _message; }
        }

        public IWriteFuture Future
        {
            get { return _future; }
        }

        public virtual Boolean Encoded
        {
            get { return false; }
        }

        class UnusedFuture : IWriteFuture
        {
            public static readonly UnusedFuture Instance = new UnusedFuture();

            public event EventHandler<IoFutureEventArgs> Complete
            {
                add { throw new NotSupportedException(); }
                remove { throw new NotSupportedException(); }
            }

            public Boolean Written
            {
                get { return false; }
                set { }
            }

            public Exception Exception
            {
                get { return null; }
                set { }
            }

            public IoSession Session
            {
                get { return null; }
            }

            public Boolean Done
            {
                get { return true; }
            }

            public IWriteFuture Await()
            {
                return this;
            }

            public Boolean Await(Int32 timeoutMillis)
            {
                return true;
            }

            IoFuture IoFuture.Await()
            {
                return Await();
            }
        }
    }
}
