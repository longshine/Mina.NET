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
        private readonly AsyncSocketProcessor _processor;

        protected AbstractSocketConnector()
            : base(new DefaultSocketSessionConfig())
        {
            _processor = new AsyncSocketProcessor(() => ManagedSessions.Values);
        }

        /// <inheritdoc/>
        public override ITransportMetadata TransportMetadata
        {
            get { return SocketSession.Metadata; }
        }

        /// <inheritdoc/>
        public new ISocketSessionConfig SessionConfig
        {
            get { return (ISocketSessionConfig)base.SessionConfig; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to reuse the read buffer
        /// sent to <see cref="SocketSession.FilterChain"/> by
        /// <see cref="Core.Filterchain.IoFilterChain.FireMessageReceived(Object)"/>.
        /// </summary>
        /// <remarks>
        /// If any thread model, i.e. an <see cref="Filter.Executor.ExecutorFilter"/>,
        /// is added before filters that process the incoming <see cref="Core.Buffer.IoBuffer"/>
        /// in <see cref="Core.Filterchain.IoFilter.MessageReceived(Core.Filterchain.INextFilter, IoSession, Object)"/>,
        /// this must be set to <code>false</code> to avoid undetermined state
        /// of the read buffer. The default value is <code>true</code>.
        /// </remarks>
        public Boolean ReuseBuffer { get; set; }

        protected IoProcessor<SocketSession> Processor
        {
            get { return _processor; }
        }

        /// <inheritdoc/>
        protected override IConnectFuture Connect0(EndPoint remoteEP, EndPoint localEP, Action<IoSession, IConnectFuture> sessionInitializer)
        {
            System.Net.Sockets.Socket socket = new System.Net.Sockets.Socket(remoteEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            if (localEP != null)
                socket.Bind(localEP);
            ConnectorContext ctx = new ConnectorContext(socket, remoteEP, sessionInitializer);
            BeginConnect(ctx);
            return ctx.Future;
        }

        /// <inheritdoc/>
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

            _processor.IdleStatusChecker.Start();
        }

        protected void EndConnect(Exception cause, ConnectorContext connector)
        {
            connector.Future.Exception = cause;
            connector.Socket.Close();
        }

        /// <inheritdoc/>
        protected override void Dispose(Boolean disposing)
        {
            if (disposing)
            {
                _processor.Dispose();
            }
            base.Dispose(disposing);
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
                    ((IDisposable)_socket).Dispose();
                    _future.Dispose();
                }
            }
        }
    }
}
