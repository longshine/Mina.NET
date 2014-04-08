using System;
using System.Net.Sockets;
using Mina.Core.Service;

namespace Mina.Transport.Socket
{
    /// <summary>
    /// <see cref="IoConnector"/> for socket transport (TCP/IP).
    /// </summary>
    public class AsyncSocketConnector : AbstractSocketConnector, ISocketConnector
    {
        public AsyncSocketConnector()
            : base(new DefaultSocketSessionConfig())
        { }

        public new ISocketSessionConfig SessionConfig
        {
            get { return (ISocketSessionConfig)base.SessionConfig; }
        }

        /// <inheritdoc/>
        public override ITransportMetadata TransportMetadata
        {
            get { return AsyncSocketSession.Metadata; }
        }

        /// <inheritdoc/>
        protected override System.Net.Sockets.Socket NewSocket(AddressFamily addressFamily)
        {
            return new System.Net.Sockets.Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);
        }

        /// <inheritdoc/>
        protected override void BeginConnect(ConnectorContext connector)
        {
            connector.Socket.BeginConnect(connector.RemoteEP, ConnectCallback, connector);
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            ConnectorContext connector = (ConnectorContext)ar.AsyncState;
            try
            {
                connector.Socket.EndConnect(ar);
            }
            catch (Exception ex)
            {
                EndConnect(ex, connector);
                return;
            }

            EndConnect(new AsyncSocketSession(this, Processor, connector.Socket, ReuseBuffer), connector);
        }
    }
}
