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

        protected override void BeginAccept(ListenerContext listener)
        {
            listener.Socket.BeginAccept(AcceptCallback, listener);
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            ListenerContext listener = (ListenerContext)ar.AsyncState;
            SocketSession session;

            try
            {
                System.Net.Sockets.Socket socket = listener.Socket.EndAccept(ar);
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

            EndAccept(session, listener);
        }
    }
}
