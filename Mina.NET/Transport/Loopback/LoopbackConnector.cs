using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Mina.Core.Filterchain;
using Mina.Core.Future;
using Mina.Core.Service;
using Mina.Core.Session;
using Mina.Util;

namespace Mina.Transport.Loopback
{
    /// <summary>
    /// Connects to <see cref="IoHandler"/>s which is bound on the specified
    /// <see cref="LoopbackEndPoint"/>.
    /// </summary>
    public class LoopbackConnector : AbstractIoConnector
    {
        static readonly HashSet<LoopbackEndPoint> takenLocalEPs = new HashSet<LoopbackEndPoint>();
        static Int32 nextLocalPort = -1;
        private IdleStatusChecker _idleStatusChecker;

        /// <summary>
        /// Instantiates.
        /// </summary>
        public LoopbackConnector()
            : base(new DefaultLoopbackSessionConfig())
        {
            _idleStatusChecker = new IdleStatusChecker(() => ManagedSessions.Values);
        }

        /// <inheritdoc/>
        public override ITransportMetadata TransportMetadata
        {
            get { return LoopbackSession.Metadata; }
        }

        /// <inheritdoc/>
        protected override IConnectFuture Connect0(EndPoint remoteEP, EndPoint localEP, Action<IoSession, IConnectFuture> sessionInitializer)
        {
            LoopbackPipe entry;
            if (!LoopbackAcceptor.BoundHandlers.TryGetValue(remoteEP, out entry))
                return DefaultConnectFuture.NewFailedFuture(new IOException("Endpoint unavailable: " + remoteEP));

            DefaultConnectFuture future = new DefaultConnectFuture();

            // Assign the local end point dynamically,
            LoopbackEndPoint actualLocalEP;
            try
            {
                actualLocalEP = NextLocalEP();
            }
            catch (IOException e)
            {
                return DefaultConnectFuture.NewFailedFuture(e);
            }

            LoopbackSession localSession = new LoopbackSession(this, actualLocalEP, Handler, entry);

            InitSession(localSession, future, sessionInitializer);

            // and reclaim the local end point when the connection is closed.
            localSession.CloseFuture.Complete += ReclaimLocalEP;

            // initialize connector session
            try
            {
                IoFilterChain filterChain = localSession.FilterChain;
                this.FilterChainBuilder.BuildFilterChain(filterChain);

                // The following sentences don't throw any exceptions.
                IoServiceSupport serviceSupport = this as IoServiceSupport;
                if (serviceSupport != null)
                    serviceSupport.FireSessionCreated(localSession);
            }
            catch (Exception ex)
            {
                future.Exception = ex;
                return future;
            }

            // initialize acceptor session
            LoopbackSession remoteSession = localSession.RemoteSession;
            ((LoopbackAcceptor)remoteSession.Service).DoFinishSessionInitialization(remoteSession, null);
            try
            {
                IoFilterChain filterChain = remoteSession.FilterChain;
                entry.Acceptor.FilterChainBuilder.BuildFilterChain(filterChain);

                // The following sentences don't throw any exceptions.
                IoServiceSupport serviceSupport = entry.Acceptor as IoServiceSupport;
                if (serviceSupport != null)
                    serviceSupport.FireSessionCreated(remoteSession);
            }
            catch (Exception ex)
            {
                ExceptionMonitor.Instance.ExceptionCaught(ex);
                remoteSession.Close(true);
            }

            // Start chains, and then allow and messages read/written to be processed. This is to ensure that
            // sessionOpened gets received before a messageReceived
            ((LoopbackFilterChain)localSession.FilterChain).Start();
            ((LoopbackFilterChain)remoteSession.FilterChain).Start();

            return future;
        }

        /// <inheritdoc/>
        protected override void Dispose(Boolean disposing)
        {
            if (disposing)
            {
                _idleStatusChecker.Dispose();
            }
            base.Dispose(disposing);
        }

        private static LoopbackEndPoint NextLocalEP()
        {
            lock (takenLocalEPs)
            {
                if (nextLocalPort >= 0)
                    nextLocalPort = -1;
                for (Int32 i = 0; i < Int32.MaxValue; i++)
                {
                    LoopbackEndPoint answer = new LoopbackEndPoint(nextLocalPort--);
                    if (!takenLocalEPs.Contains(answer))
                    {
                        takenLocalEPs.Add(answer);
                        return answer;
                    }
                }
            }

            throw new IOException("Can't assign a Loopback port.");
        }

        private static void ReclaimLocalEP(Object sender, IoFutureEventArgs e)
        {
            lock (takenLocalEPs)
            {
                takenLocalEPs.Remove((LoopbackEndPoint)e.Future.Session.LocalEndPoint);
            }
        }
    }
}
