using System;
using System.Net;
using System.Threading;
using Mina.Core.Buffer;
using Mina.Core.File;
using Mina.Core.Service;
using Mina.Core.Write;

namespace Mina.Transport.Socket
{
    /// <summary>
    /// An <see cref="Core.Session.IoSession"/> for datagram transport (UDP/IP).
    /// </summary>
    public partial class AsyncDatagramSession : SocketSession
    {

        /// <summary>
        /// Transport metadata for async datagram session.
        /// </summary>
        public static readonly ITransportMetadata Metadata
            = new DefaultTransportMetadata("async", "datagram", true, false, typeof(IPEndPoint));

        private readonly AsyncDatagramAcceptor.SocketContext _socketContext;
        private Int32 _scheduledForFlush;

        /// <summary>
        /// Creates a new acceptor-side session instance.
        /// </summary>
        internal AsyncDatagramSession(IoService service, IoProcessor<AsyncDatagramSession> processor,
            AsyncDatagramAcceptor.SocketContext ctx, EndPoint remoteEP, Boolean reuseBuffer)
            : base(service, processor, new DefaultDatagramSessionConfig(), ctx.Socket, ctx.Socket.LocalEndPoint, remoteEP, reuseBuffer)
        {
            _socketContext = ctx;
        }

        /// <inheritdoc/>
        public override ITransportMetadata TransportMetadata
        {
            get { return Metadata; }
        }

        internal AsyncDatagramAcceptor.SocketContext Context
        {
            get { return _socketContext; }
        }

        public Boolean IsScheduledForFlush
        {
            get { return _scheduledForFlush != 0; }
        }

        public Boolean ScheduledForFlush()
        {
            return Interlocked.CompareExchange(ref _scheduledForFlush, 1, 0) == 0;
        }

        public void UnscheduledForFlush()
        {
            Interlocked.Exchange(ref _scheduledForFlush, 0);
        }

        /// <inheritdoc/>
        protected override void BeginSend(IWriteRequest request, IoBuffer buf)
        {
            EndPoint destination = request.Destination;
            if (destination == null)
                destination = this.RemoteEndPoint;
            BeginSend(buf, destination);
        }

        /// <inheritdoc/>
        protected override void BeginSendFile(IWriteRequest request, IFileRegion file)
        {
            EndSend(new InvalidOperationException("Cannot send a file via UDP"));
        }
    }
}
