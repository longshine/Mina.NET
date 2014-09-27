using System.Net;
using Mina.Core.Service;

namespace Mina.Transport.Socket
{
    /// <summary>
    /// <see cref="IoConnector"/> for socket transport (TCP/IP).
    /// </summary>
    public interface ISocketConnector : IoConnector
    {
        /// <summary>
        /// Gets the default configuration of the new SocketSessions
        /// created by this connect service.
        /// </summary>
        new ISocketSessionConfig SessionConfig { get; }
        /// <inheritdoc/>
        new IPEndPoint DefaultRemoteEndPoint { get; set; }
    }
}
