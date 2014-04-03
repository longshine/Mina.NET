using System;
using System.Net;
using System.Net.Sockets;
using Mina.Core.Buffer;
using Mina.Core.Service;

namespace Mina.Transport.Socket
{
    partial class AsyncDatagramSession : SocketSession
    {
        private readonly SocketAsyncEventArgsBuffer _readBuffer;
        private readonly SocketAsyncEventArgsBuffer _writeBuffer;
        private readonly EventHandler<SocketAsyncEventArgs> _completeHandler;

        /// <summary>
        /// Creates a new connector-side session instance.
        /// </summary>
        public AsyncDatagramSession(IoService service, IoProcessor<SocketSession> processor,
            System.Net.Sockets.Socket socket, EndPoint remoteEP,
            SocketAsyncEventArgsBuffer readBuffer, SocketAsyncEventArgsBuffer writeBuffer, Boolean reuseBuffer)
            : base(service, processor, new DatagramSessionConfigImpl(socket), socket, socket.LocalEndPoint, socket.RemoteEndPoint, reuseBuffer)
        {
            _readBuffer = readBuffer;
            _readBuffer.SocketAsyncEventArgs.UserToken = this;
            _writeBuffer = writeBuffer;
            _writeBuffer.SocketAsyncEventArgs.UserToken = this;
            _completeHandler = saea_Completed;
        }

        private void BeginSend(IoBuffer buf, EndPoint destination)
        {
            _writeBuffer.Clear();

            SocketAsyncEventArgs saea;
            SocketAsyncEventArgsBuffer saeaBuf = buf as SocketAsyncEventArgsBuffer;
            if (saeaBuf == null)
            {
                if (_writeBuffer.Remaining < buf.Remaining)
                {
                    // TODO allocate a temp buffer
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

            saea.RemoteEndPoint = destination;

            Boolean willRaiseEvent;
            try
            {
                willRaiseEvent = Socket.SendToAsync(saea);
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
            if (e != _writeBuffer.SocketAsyncEventArgs)
            {
                e.Completed -= _completeHandler;
            }
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
                if (_readBuffer.SocketAsyncEventArgs.RemoteEndPoint == null)
                    _readBuffer.SocketAsyncEventArgs.RemoteEndPoint = Socket.RemoteEndPoint;

                willRaiseEvent = Socket.ReceiveFromAsync(_readBuffer.SocketAsyncEventArgs);
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
            }
            else if (e.SocketError != SocketError.OperationAborted
                && e.SocketError != SocketError.Interrupted)
            {
                EndReceive(new SocketException((Int32)e.SocketError));
            }
        }
    }
}
