using System;
using System.Net.Sockets;
using Mina.Util;

namespace Mina.Transport.Socket
{
    public class AsyncSocketConnector : AbstractSocketConnector
    {
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
                    new SocketAsyncEventArgsBuffer(readBuffer), new SocketAsyncEventArgsBuffer(writeBuffer)), connector);
            }
            else
            {
                EndConnect(new SocketException((Int32)e.SocketError), connector);
            }
        }
    }
}
