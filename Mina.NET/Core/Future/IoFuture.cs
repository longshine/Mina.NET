using System;
using Mina.Core.Session;

namespace Mina.Core.Future
{
    /// <summary>
    /// Represents the completion of an asynchronous I/O operation on an <see cref="IoSession"/>.
    /// </summary>
    public interface IoFuture
    {
        /// <summary>
        /// Gets the <see cref="IoSession"/> which is associated with this future.
        /// </summary>
        IoSession Session { get; }
        /// <summary>
        /// Returns if the asynchronous operation is completed.
        /// </summary>
        Boolean Done { get; }
        /// <summary>
        /// Event that this future is completed.
        /// If the listener is added after the completion, the listener is directly notified.
        /// </summary>
        event EventHandler<IoFutureEventArgs> Complete;
        /// <summary>
        /// Wait for the asynchronous operation to complete.
        /// </summary>
        /// <returns>self</returns>
        IoFuture Await();
        /// <summary>
        /// Wait for the asynchronous operation to complete with the specified timeout.
        /// </summary>
        /// <returns><tt>true</tt> if the operation is completed</returns>
        Boolean Await(Int32 millisecondsTimeout);
    }

    /// <summary>
    /// Contains data for events of <see cref="IoFuture"/>.
    /// </summary>
    public class IoFutureEventArgs : EventArgs
    {
        private readonly IoFuture _future;

        /// <summary>
        /// </summary>
        public IoFutureEventArgs(IoFuture future)
        {
            _future = future;
        }

        /// <summary>
        /// Gets the associated future.
        /// </summary>
        public IoFuture Future
        {
            get { return _future; }
        }
    }
}
