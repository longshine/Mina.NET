using System;

namespace Mina.Core.Future
{
    /// <summary>
    /// An <see cref="IoFuture"/> for asynchronous read requests.
    /// </summary>
    public interface IReadFuture : IoFuture
    {
        /// <summary>
        /// Gets or sets the received message.
        /// </summary>
        /// <remarks>
        /// Returns null if this future is not ready or the associated
        /// <see cref="Core.Session.IoSession"/> has been closed.
        /// All threads waiting for will be notified while being set.
        /// </remarks>
        Object Message { get; set; }
        /// <summary>
        /// Returns <code>true</code> if a message was received successfully.
        /// </summary>
        Boolean Read { get; }
        /// <summary>
        /// Gets or sets a value indicating if the <see cref="Core.Session.IoSession"/>
        /// associated with this future has been closed.
        /// </summary>
        Boolean Closed { get; set; }
        /// <summary>
        /// Gets or sets the cause of the read failure if and only if the read
        /// operation has failed due to an <see cref="Exception"/>.
        /// Otherwise null is returned.
        /// </summary>
        Exception Exception { get; set; }
        /// <inheritdoc/>
        new IReadFuture Await();
    }
}
