using System;
using System.Net;
using Mina.Core.Service;

namespace Mina.Transport.Socket
{
    /// <summary>
    /// <see cref="IoAcceptor"/> for socket transport (TCP/IP).  This class handles incoming TCP/IP based socket connections.
    /// </summary>
    public interface ISocketAcceptor : IoAcceptor
    {
        /// <inheritdoc/>
        new ISocketSessionConfig SessionConfig { get; }
        /// <inheritdoc/>
        new IPEndPoint LocalEndPoint { get; }
        /// <inheritdoc/>
        new IPEndPoint DefaultLocalEndPoint { get; set; }
        /// <summary>
        /// Gets or sets the Reuse Address flag.
        /// </summary>
        Boolean ReuseAddress { get; set; }
        /// <summary>
        /// Gets or sets the size of the backlog. This can only be set when this class is not bound.
        /// </summary>
        Int32 Backlog { get; set; }
    }
}
