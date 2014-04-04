using System.Net;

namespace Mina.Core.Session
{
    /// <summary>
    ///  A connectionless transport can recycle existing sessions by assigning an
    ///  <see cref="IoSessionRecycler"/> to an <see cref="Core.Service.IoService"/>.
    /// </summary>
    public interface IoSessionRecycler
    {
        /// <summary>
        /// Called when the underlying transport creates or writes a new <see cref="IoSession"/>.
        /// </summary>
        /// <param name="session"></param>
        void Put(IoSession session);
        /// <summary>
        /// Attempts to retrieve a recycled <see cref="IoSession"/>.
        /// </summary>
        /// <param name="remoteEP">the remote endpoint of the <see cref="IoSession"/> the transport wants to recycle</param>
        /// <returns>a recycled <see cref="IoSession"/>, or null if one cannot be found</returns>
        IoSession Recycle(EndPoint remoteEP);
        /// <summary>
        /// Called when an <see cref="IoSession"/> is explicitly closed.
        /// </summary>
        /// <param name="session"></param>
        void Remove(IoSession session);
    }

    /// <summary>
    /// A dummy recycler that doesn't recycle any sessions.
    /// Using this recycler will make all session lifecycle events
    /// to be fired for every I/O for all connectionless sessions.
    /// </summary>
    public class NoopRecycler : IoSessionRecycler
    {
        /// <summary>
        /// A dummy recycler that doesn't recycle any sessions.
        /// </summary>
        public static readonly NoopRecycler Instance = new NoopRecycler();

        private NoopRecycler()
        { }

        /// <inheritdoc/>
        public void Put(IoSession session)
        {
            // do nothing
        }

        /// <inheritdoc/>
        public IoSession Recycle(EndPoint remoteEP)
        {
            return null;
        }

        /// <inheritdoc/>
        public void Remove(IoSession session)
        {
            // do nothing
        }
    }
}
