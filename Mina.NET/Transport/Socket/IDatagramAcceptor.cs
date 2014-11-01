using System.Net;
using Mina.Core.Service;
using Mina.Core.Session;

namespace Mina.Transport.Socket
{
    /// <summary>
    /// <see cref="IoAcceptor"/> for socket transport (TCP/IP).  This class handles incoming TCP/IP based socket connections.
    /// </summary>
    public interface IDatagramAcceptor : IoAcceptor
    {
        /// <inheritdoc/>
        new IDatagramSessionConfig SessionConfig { get; }
        /// <inheritdoc/>
        new IPEndPoint LocalEndPoint { get; }
        /// <inheritdoc/>
        new IPEndPoint DefaultLocalEndPoint { get; set; }
        /// <summary>
        /// Gets or sets the <see cref="IoSessionRecycler"/> for this service.
        /// </summary>
        IoSessionRecycler SessionRecycler { get; set; }
    }
}
