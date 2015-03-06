using System;
using System.Net;
using System.Net.Sockets;
using Mina.Core.Buffer;
using Mina.Core.File;
using Mina.Core.Filterchain;
using Mina.Core.Service;
using Mina.Core.Write;
using Mina.Filter.Ssl;
using Mina.Util;

namespace Mina.Transport.Socket
{
    /// <summary>
    /// An <see cref="Core.Session.IoSession"/> for socket transport (TCP/IP).
    /// </summary>
    public class AsyncSocketSession : SocketSession
    {
        /// <summary>
        /// Transport metadata for async socket session.
        /// </summary>
        public static readonly ITransportMetadata Metadata
            = new DefaultTransportMetadata("async", "socket", false, true, typeof(IPEndPoint));

        private readonly SocketAsyncEventArgsBuffer _readBuffer;
        private readonly SocketAsyncEventArgsBuffer _writeBuffer;
        private readonly EventHandler<SocketAsyncEventArgs> _completeHandler;

        /// <summary>
        /// Instantiates.
        /// </summary>
        /// <param name="service">the service this session belongs to</param>
        /// <param name="processor">the processor to process this session</param>
        /// <param name="socket">the associated socket</param>
        /// <param name="readBuffer">the <see cref="SocketAsyncEventArgsBuffer"/> as reading buffer</param>
        /// <param name="writeBuffer">the <see cref="SocketAsyncEventArgsBuffer"/> as writing buffer</param>
        /// <param name="reuseBuffer">whether or not reuse internal buffer, see <seealso cref="SocketSession.ReuseBuffer"/> for more</param>
        public AsyncSocketSession(IoService service, IoProcessor<SocketSession> processor, System.Net.Sockets.Socket socket,
            SocketAsyncEventArgsBuffer readBuffer, SocketAsyncEventArgsBuffer writeBuffer, Boolean reuseBuffer)
            : base(service, processor, new SessionConfigImpl(socket), socket, socket.LocalEndPoint, socket.RemoteEndPoint, reuseBuffer)
        {
            _readBuffer = readBuffer;
            _readBuffer.SocketAsyncEventArgs.UserToken = this;
            _writeBuffer = writeBuffer;
            _writeBuffer.SocketAsyncEventArgs.UserToken = this;
            _completeHandler = saea_Completed;
        }

        /// <summary>
        /// Gets the reading buffer belonged to this session.
        /// </summary>
        public SocketAsyncEventArgsBuffer ReadBuffer
        {
            get { return _readBuffer; }
        }

        /// <summary>
        /// Gets the writing buffer belonged to this session.
        /// </summary>
        public SocketAsyncEventArgsBuffer WriteBuffer
        {
            get { return _writeBuffer; }
        }

        /// <inheritdoc/>
        public override ITransportMetadata TransportMetadata
        {
            get { return Metadata; }
        }

        /// <inheritdoc/>
        public override Boolean Secured
        {
            get
            {
                IoFilterChain chain = this.FilterChain;
                SslFilter sslFilter = (SslFilter)chain.Get(typeof(SslFilter));
                return sslFilter != null && sslFilter.IsSslStarted(this);
            }
        }

        /// <inheritdoc/>
        protected override void BeginSend(IWriteRequest request, IoBuffer buf)
        {
            SocketAsyncEventArgs saea;
            SocketAsyncEventArgsBuffer saeaBuf = buf as SocketAsyncEventArgsBuffer;
            if (saeaBuf == null)
            {
                _writeBuffer.Clear();
                if (_writeBuffer.Remaining < buf.Remaining)
                {
                    Int32 oldLimit = buf.Limit;
                    buf.Limit = buf.Position + _writeBuffer.Remaining;
                    _writeBuffer.Put(buf);
                    buf.Limit = oldLimit;
                }
                else
                {
                    _writeBuffer.Put(buf);
                }
                _writeBuffer.Flip();
                _writeBuffer.SetBuffer();
                saea = _writeBuffer.SocketAsyncEventArgs;
            }
            else
            {
                saea = saeaBuf.SocketAsyncEventArgs;
                saea.Completed += _completeHandler;
            }

            Boolean willRaiseEvent;
            try
            {
                willRaiseEvent = Socket.SendAsync(saea);
            }
            catch (ObjectDisposedException)
            {
                // do nothing
                return;
            }
            catch (Exception ex)
            {
                EndSend(ex);
                return;
            }
            if (!willRaiseEvent)
            {
                ProcessSend(saea);
            }
        }

        /// <inheritdoc/>
        protected override void BeginSendFile(IWriteRequest request, IFileRegion file)
        {
            SocketAsyncEventArgs saea = _writeBuffer.SocketAsyncEventArgs;
            saea.SendPacketsElements = new SendPacketsElement[] {
                new SendPacketsElement(file.FullName)
            };

            Boolean willRaiseEvent;
            try
            {
                willRaiseEvent = Socket.SendPacketsAsync(saea);
            }
            catch (ObjectDisposedException)
            {
                // do nothing
                return;
            }
            catch (Exception ex)
            {
                EndSend(ex);
                return;
            }
            if (!willRaiseEvent)
            {
                ProcessSend(saea);
            }
        }

        void saea_Completed(Object sender, SocketAsyncEventArgs e)
        {
            e.Completed -= _completeHandler;
            ProcessSend(e);
        }

        /// <summary>
        /// Processes send events.
        /// </summary>
        /// <param name="e"></param>
        public void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                EndSend(e.BytesTransferred);
            }
            else if (e.SocketError != SocketError.OperationAborted
                && e.SocketError != SocketError.Interrupted
                && e.SocketError != SocketError.ConnectionReset)
            {
                EndSend(new SocketException((Int32)e.SocketError));
            }
            else
            {
                // closed
                Processor.Remove(this);
            }
        }

        /// <inheritdoc/>
        protected override void BeginReceive()
        {
            _readBuffer.Clear();

            Boolean willRaiseEvent;
            try
            {
                willRaiseEvent = Socket.ReceiveAsync(_readBuffer.SocketAsyncEventArgs);
            }
            catch (ObjectDisposedException)
            {
                // do nothing
                return;
            }
            catch (Exception ex)
            {
                EndReceive(ex);
                return;
            }
            if (!willRaiseEvent)
            {
                ProcessReceive(_readBuffer.SocketAsyncEventArgs);
            }
        }

        /// <summary>
        /// Processes receive events.
        /// </summary>
        /// <param name="e"></param>
        public void ProcessReceive(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                if (e.BytesTransferred > 0)
                {
                    _readBuffer.Position = e.BytesTransferred;
                    _readBuffer.Flip();

                    if (ReuseBuffer)
                    {
                        EndReceive(_readBuffer);
                    }
                    else
                    {
                        IoBuffer buf = IoBuffer.Allocate(_readBuffer.Remaining);
                        buf.Put(_readBuffer);
                        buf.Flip();
                        EndReceive(buf);
                    }

                    return;
                }
                else
                {
                    // closed
                    //Processor.Remove(this);
                    this.FilterChain.FireInputClosed();
                }
            }
            else if (e.SocketError != SocketError.OperationAborted
                && e.SocketError != SocketError.Interrupted
                && e.SocketError != SocketError.ConnectionReset)
            {
                EndReceive(new SocketException((Int32)e.SocketError));
            }
            else
            {
                // closed
                Processor.Remove(this);
            }
        }
    }
}
