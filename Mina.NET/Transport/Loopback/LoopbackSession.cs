using System;
using System.Collections.Concurrent;
using System.Net;
using Mina.Core.Filterchain;
using Mina.Core.Service;
using Mina.Core.Session;

namespace Mina.Transport.Loopback
{
    /// <summary>
    /// A <see cref="IoSession"/> for loopback transport.
    /// </summary>
    class LoopbackSession : AbstractIoSession
    {
        public static readonly ITransportMetadata Metadata
            = new DefaultTransportMetadata("mina", "loopback", false, false, typeof(LoopbackEndPoint));

        private readonly LoopbackEndPoint _localEP;
        private readonly LoopbackEndPoint _remoteEP;
        private readonly LoopbackFilterChain _filterChain;
        private readonly ConcurrentQueue<Object> _receivedMessageQueue;
        private readonly LoopbackSession _remoteSession;
        private readonly Object _lock;

        /// <summary>
        /// Constructor for client-side session.
        /// </summary>
        public LoopbackSession(IoService service, LoopbackEndPoint localEP,
            IoHandler handler, LoopbackPipe remoteEntry)
            : base(service)
        {
            Config = new DefaultLoopbackSessionConfig();
            _lock = new Byte[0];
            _localEP = localEP;
            _remoteEP = remoteEntry.Endpoint;
            _filterChain = new LoopbackFilterChain(this);
            _receivedMessageQueue = new ConcurrentQueue<Object>();
            _remoteSession = new LoopbackSession(this, remoteEntry);
        }

        /// <summary>
        /// Constructor for server-side session.
        /// </summary>
        public LoopbackSession(LoopbackSession remoteSession, LoopbackPipe entry)
            : base(entry.Acceptor)
        {
            Config = new DefaultLoopbackSessionConfig();
            _lock = remoteSession._lock;
            _localEP = remoteSession._remoteEP;
            _remoteEP = remoteSession._localEP;
            _filterChain = new LoopbackFilterChain(this);
            _remoteSession = remoteSession;
            _receivedMessageQueue = new ConcurrentQueue<Object>();
        }

        public override IoProcessor Processor
        {
            get { return _filterChain.Processor; }
        }

        public override IoFilterChain FilterChain
        {
            get { return _filterChain; }
        }

        public override EndPoint LocalEndPoint
        {
            get { return _localEP; }
        }

        public override EndPoint RemoteEndPoint
        {
            get { return _remoteEP; }
        }

        public override ITransportMetadata TransportMetadata
        {
            get { return Metadata; }
        }

        public LoopbackSession RemoteSession
        {
            get { return _remoteSession; }
        }

        internal ConcurrentQueue<Object> ReceivedMessageQueue
        {
            get { return _receivedMessageQueue; }
        }

        internal Object Lock
        {
            get { return _lock; }
        }
    }
}
