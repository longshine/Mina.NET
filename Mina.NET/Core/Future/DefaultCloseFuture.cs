using System;
using Mina.Core.Session;

namespace Mina.Core.Future
{
    /// <summary>
    /// A default implementation of <see cref="ICloseFuture"/>.
    /// </summary>
    public class DefaultCloseFuture : DefaultIoFuture, ICloseFuture
    {
        public DefaultCloseFuture(IoSession session)
            : base(session)
        { }

        public Boolean Closed
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

        public new ICloseFuture Await()
        {
            return (ICloseFuture)base.Await();
        }
    }
}
