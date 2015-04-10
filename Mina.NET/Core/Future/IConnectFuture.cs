using System;
using Mina.Core.Session;

namespace Mina.Core.Future
{
    /// <summary>
    /// An <see cref="IoFuture"/> for asynchronous connect requests.
    /// </summary>
    public interface IConnectFuture : IoFuture
    {
        /// <summary>
        /// Returns <code>true</code> if the connect operation is finished successfully.
        /// </summary>
        Boolean Connected { get; }
        /// <summary>
        /// Returns <code>true</code> if the connect operation has been
        /// canceled by <see cref="Cancel()"/>.
        /// </summary>
        Boolean Canceled { get; }
        /// <summary>
        /// Gets or sets the cause of the connection failure.
        /// </summary>
        /// <remarks>
        /// Returns null if the connect operation is not finished yet,
        /// or if the connection attempt is successful.
        /// </remarks>
        Exception Exception { get; set; }
        /// <summary>
        /// Sets the newly connected session and notifies all threads waiting for this future.
        /// </summary>
        /// <param name="session"></param>
        void SetSession(IoSession session);
        /// <summary>
        /// Cancels the connection attempt and notifies all threads waiting for this future.
        /// <returns>
        /// <code>true</code> if the future has been cancelled by this call,
        /// <code>false</code>if the future was already cancelled.
        /// </returns>
        /// </summary>
        Boolean Cancel();
        /// <inheritdoc/>
        new IConnectFuture Await();
    }
}
