using System;
using System.Net;
using System.Net.Sockets;
using Mina.Core.Future;
using Mina.Core.Service;
using Mina.Core.Session;
using Mina.Util;

namespace Mina.Transport.Socket
{
    public abstract class AbstractSocketConnector : AbstractIoConnector, ISocketConnector
    {
        protected readonly IoProcessor<SocketSession> _processor = new AsyncSocketProcessor();

        protected AbstractSocketConnector()
            : base(new DefaultSocketSessionConfig())
        { }

        public new ISocketSessionConfig SessionConfig
        {
            get { return (ISocketSessionConfig)base.SessionConfig; }
        }

        protected override IConnectFuture Connect0(EndPoint remoteEP, EndPoint localEP, Action<IoSession, IConnectFuture> sessionInitializer)
        {
            System.Net.Sockets.Socket socket = new System.Net.Sockets.Socket(remoteEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            if (localEP != null)
                socket.Bind(localEP);
            ConnectorContext ctx = new ConnectorContext(socket, remoteEP, sessionInitializer);
            BeginConnect(ctx);
            return ctx.Future;
        }

        protected abstract void BeginConnect(ConnectorContext connector);

        protected void EndConnect(IoSession session, ConnectorContext connector)
        {
            try
            {
                InitSession(session, connector.Future, connector.SessionInitializer);
                session.Processor.Add(session);
            }
            catch (Exception ex)
            {
                ExceptionMonitor.Instance.ExceptionCaught(ex);
            }
        }

        protected void EndConnect(Exception cause, ConnectorContext connector)
        {
            connector.Future.Exception = cause;
            connector.Socket.Close();
        }

        protected class ConnectorContext : IDisposable
        {
            private readonly System.Net.Sockets.Socket _socket;
            private readonly EndPoint _remoteEP;
            private readonly Action<IoSession, IConnectFuture> _sessionInitializer;
            private readonly DefaultConnectFuture _future = new DefaultConnectFuture();

            public ConnectorContext(System.Net.Sockets.Socket socket, EndPoint remoteEP, Action<IoSession, IConnectFuture> sessionInitializer)
            {
                _socket = socket;
                _remoteEP = remoteEP;
                _sessionInitializer = sessionInitializer;
            }

            public System.Net.Sockets.Socket Socket
            {
                get { return _socket; }
            }

            public EndPoint RemoteEP
            {
                get { return _remoteEP; }
            }

            public IConnectFuture Future
            {
                get { return _future; }
            }

            public Action<IoSession, IConnectFuture> SessionInitializer
            {
                get { return _sessionInitializer; }
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(Boolean disposing)
            {
                if (disposing)
                {
                    _socket.Dispose();
                    _future.Dispose();
                }
            }
        }
    }
}
