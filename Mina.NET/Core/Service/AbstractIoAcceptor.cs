using System.Net;
using Mina.Core.Session;

namespace Mina.Core.Service
{
    public abstract class AbstractIoAcceptor : AbstractIoService, IoAcceptor
    {
        public AbstractIoAcceptor(IoSessionConfig sessionConfig)
            : base(sessionConfig)
        { }

        public abstract void Bind(EndPoint localEP);
    }
}
