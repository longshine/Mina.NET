using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Core.Service
{
    /// <summary>
    /// An internal interface to represent an 'I/O processor' that performs
    /// actual I/O operations for <see cref="IoSession"/>s.
    /// </summary>
    public interface IoProcessor
    {
        /// <summary>
        /// Adds the specified <see cref="IoSession"/> to the I/O processor so that
        /// the I/O processor starts to perform any I/O operations related
        /// with the <see cref="IoSession"/>.
        /// </summary>
        /// <param name="session">the session to add</param>
        void Add(IoSession session);
        /// <summary>
        /// Writes the <see cref="IWriteRequest"/> for the specified <see cref="IoSession"/>.
        /// </summary>
        /// <param name="session">the session where the message to be written</param>
        /// <param name="writeRequest">thr message to write</param>
        void Write(IoSession session, IWriteRequest writeRequest);
        /// <summary>
        /// Flushes the internal write request queue of the specified <see cref="IoSession"/>.
        /// </summary>
        /// <param name="session">the session to flush</param>
        void Flush(IoSession session);
        /// <summary>
        /// Removes and closes the specified <see cref="IoSession"/> from the I/O
        ///  processor so that the I/O processor closes the connection
        ///  associated with the <see cref="IoSession"/> and releases any other
        ///  related resources.
        /// </summary>
        /// <param name="session">the session to remove</param>
        void Remove(IoSession session);
        /// <summary>
        /// Controls the traffic of the specified <paramref name="session"/>
        /// depending of the <see cref="IoSession.ReadSuspended"/>
        /// and <see cref="IoSession.WriteSuspended"/> flags.
        /// </summary>
        /// <param name="session">the session to control</param>
        void UpdateTrafficControl(IoSession session);
    }

    /// <summary>
    /// An internal interface to represent an 'I/O processor' that performs
    /// actual I/O operations for <typeparamref name="S"/>s.
    /// </summary>
    /// <typeparam name="S">the type of sessions</typeparam>
    public interface IoProcessor<in S> : IoProcessor
        where S : IoSession
    {
        /// <summary>
        /// Adds the specified <typeparamref name="S"/> to the I/O processor so that
        /// the I/O processor starts to perform any I/O operations related
        /// with the <typeparamref name="S"/>.
        /// </summary>
        /// <param name="session">the session to add</param>
        void Add(S session);
        /// <summary>
        /// Writes the <see cref="IWriteRequest"/> for the specified <typeparamref name="S"/>.
        /// </summary>
        /// <param name="session">the session we want the message to be written</param>
        /// <param name="writeRequest">the message to write</param>
        void Write(S session, IWriteRequest writeRequest);
        /// <summary>
        /// Flushes the internal write request queue of the specified <typeparamref name="S"/>.
        /// </summary>
        /// <param name="session">the session to flush</param>
        void Flush(S session);
        /// <summary>
        /// Removes and closes the specified <typeparamref name="S"/> from the I/O
        ///  processor so that the I/O processor closes the connection
        ///  associated with the <typeparamref name="S"/> and releases any other
        ///  related resources.
        /// </summary>
        /// <param name="session">the session to remove</param>
        void Remove(S session);
        /// <summary>
        /// Controls the traffic of the specified <paramref name="session"/>
        /// depending of the <see cref="IoSession.ReadSuspended"/>
        /// and <see cref="IoSession.WriteSuspended"/> flags.
        /// </summary>
        /// <param name="session">the session to control</param>
        void UpdateTrafficControl(S session);
    }
}
