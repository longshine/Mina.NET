using System;
using System.IO;
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
    /// <summary>
    /// Base implementation of <see cref="IoSession"/> for socket transport (TCP/IP).
    /// </summary>
    public abstract class SocketSession : AbstractIoSession
    {
        private static readonly Object dummy = IoBuffer.Wrap(new Byte[0]);
        private readonly System.Net.Sockets.Socket _socket;
        private readonly EndPoint _localEP;
        private readonly EndPoint _remoteEP;
        private readonly IoProcessor<SocketSession> _processor;
        private readonly IoFilterChain _filterChain;
        private Int32 _writing;
        private Object _pendingReceivedMessage = dummy;

        /// <summary>
        /// </summary>
        protected SocketSession(IoService service, IoProcessor<SocketSession> processor, System.Net.Sockets.Socket socket)
            : base(service)
        {
            _socket = socket;
            _localEP = socket.LocalEndPoint;
            _remoteEP = socket.RemoteEndPoint;
            Config = new SessionConfigImpl(socket);
            if (service.SessionConfig != null)
                Config.SetAll(service.SessionConfig);
            _processor = processor;
            _filterChain = new DefaultIoFilterChain(this);
        }

        /// <inheritdoc/>
        public override IoProcessor Processor
        {
            get { return _processor; }
        }

        /// <inheritdoc/>
        public override IoFilterChain FilterChain
        {
            get { return _filterChain; }
        }

        /// <inheritdoc/>
        public override EndPoint LocalEndPoint
        {
            get { return _localEP; }
        }

        /// <inheritdoc/>
        public override EndPoint RemoteEndPoint
        {
            get { return _remoteEP; }
        }

        /// <summary>
        /// Gets the <see cref="System.Net.Sockets.Socket"/>
        /// associated with this session.
        /// </summary>
        public System.Net.Sockets.Socket Socket
        {
            get { return _socket; }
        }

        /// <summary>
        /// Starts this session.
        /// </summary>
        public void Start()
        {
            if (ReadSuspended)
                return;

            if (_pendingReceivedMessage != null)
            {
                if (!Object.ReferenceEquals(_pendingReceivedMessage, dummy))
                    FilterChain.FireMessageReceived(_pendingReceivedMessage);
                _pendingReceivedMessage = null;
                BeginReceive();
            }
        }

        /// <summary>
        /// Flushes this session.
        /// </summary>
        public void Flush()
        {
            if (WriteSuspended)
                return;
            if (Interlocked.CompareExchange(ref _writing, 1, 0) > 0)
                return;
            BeginSend();
        }

        private void BeginSend()
        {
            IWriteRequest req = CurrentWriteRequest;
            if (req == null)
            {
                req = WriteRequestQueue.Poll(this);

                if (req == null)
                {
                    Interlocked.Exchange(ref _writing, 0);
                    return;
                }
                
                CurrentWriteRequest = req;
            }

            IoBuffer buf = req.Message as IoBuffer;

            if (buf == null)
            {
                FileInfo file = req.Message as FileInfo;
                if (file == null)
                    throw new InvalidOperationException("Don't know how to handle message of type '"
                            + req.Message.GetType().Name + "'.  Are you missing a protocol encoder?");
                else
                    BeginSendFile(file);
            }
            else
            {
                if (buf.HasRemaining)
                    BeginSend(buf);
                else
                    EndSend(0);
            }
        }

        /// <summary>
        /// Begins send operation.
        /// </summary>
        /// <param name="buf">the buffer to send</param>
        protected abstract void BeginSend(IoBuffer buf);

        /// <summary>
        /// Begins to send a file.
        /// </summary>
        /// <param name="file">the file to send</param>
        protected abstract void BeginSendFile(FileInfo file);

        /// <summary>
        /// Ends send operation.
        /// </summary>
        /// <param name="bytesTransferred">the bytes transferred in last send operation</param>
        protected void EndSend(Int32 bytesTransferred)
        {
            this.IncreaseWrittenBytes(bytesTransferred, DateTime.Now);

            IWriteRequest req = CurrentWriteRequest;
            if (req != null)
            {
                IoBuffer buf = req.Message as IoBuffer;
                if (buf == null)
                {
                    FileInfo file = req.Message as FileInfo;
                    if (file != null)
                    {
                        FireMessageSent(req);
                    }
                    else
                    {
                        // we only send buffers and files so technically it shouldn't happen
                    }
                }
                else if (!buf.HasRemaining)
                {
                    // Buffer has been sent, clear the current request.
                    Int32 pos = buf.Position;
                    buf.Reset();

                    FireMessageSent(req);

                    // And set it back to its position
                    buf.Position = pos;
                }
            }

            if (Socket.Connected)
                BeginSend();
        }

        /// <summary>
        /// Begins receive operation.
        /// </summary>
        protected abstract void BeginReceive();

        /// <summary>
        /// Ends receive operation.
        /// </summary>
        /// <param name="buf">the buffer received in last receive operation</param>
        protected void EndReceive(IoBuffer buf)
        {
            if (ReadSuspended)
            {
                _pendingReceivedMessage = buf;
            }
            else
            {
                FilterChain.FireMessageReceived(buf);

                if (Socket.Connected)
                    BeginReceive();
            }
        }

        private void FireMessageSent(IWriteRequest req)
        {
            CurrentWriteRequest = null;
            try
            {
                this.FilterChain.FireMessageSent(req);
            }
            catch (Exception ex)
            {
                ExceptionMonitor.Instance.ExceptionCaught(ex);
            }
        }
    }
}
