using System;
using Mina.Core.Session;

namespace Mina.Core.Future
{
    /// <summary>
    /// A default implementation of <see cref="ICloseFuture"/>.
    /// </summary>
    public class DefaultCloseFuture : DefaultIoFuture, ICloseFuture
    {
        /// <summary>
        /// </summary>
        public DefaultCloseFuture(IoSession session)
            : base(session)
        { }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public new ICloseFuture Await()
        {
            return (ICloseFuture)base.Await();
        }
    }
}
