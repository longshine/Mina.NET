using System;
using System.Net.Sockets;
using Mina.Core.Service;

namespace Mina.Transport.Socket
{
    /// <summary>
    /// <see cref="IoConnector"/> for datagram transport (UDP/IP).
    /// </summary>
    public class AsyncDatagramConnector : AbstractSocketConnector, IDatagramConnector
    {
        public AsyncDatagramConnector()
            : base(new DefaultDatagramSessionConfig())
        { }

        public new IDatagramSessionConfig SessionConfig
        {
            get { return (IDatagramSessionConfig)base.SessionConfig; }
        }

        /// <inheritdoc/>
        public override ITransportMetadata TransportMetadata
        {
            get { return AsyncDatagramSession.Metadata; }
        }

        /// <inheritdoc/>
        protected override System.Net.Sockets.Socket NewSocket(AddressFamily addressFamily)
        {
            return new System.Net.Sockets.Socket(addressFamily, SocketType.Dgram, ProtocolType.Udp);
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

            EndConnect(new AsyncDatagramSession(this, Processor, connector.Socket, connector.RemoteEP,
                ReuseBuffer), connector);
        }
    }
}
