using System.Net;
using Mina.Core.Service;

namespace Mina.Transport.Socket
{
    /// <summary>
    /// <see cref="IoConnector"/> for socket transport (UDP/IP)
    /// </summary>
    public interface IDatagramConnector : IoConnector
    {
        /// <inheritdoc/>
        new IDatagramSessionConfig SessionConfig { get; }
        /// <inheritdoc/>
        new IPEndPoint DefaultRemoteEndPoint { get; set; }
    }
}
