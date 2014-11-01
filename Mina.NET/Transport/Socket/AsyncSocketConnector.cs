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
        /// <summary>
        /// Instantiates.
        /// </summary>
        public AsyncSocketConnector()
            : base(new DefaultSocketSessionConfig())
        { }

        /// <inheritdoc/>
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
            SocketAsyncEventArgs e = new SocketAsyncEventArgs();
            e.Completed += new EventHandler<SocketAsyncEventArgs>(SocketAsyncEventArgs_Completed);
            e.RemoteEndPoint = connector.RemoteEP;
            e.UserToken = connector;
            Boolean willRaiseEvent = connector.Socket.ConnectAsync(e);
            if (!willRaiseEvent)
                ProcessConnect(e);
        }

        void SocketAsyncEventArgs_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Connect:
                    ProcessConnect(e);
                    break;
                case SocketAsyncOperation.Receive:
                    ((AsyncSocketSession)e.UserToken).ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                case SocketAsyncOperation.SendPackets:
                    ((AsyncSocketSession)e.UserToken).ProcessSend(e);
                    break;
            }
        }

        private void ProcessConnect(SocketAsyncEventArgs e)
        {
            ConnectorContext connector = (ConnectorContext)e.UserToken;
            if (e.SocketError == SocketError.Success)
            {
                SocketAsyncEventArgs readBuffer = e;
                readBuffer.AcceptSocket = null;
                readBuffer.RemoteEndPoint = null;
                readBuffer.SetBuffer(new Byte[SessionConfig.ReadBufferSize], 0, SessionConfig.ReadBufferSize);

                SocketAsyncEventArgs writeBuffer = new SocketAsyncEventArgs();
                writeBuffer.SetBuffer(new Byte[SessionConfig.ReadBufferSize], 0, SessionConfig.ReadBufferSize);
                writeBuffer.Completed += new EventHandler<SocketAsyncEventArgs>(SocketAsyncEventArgs_Completed);

                EndConnect(new AsyncSocketSession(this, Processor, connector.Socket,
                    new SocketAsyncEventArgsBuffer(readBuffer), new SocketAsyncEventArgsBuffer(writeBuffer),
                    ReuseBuffer), connector);
            }
            else
            {
                EndConnect(new SocketException((Int32)e.SocketError), connector);
            }
        }
    }
}
