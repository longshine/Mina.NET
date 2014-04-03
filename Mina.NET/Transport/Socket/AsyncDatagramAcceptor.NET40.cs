using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Mina.Core.Buffer;
using Mina.Core.Service;
using Mina.Core.Session;
using Mina.Util;

namespace Mina.Transport.Socket
{
    partial class AsyncDatagramAcceptor : AbstractIoAcceptor, IDatagramAcceptor
    {
        private void BeginReceive(SocketContext ctx)
        {
            if (ctx.receiveBuffer == null)
            {
                Byte[] buffer = new Byte[SessionConfig.ReadBufferSize];
                ctx.receiveBuffer = new SocketAsyncEventArgs();
                ctx.receiveBuffer.SetBuffer(buffer, 0, buffer.Length);
                ctx.receiveBuffer.Completed += new EventHandler<SocketAsyncEventArgs>(OnCompleted);
                ctx.receiveBuffer.UserToken = ctx;
            }

            ctx.receiveBuffer.RemoteEndPoint = new IPEndPoint(ctx.Socket.AddressFamily == AddressFamily.InterNetwork ?
                IPAddress.Any : IPAddress.IPv6Any, 0);

            Boolean willRaiseEvent;
            try
            {
                willRaiseEvent = ctx.Socket.ReceiveFromAsync(ctx.receiveBuffer);
            }
            catch (ObjectDisposedException)
            {
                // do nothing
                return;
            }
            if (!willRaiseEvent)
            {
                ProcessReceive(ctx.receiveBuffer);
            }
        }

        void OnCompleted(Object sender, SocketAsyncEventArgs e)
        {
            ProcessReceive(e);
        }

        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                IoBuffer buf = IoBuffer.Allocate(e.BytesTransferred);
                buf.Put(e.Buffer, e.Offset, e.BytesTransferred);
                buf.Flip();
                EndReceive((SocketContext)e.UserToken, buf, e.RemoteEndPoint);
            }
            else if (e.SocketError != SocketError.OperationAborted
                && e.SocketError != SocketError.Interrupted)
            {
                ExceptionMonitor.Instance.ExceptionCaught(new SocketException((Int32)e.SocketError));
            }
        }

        partial class SocketContext
        {
            public SocketAsyncEventArgs receiveBuffer;
            private SocketAsyncEventArgsBuffer _writeBuffer;
            private readonly EventHandler<SocketAsyncEventArgs> _completeHandler;

            public SocketContext(System.Net.Sockets.Socket socket, IoSessionConfig config)
            {
                _socket = socket;

                _completeHandler = OnCompleted;

                Byte[] writeBuffer = new Byte[config.ReadBufferSize];
                _writeBuffer = SocketAsyncEventArgsBufferAllocator.Instance.Wrap(writeBuffer);
                _writeBuffer.SocketAsyncEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnCompleted);
                _writeBuffer.SocketAsyncEventArgs.UserToken = this;
            }

            public void Close()
            {
                _socket.Close();
                receiveBuffer.Dispose();
                _writeBuffer.Dispose();
            }

            private void BeginSend(AsyncDatagramSession session, IoBuffer buf, EndPoint remoteEP)
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

                saea.UserToken = session;
                saea.RemoteEndPoint = remoteEP;

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
                    EndSend(session, ex);
                    return;
                }
                if (!willRaiseEvent)
                {
                    ProcessSend(saea);
                }
            }

            void OnCompleted(Object sender, SocketAsyncEventArgs e)
            {
                if (e != _writeBuffer.SocketAsyncEventArgs)
                {
                    e.Completed -= _completeHandler;
                }
                ProcessSend(e);
            }

            private void ProcessSend(SocketAsyncEventArgs e)
            {
                if (e.SocketError == SocketError.Success)
                {
                    EndSend((AsyncDatagramSession)e.UserToken, e.BytesTransferred);
                }
                else
                {
                    EndSend((AsyncDatagramSession)e.UserToken, new SocketException((Int32)e.SocketError));
                }
            }
        }
    }
}
