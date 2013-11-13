using System;
using Mina.Core.Session;

namespace Mina.Filter.KeepAlive
{
    /// <summary>
    /// Provides keep-alive messages to <see cref="KeepAliveFilter"/>.
    /// </summary>
    public interface IKeepAliveMessageFactory
    {
        /// <summary>
        /// Returns <tt>true</tt> if and only if the specified message is a
        /// keep-alive request message.
        /// </summary>
        Boolean IsRequest(IoSession session, Object message);
        /// <summary>
        /// Returns <tt>true</tt> if and only if the specified message is a 
        /// keep-alive response message;
        /// </summary>
        Boolean IsResponse(IoSession session, Object message);
        /// <summary>
        /// Returns a (new) keep-alive request message.
        /// Returns <tt>null</tt> if no request is required.
        /// </summary>
        Object GetRequest(IoSession session);
        /// <summary>
        /// Returns a (new) response message for the specified keep-alive request.
        /// Returns <tt>null</tt> if no response is required.
        /// </summary>
        Object GetResponse(IoSession session, Object request);
    }
}
