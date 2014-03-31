using System;
using System.Collections.Generic;
using System.Net;
using Mina.Core.Filterchain;
using Mina.Core.Service;
using Mina.Core.Write;

namespace Mina.Core.Session
{
    /// <summary>
    /// A dummy <see cref="IoSession"/> for unit-testing or non-network-use of
    /// the classes that depends on <see cref="IoSession"/>.
    /// </summary>
    public class DummySession : AbstractIoSession
    {
        private static readonly ITransportMetadata Metadata
            = new DefaultTransportMetadata("mina", "dummy", false, false, typeof(IPEndPoint));

        private volatile IoHandler _handler = new IoHandlerAdapter();
        private readonly IoProcessor<DummySession> _processor;
        private readonly IoFilterChain _filterChain;
        private volatile EndPoint _remoteAddress = AnonymousEndPoint.Instance;
        private volatile ITransportMetadata _transportMetadata = Metadata;

        /// <summary>
        /// </summary>
        public DummySession()
            : base(new DummyService(new DummyConfig()))
        {
            _processor = new DummyProcessor();
            _filterChain = new DefaultIoFilterChain(this);

            IoSessionDataStructureFactory factory = new DefaultIoSessionDataStructureFactory();
            AttributeMap = factory.GetAttributeMap(this);
            SetWriteRequestQueue(factory.GetWriteRequestQueue(this));
        }

        /// <inheritdoc/>
        public override IoProcessor Processor
        {
            get { return _processor; }
        }

        /// <inheritdoc/>
        public override IoHandler Handler
        {
            get { return _handler; }
        }

        /// <inheritdoc/>
        public override IoFilterChain FilterChain
        {
            get { return _filterChain; }
        }

        /// <inheritdoc/>
        public override EndPoint LocalEndPoint
        {
            get { return AnonymousEndPoint.Instance; }
        }

        /// <inheritdoc/>
        public override EndPoint RemoteEndPoint
        {
            get { return _remoteAddress; }
        }

        /// <inheritdoc/>
        public override ITransportMetadata TransportMetadata
        {
            get { return _transportMetadata; }
        }

        /// <summary>
        /// </summary>
        public void SetTransportMetadata(ITransportMetadata metadata)
        {
            _transportMetadata = metadata;
        }

        /// <summary>
        /// </summary>
        public void SetRemoteEndPoint(EndPoint ep)
        {
            _remoteAddress = ep;
        }

        /// <summary>
        /// </summary>
        public void SetHandler(IoHandler handler)
        {
            _handler = handler;
        }

        class DummyService : AbstractIoAcceptor
        {
            public DummyService(IoSessionConfig sessionConfig)
                : base(sessionConfig)
            { }

            public override ITransportMetadata TransportMetadata
            {
                get { return Metadata; }
            }

            protected override IEnumerable<EndPoint> BindInternal(IEnumerable<EndPoint> localEndPoints)
            {
                throw new NotSupportedException();
            }

            protected override void UnbindInternal(IEnumerable<EndPoint> localEndPoints)
            {
                throw new NotImplementedException();
            }
        }

        class DummyProcessor : IoProcessor<DummySession>
        {
            public void Add(DummySession session)
            {
                // Do nothing
            }

            public void Remove(DummySession session)
            {
                if (!session.CloseFuture.Closed)
                    session.FilterChain.FireSessionClosed();
            }

            public void Write(DummySession session, IWriteRequest writeRequest)
            {
                IWriteRequestQueue queue = session.WriteRequestQueue;
                queue.Offer(session, writeRequest);
                if (!session.WriteSuspended)
                    Flush(session);
            }

            public void Flush(DummySession session)
            {
                IWriteRequest req = session.WriteRequestQueue.Poll(session);

                // Chek that the request is not null. If the session has been closed,
                // we may not have any pending requests.
                if (req != null)
                {
                    Object m = req.Message;
                    session.FilterChain.FireMessageSent(req);
                }
            }

            public void UpdateTrafficControl(DummySession session)
            {
                // Do nothing
            }

            void IoProcessor.Write(IoSession session, IWriteRequest writeRequest)
            {
                Write((DummySession)session, writeRequest);
            }

            void IoProcessor.Flush(IoSession session)
            {
                Flush((DummySession)session);
            }

            void IoProcessor.Add(IoSession session)
            {
                Add((DummySession)session);
            }

            void IoProcessor.Remove(IoSession session)
            {
                Remove((DummySession)session);
            }

            void IoProcessor.UpdateTrafficControl(IoSession session)
            {
                UpdateTrafficControl((DummySession)session);
            }
        }

        class DummyConfig : AbstractIoSessionConfig
        {
            protected override void DoSetAll(IoSessionConfig config)
            {
                // Do nothing
            }
        }

        class AnonymousEndPoint : EndPoint
        {
            public static AnonymousEndPoint Instance = new AnonymousEndPoint();

            private AnonymousEndPoint() { }

            public override String ToString()
            {
                return "?";
            }
        }
    }
}
