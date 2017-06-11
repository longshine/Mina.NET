using System;
using System.IO;
using System.Net;
using System.Threading;
using Mina.Core.Buffer;
using Mina.Core.File;
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
        private readonly IoProcessor _processor;
        private readonly IoFilterChain _filterChain;
        private Int32 _writing;
        private Object _pendingReceivedMessage = dummy;

        /// <summary>
        /// </summary>
        protected SocketSession(IoService service, IoProcessor processor, IoSessionConfig config,
            System.Net.Sockets.Socket socket, EndPoint localEP, EndPoint remoteEP, Boolean reuseBuffer)
            : base(service)
        {
            _socket = socket;
            _localEP = localEP;
            _remoteEP = remoteEP;
            Config = config;
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
        public override bool Active
        {
            get { return _socket.Connected; }
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
        /// Gets or sets a value indicating whether to reuse the internal
        /// read buffer as the buffer sent to <see cref="SocketSession.FilterChain"/>
        /// by <see cref="Core.Filterchain.IoFilterChain.FireMessageReceived(Object)"/>.
        /// </summary>
        /// <remarks>
        /// If any thread model, i.e. an <see cref="Filter.Executor.ExecutorFilter"/>,
        /// is added before filters that process the incoming <see cref="Core.Buffer.IoBuffer"/>
        /// in <see cref="Core.Filterchain.IoFilter.MessageReceived(Core.Filterchain.INextFilter, Core.Session.IoSession, Object)"/>,
        /// this must be set to <code>false</code> since the internal read buffer
        /// will be reset every time a session begins to receive.
        /// </remarks>
        /// <seealso cref="AbstractSocketAcceptor.ReuseBuffer"/>
        public Boolean ReuseBuffer { get; set; }

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
                IFileRegion file = req.Message as IFileRegion;
                if (file == null)
                    EndSend(new InvalidOperationException("Don't know how to handle message of type '"
                            + req.Message.GetType().Name + "'.  Are you missing a protocol encoder?"),
                            true);
                else
                    BeginSendFile(req, file);
            }
            else if (buf.HasRemaining)
            {
                BeginSend(req, buf);
            }
            else
            {
                EndSend(0);
            }
        }

        /// <summary>
        /// Begins send operation.
        /// </summary>
        /// <param name="request">the current write request</param>
        /// <param name="buf">the buffer to send</param>
        protected abstract void BeginSend(IWriteRequest request, IoBuffer buf);

        /// <summary>
        /// Begins to send a file.
        /// </summary>
        /// <param name="request">the current write request</param>
        /// <param name="file">the file to send</param>
        protected abstract void BeginSendFile(IWriteRequest request, IFileRegion file);

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
                    IFileRegion file = req.Message as IFileRegion;
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

                    buf.Free();
                }
            }

            if (Socket.Connected)
                BeginSend();
        }

        /// <summary>
        /// Ends send operation.
        /// </summary>
        /// <param name="ex">the exception caught</param>
        protected void EndSend(Exception ex)
        {
            EndSend(ex, false);
        }

        /// <summary>
        /// Ends send operation.
        /// </summary>
        /// <param name="ex">the exception caught</param>
        /// <param name="discardWriteRequest">discard the current write quest or not</param>
        protected void EndSend(Exception ex, Boolean discardWriteRequest)
        {
            IWriteRequest req = CurrentWriteRequest;
            if (req != null)
            {
                req.Future.Exception = ex;
                if (discardWriteRequest)
                {
                    CurrentWriteRequest = null;
                    IoBuffer buf = req.Message as IoBuffer;
                    if (buf != null)
                        buf.Free();
                }
            }
            this.FilterChain.FireExceptionCaught(ex);
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

        /// <summary>
        /// Ends receive operation.
        /// </summary>
        /// <param name="ex">the exception caught</param>
        protected void EndReceive(Exception ex)
        {
            this.FilterChain.FireExceptionCaught(ex);
            if (Socket.Connected && !ReadSuspended)
                BeginReceive();
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
                this.FilterChain.FireExceptionCaught(ex);
            }
        }
    }
}
