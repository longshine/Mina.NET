using System;
using System.Net;
using Mina.Core.Future;
using Mina.Core.Session;

namespace Mina.Core.Service
{
    /// <summary>
    /// A base implementation of <see cref="IoConnector"/>.
    /// </summary>
    public abstract class AbstractIoConnector : AbstractIoService, IoConnector
    {
        private Int64 _connectTimeoutInMillis = 60000L;
        private EndPoint _defaultRemoteEP;

        /// <summary>
        /// </summary>
        protected AbstractIoConnector(IoSessionConfig sessionConfig)
            : base(sessionConfig)
        { }

        /// <inheritdoc/>
        public EndPoint DefaultRemoteEndPoint
        {
            get { return _defaultRemoteEP; }
            set { _defaultRemoteEP = value; }
        }

        /// <inheritdoc/>
        public Int32 ConnectTimeout
        {
            get { return (Int32)(_connectTimeoutInMillis / 1000L); }
            set { _connectTimeoutInMillis = value * 1000L; }
        }

        /// <inheritdoc/>
        public Int64 ConnectTimeoutInMillis
        {
            get { return _connectTimeoutInMillis; }
            set { _connectTimeoutInMillis = value; }
        }

        /// <inheritdoc/>
        public IConnectFuture Connect()
        {
            if (_defaultRemoteEP == null)
                throw new InvalidOperationException("DefaultRemoteEndPoint is not set.");
            return Connect(_defaultRemoteEP, null, null);
        }

        /// <inheritdoc/>
        public IConnectFuture Connect(Action<IoSession, IConnectFuture> sessionInitializer)
        {
            if (_defaultRemoteEP == null)
                throw new InvalidOperationException("DefaultRemoteEndPoint is not set.");
            return Connect(_defaultRemoteEP, null, sessionInitializer);
        }

        /// <inheritdoc/>
        public IConnectFuture Connect(EndPoint remoteEP)
        {
            return Connect(remoteEP, null, null);
        }

        /// <inheritdoc/>
        public IConnectFuture Connect(EndPoint remoteEP, Action<IoSession, IConnectFuture> sessionInitializer)
        {
            return Connect(remoteEP, null, sessionInitializer);
        }

        /// <inheritdoc/>
        public IConnectFuture Connect(EndPoint remoteEP, EndPoint localEP)
        {
            return Connect(remoteEP, localEP, null);
        }

        /// <inheritdoc/>
        public IConnectFuture Connect(EndPoint remoteEP, EndPoint localEP, Action<IoSession, IConnectFuture> sessionInitializer)
        {
            if (remoteEP == null)
                throw new ArgumentNullException("remoteEP");

            return Connect0(remoteEP, localEP, sessionInitializer);
        }

        /// <summary>
        /// Implement this method to perform the actual connect operation.
        /// </summary>
        protected abstract IConnectFuture Connect0(EndPoint remoteEP, EndPoint localEP, Action<IoSession, IConnectFuture> sessionInitializer);

        /// <inheritdoc/>
        protected override void FinishSessionInitialization0(IoSession session, IoFuture future)
        {
            // In case that IConnectFuture.Cancel() is invoked before
            // SetSession() is invoked, add a listener that closes the
            // connection immediately on cancellation.
            future.Complete += (s, e) =>
            {
                if (((IConnectFuture)e.Future).Canceled)
                    session.Close(true);
            };
        }
    }
}
