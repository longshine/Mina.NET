using System;
using Mina.Core.Service;
using Mina.Core.Session;
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
            try
            {
                listener.Socket.BeginAccept(AcceptCallback, listener);
            }
            catch (ObjectDisposedException)
            {
                // do nothing
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                ExceptionMonitor.Instance.ExceptionCaught(ex);
            }
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            ListenerContext listener = (ListenerContext)ar.AsyncState;
            System.Net.Sockets.Socket socket;

            try
            {
                socket = listener.Socket.EndAccept(ar);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (Exception ex)
            {
                ExceptionMonitor.Instance.ExceptionCaught(ex);
                socket = null;
            }

            EndAccept(socket, listener);
        }

        protected override IoSession NewSession(IoProcessor<SocketSession> processor, System.Net.Sockets.Socket socket)
        {
            return new AsyncSocketSession(this, processor, socket, ReuseBuffer);
        }
    }
}
