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
        void Add(IoSession session);
        void Write(IoSession session, IWriteRequest writeRequest);
        void Flush(IoSession session);
        void Remove(IoSession session);
    }

    public interface IoProcessor<S> : IoProcessor
        where S : IoSession
    {
        void Add(S session);
        /// <summary>
        /// Writes the WriteRequest for the specified session.
        /// </summary>
        /// <param name="session">the session we want the message to be written</param>
        /// <param name="writeRequest">the WriteRequest to write</param>
        void Write(S session, IWriteRequest writeRequest);
        void Flush(S session);
        void Remove(S session);
    }
}
