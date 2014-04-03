using Mina.Core.Service;
using Mina.Core.Session;

namespace Mina.Transport.Socket
{
    /// <summary>
    /// <see cref="IoAcceptor"/> for socket transport (TCP/IP).  This class handles incoming TCP/IP based socket connections.
    /// </summary>
    public interface IDatagramAcceptor : IoAcceptor
    {
        /// <summary>
        /// Gets or sets the <see cref="IoSessionRecycler"/> for this service.
        /// </summary>
        IoSessionRecycler SessionRecycler { get; set; }
    }
}
