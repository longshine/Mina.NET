using System.Net;

namespace Mina.Core.Service
{
    /// <summary>
    /// Accepts incoming connection, communicates with clients, and fires events to <see cref="IoHandler"/>s.
    /// </summary>
    public interface IoAcceptor : IoService
    {
        void Bind(EndPoint localEP);
        void Unbind();
    }
}
