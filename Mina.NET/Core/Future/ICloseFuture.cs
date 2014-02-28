using System;

namespace Mina.Core.Future
{
    /// <summary>
    /// An <see cref="IoFuture"/> for asynchronous close requests.
    /// </summary>
    public interface ICloseFuture : IoFuture
    {
        /// <summary>
        /// Gets or sets a value indicating if the close request is finished and
        /// the associated <see cref="Core.Session.IoSession"/> been closed.
        /// </summary>
        Boolean Closed { get; set; }
        /// <inheritdoc/>
        new ICloseFuture Await();
    }
}
