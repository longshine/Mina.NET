using System;
using System.Net;
using System.Net.Sockets;
using Mina.Core.Buffer;
using Mina.Core.File;
using Mina.Core.Service;
using Mina.Core.Write;
using Mina.Util;

namespace Mina.Transport.Socket
{
    public class AsyncSocketSession : SocketSession
    {
        public static readonly ITransportMetadata Metadata
            = new DefaultTransportMetadata("async", "socket", false, true, typeof(IPEndPoint));

        private readonly SocketAsyncEventArgsBuffer _readBuffer;
        private readonly SocketAsyncEventArgsBuffer _writeBuffer;
        private readonly EventHandler<SocketAsyncEventArgs> _completeHandler;

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
        protected override void BeginSend(IWriteRequest request, IoBuffer buf)
        {
            _writeBuffer.Clear();

            SocketAsyncEventArgs saea;
            SocketAsyncEventArgsBuffer saeaBuf = buf as SocketAsyncEventArgsBuffer;
            if (saeaBuf == null)
            {
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
                saea = _writeBuffer.SocketAsyncEventArgs;
                saea.SetBuffer(saea.Offset + _writeBuffer.Position, _writeBuffer.Limit);
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
            else
            {
                EndSend(new SocketException((Int32)e.SocketError));
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
                    Processor.Remove(this);
                }
            }
            else
            {
                EndReceive(new SocketException((Int32)e.SocketError));
            }
        }
    }
}
