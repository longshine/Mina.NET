using System;
using Mina.Util;

namespace Mina.Transport.Socket
{
    public class AsyncSocketAcceptor : AbstractSocketAcceptor
    {
        public AsyncSocketAcceptor()
            : this(1024)
        { }

        public AsyncSocketAcceptor(Int32 maxConnections)
            : base(maxConnections)
        { }

        protected override void BeginAccept(Object state)
        {
            _listenSocket.BeginAccept(AcceptCallback, _listenSocket);
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            System.Net.Sockets.Socket listener = (System.Net.Sockets.Socket)ar.AsyncState;
            SocketSession session;

            try
            {
                System.Net.Sockets.Socket socket = listener.EndAccept(ar);
                session = new AsyncSocketSession(this, this, socket);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (Exception ex)
            {
                ExceptionMonitor.Instance.ExceptionCaught(ex);
                session = null;
            }

            EndAccept(session, null);
        }
    }
}
