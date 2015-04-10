using System;
using System.Net;
using System.Net.Sockets;
using Mina.Core.Future;
using Mina.Core.Service;
using Mina.Core.Session;
using Mina.Util;

namespace Mina.Transport.Socket
{
    /// <summary>
    /// Base class of socket connector.
    /// </summary>
    public abstract class AbstractSocketConnector : AbstractIoConnector
    {
        private readonly AsyncSocketProcessor _processor;

        /// <summary>
        /// Instantiates.
        /// </summary>
        protected AbstractSocketConnector(IoSessionConfig sessionConfig)
            : base(sessionConfig)
        {
            _processor = new AsyncSocketProcessor(() => ManagedSessions.Values);
        }

        /// <inheritdoc/>
        public new IPEndPoint DefaultRemoteEndPoint
        {
            get { return (IPEndPoint)base.DefaultRemoteEndPoint; }
            set { base.DefaultRemoteEndPoint = value; }
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

        /// <summary>
        /// Gets the <see cref="IoProcessor"/>.
        /// </summary>
        protected IoProcessor<SocketSession> Processor
        {
            get { return _processor; }
        }

        /// <inheritdoc/>
        protected override IConnectFuture Connect0(EndPoint remoteEP, EndPoint localEP, Action<IoSession, IConnectFuture> sessionInitializer)
        {
            System.Net.Sockets.Socket socket = NewSocket(remoteEP.AddressFamily);
            if (localEP != null)
                socket.Bind(localEP);
            ConnectorContext ctx = new ConnectorContext(socket, remoteEP, sessionInitializer);
            BeginConnect(ctx);
            return ctx;
        }

        /// <summary>
        /// Creates a socket according to the address family.
        /// </summary>
        /// <param name="addressFamily">the <see cref="AddressFamily"/></param>
        /// <returns>the socket created</returns>
        protected abstract System.Net.Sockets.Socket NewSocket(AddressFamily addressFamily);

        /// <summary>
        /// Begins connecting.
        /// </summary>
        /// <param name="connector">the context of current connector</param>
        protected abstract void BeginConnect(ConnectorContext connector);

        /// <summary>
        /// Ends connecting.
        /// </summary>
        /// <param name="session">the connected session</param>
        /// <param name="connector">the context of current connector</param>
        protected void EndConnect(IoSession session, ConnectorContext connector)
        {
            try
            {
                InitSession(session, connector, connector.SessionInitializer);
                session.Processor.Add(session);
            }
            catch (Exception ex)
            {
                ExceptionMonitor.Instance.ExceptionCaught(ex);
            }

            _processor.IdleStatusChecker.Start();
        }

        /// <summary>
        /// Ends connecting.
        /// </summary>
        /// <param name="cause">the exception occurred</param>
        /// <param name="connector">the context of current connector</param>
        protected void EndConnect(Exception cause, ConnectorContext connector)
        {
            connector.Exception = cause;
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

        /// <summary>
        /// Provides context info for a socket connector.
        /// </summary>
        protected class ConnectorContext : DefaultConnectFuture
        {
            private readonly System.Net.Sockets.Socket _socket;
            private readonly EndPoint _remoteEP;
            private readonly Action<IoSession, IConnectFuture> _sessionInitializer;

            /// <summary>
            /// Instantiates.
            /// </summary>
            /// <param name="socket">the associated socket</param>
            /// <param name="remoteEP">the remote endpoint</param>
            /// <param name="sessionInitializer">the funciton to initialize session</param>
            public ConnectorContext(System.Net.Sockets.Socket socket, EndPoint remoteEP, Action<IoSession, IConnectFuture> sessionInitializer)
            {
                _socket = socket;
                _remoteEP = remoteEP;
                _sessionInitializer = sessionInitializer;
            }

            /// <summary>
            /// Gets the associated socket.
            /// </summary>
            public System.Net.Sockets.Socket Socket
            {
                get { return _socket; }
            }

            /// <summary>
            /// Gets the remote endpoint.
            /// </summary>
            public EndPoint RemoteEP
            {
                get { return _remoteEP; }
            }

            /// <summary>
            /// Gets the funciton to initialize session.
            /// </summary>
            public Action<IoSession, IConnectFuture> SessionInitializer
            {
                get { return _sessionInitializer; }
            }

            /// <inheritdoc/>
            public override Boolean Cancel()
            {
                Boolean justCancelled = base.Cancel();
                if (justCancelled)
                {
                    _socket.Close();
                }
                return justCancelled;
            }

            /// <inheritdoc/>
            protected override void Dispose(Boolean disposing)
            {
                if (disposing)
                {
                    ((IDisposable)_socket).Dispose();
                }
                base.Dispose(disposing);
            }
        }
    }
}
