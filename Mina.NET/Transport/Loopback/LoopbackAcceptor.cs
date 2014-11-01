using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Mina.Core.Future;
using Mina.Core.Service;
using Mina.Core.Session;

namespace Mina.Transport.Loopback
{
    /// <summary>
    /// Binds the specified <see cref="IoHandler"/> to the specified
    /// <see cref="LoopbackEndPoint"/>.
    /// </summary>
    public class LoopbackAcceptor : AbstractIoAcceptor
    {
        internal static readonly Dictionary<EndPoint, LoopbackPipe> BoundHandlers
            = new Dictionary<EndPoint, LoopbackPipe>();
        private IdleStatusChecker _idleStatusChecker;

        /// <summary>
        /// Instantiates.
        /// </summary>
        public LoopbackAcceptor()
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
        protected override IEnumerable<EndPoint> BindInternal(IEnumerable<EndPoint> localEndPoints)
        {
            HashSet<EndPoint> newLocalEPs = new HashSet<EndPoint>();

            lock (BoundHandlers)
            {
                foreach (EndPoint ep in localEndPoints)
                {
                    LoopbackEndPoint localEP = ep as LoopbackEndPoint;
                    if (localEP == null || localEP.Port == 0)
                    {
                        localEP = null;
                        for (Int32 i = 10000; i < Int32.MaxValue; i++)
                        {
                            LoopbackEndPoint newLocalEP = new LoopbackEndPoint(i);
                            if (!BoundHandlers.ContainsKey(newLocalEP) && !newLocalEPs.Contains(newLocalEP))
                            {
                                localEP = newLocalEP;
                                break;
                            }
                        }

                        if (localEP == null)
                            throw new IOException("No port available.");
                    }
                    else if (localEP.Port < 0)
                    {
                        throw new IOException("Bind port number must be 0 or above.");
                    }
                    else if (BoundHandlers.ContainsKey(localEP))
                    {
                        throw new IOException("Address already bound: " + localEP);
                    }

                    newLocalEPs.Add(localEP);
                }

                foreach (LoopbackEndPoint localEP in newLocalEPs)
                {
                    if (BoundHandlers.ContainsKey(localEP))
                    {
                        foreach (LoopbackEndPoint ep in newLocalEPs)
                        {
                            BoundHandlers.Remove(ep);
                        }
                        throw new IOException("Duplicate local address: " + localEP);
                    }
                    else
                    {
                        BoundHandlers[localEP] = new LoopbackPipe(this, localEP, Handler);
                    }
                }
            }

            _idleStatusChecker.Start();

            return newLocalEPs;
        }

        /// <inheritdoc/>
        protected override void UnbindInternal(IEnumerable<EndPoint> localEndPoints)
        {
            lock (BoundHandlers)
            {
                foreach (EndPoint ep in localEndPoints)
                {
                    BoundHandlers.Remove(ep);
                }
            }

            if (BoundHandlers.Count == 0)
            {
                _idleStatusChecker.Stop();
            }
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

        internal void DoFinishSessionInitialization(IoSession session, IoFuture future)
        {
            InitSession(session, future, null);
        }
    }
}
