using System;
using System.Net;
using System.Net.Sockets;
using Mina.Util;

namespace Mina.Transport.Socket
{
    public class AsyncSocketAcceptor : AbstractSocketAcceptor
    {
        private System.Net.Sockets.Socket _listenSocket;

        public AsyncSocketAcceptor()
            : this(1024)
        { }

        public AsyncSocketAcceptor(Int32 maxConnections)
            : base(maxConnections)
        { }

        public override void Bind(EndPoint localEP)
        {
            _listenSocket = new System.Net.Sockets.Socket(localEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _listenSocket.Bind(localEP);
            _listenSocket.Listen(Backlog);

            StartAccept();

            _idleStatusChecker.Start();
        }

        public override void Unbind()
        {
            _idleStatusChecker.Stop();

            _listenSocket.Close();
            _listenSocket = null;
        }

        private void StartAccept()
        {
            _listenSocket.BeginAccept(AcceptCallback, null);
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            System.Net.Sockets.Socket socket;

            try
            {
                socket = _listenSocket.EndAccept(ar);

                SocketSession session = new AsyncSocketSession(this, this, socket);
                InitSession(session, null, null);
                session.Processor.Add(session);
            }
            catch (Exception ex)
            {
                ExceptionMonitor.Instance.ExceptionCaught(ex);
            }

            StartAccept();
        }
    }
}
