using System;
using Mina.Core.Service;

namespace Mina.Transport.Socket
{
    /// <summary>
    /// <see cref="IoAcceptor"/> for socket transport (TCP/IP).  This class handles incoming TCP/IP based socket connections.
    /// </summary>
    public interface ISocketAcceptor : IoAcceptor
    {
        Boolean ReuseAddress { get; set; }
        /// <summary>
        /// Gets or sets the size of the backlog. This can only be set when this class is not bound.
        /// </summary>
        Int32 Backlog { get; set; }
    }
}
