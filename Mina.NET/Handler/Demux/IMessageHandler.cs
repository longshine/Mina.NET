using System;
using Mina.Core.Session;

namespace Mina.Handler.Demux
{
    /// <summary>
    /// A handler interface that <see cref="DemuxingIoHandler"/> forwards
    /// <tt>MessageReceived</tt> or <tt>MessageSent</tt> events to.
    /// </summary>
    public interface IMessageHandler
    {
        /// <summary>
        /// Invoked when the specific type of message is received from or sent to
        /// the specified <code>session</code>.
        /// </summary>
        /// <param name="session">the associated <see cref="IoSession"/></param>
        /// <param name="message">the message to decode</param>
        void HandleMessage(IoSession session, Object message);
    }

    /// <summary>
    /// A handler interface that <see cref="DemuxingIoHandler"/> forwards
    /// <tt>MessageReceived</tt> or <tt>MessageSent</tt> events to.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IMessageHandler<in T> : IMessageHandler
    {
        /// <summary>
        /// Invoked when the specific type of message is received from or sent to
        /// the specified <code>session</code>.
        /// </summary>
        /// <param name="session">the associated <see cref="IoSession"/></param>
        /// <param name="message">the message to decode. Its type is set by the implementation</param>
        void HandleMessage(IoSession session, T message);
    }
}
