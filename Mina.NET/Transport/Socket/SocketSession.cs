using System;
using System.Net;
using System.Threading;
using Mina.Core.Buffer;
using Mina.Core.Filterchain;
using Mina.Core.Service;
using Mina.Core.Session;
using Mina.Core.Write;
using Mina.Util;

namespace Mina.Transport.Socket
{
    public abstract class SocketSession : AbstractIoSession
    {
        private readonly System.Net.Sockets.Socket _socket;
        private readonly IoProcessor<SocketSession> _processor;
        private readonly IoFilterChain _filterChain;
        private Int32 _writing;

        public SocketSession(IoService service, IoProcessor<SocketSession> processor, System.Net.Sockets.Socket socket)
            : base(service)
        {
            _socket = socket;
            _config = new SessionConfigImpl(socket);
            if (service.SessionConfig != null)
                _config.SetAll(service.SessionConfig);
            _processor = processor;
            _filterChain = new DefaultIoFilterChain(this);
        }

        public override IoProcessor Processor
        {
            get { return _processor; }
        }

        public override IoFilterChain FilterChain
        {
            get { return _filterChain; }
        }

        public override EndPoint LocalEndPoint
        {
            get { return _socket.LocalEndPoint; }
        }

        public override EndPoint RemoteEndPoint
        {
            get { return _socket.RemoteEndPoint; }
        }

        public System.Net.Sockets.Socket Socket
        {
            get { return _socket; }
        }

        public void Start()
        {
            BeginReceive();
        }

        public void Flush()
        {
            if (Interlocked.CompareExchange(ref _writing, 1, 0) > 0)
                return;
            BeginSend();
        }

        private void BeginSend()
        {
            IWriteRequestQueue writeRequestQueue = WriteRequestQueue;
            IWriteRequest req = writeRequestQueue.Poll(this);

            if (req == null)
            {
                Interlocked.Exchange(ref _writing, 0);
                return;
            }

            IoBuffer buf = req.Message as IoBuffer;

            if (buf == null)
            {
                throw new InvalidOperationException("Don't know how to handle message of type '"
                            + req.Message.GetType().Name + "'.  Are you missing a protocol encoder?");
            }
            else
            {
                CurrentWriteRequest = req;
                BeginSend(buf);
            }
        }

        protected abstract void BeginSend(IoBuffer buf);

        protected void EndSend(Int32 bytesTransferred)
        {
            this.IncreaseWrittenBytes(bytesTransferred, DateTime.Now);

            try
            {
                FireMessageSent();
            }
            catch (Exception ex)
            {
                ExceptionMonitor.Instance.ExceptionCaught(ex);
            }

            BeginSend();
        }

        protected abstract void BeginReceive();

        protected void EndReceive(IoBuffer buf)
        {
            FilterChain.FireMessageReceived(buf);

            BeginReceive();
        }

        private void FireMessageSent()
        {
            IWriteRequest req = CurrentWriteRequest;
            if (req != null)
            {
                CurrentWriteRequest = null;
                this.FilterChain.FireMessageSent(req);
            }
        }
    }
}
