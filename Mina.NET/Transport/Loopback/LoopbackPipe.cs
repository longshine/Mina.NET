using Mina.Core.Service;

namespace Mina.Transport.Loopback
{
    class LoopbackPipe
    {
        private readonly LoopbackAcceptor _acceptor;
        private readonly LoopbackEndPoint _endpoint;
        private readonly IoHandler _handler;

        public LoopbackPipe(LoopbackAcceptor acceptor, LoopbackEndPoint endpoint, IoHandler handler)
        {
            _acceptor = acceptor;
            _endpoint = endpoint;
            _handler = handler;
        }

        public LoopbackAcceptor Acceptor
        {
            get { return _acceptor; }
        }

        public LoopbackEndPoint Endpoint
        {
            get { return _endpoint; }
        }

        public IoHandler Handler
        {
            get { return _handler; }
        }
    }
}
