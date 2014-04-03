using System;
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
            EndPoint remoteEP = new IPEndPoint(ctx.Socket.AddressFamily == AddressFamily.InterNetwork ?
                IPAddress.Any : IPAddress.IPv6Any, 0);
            try
            {
                ctx.Socket.BeginReceiveFrom(ctx.receiveBuffer, 0, ctx.receiveBuffer.Length, SocketFlags.None, ref remoteEP, ReceiveCallback, ctx);
            }
            catch (ObjectDisposedException)
            {
                // do nothing
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            SocketContext ctx = (SocketContext)ar.AsyncState;
            EndPoint remoteEP = new IPEndPoint(ctx.Socket.AddressFamily == AddressFamily.InterNetwork ?
                IPAddress.Any : IPAddress.IPv6Any, 0);
            Int32 read = 0;
            try
            {
                read = ctx.Socket.EndReceiveFrom(ar, ref remoteEP);
            }
            catch (ObjectDisposedException)
            {
                // do nothing
                return;
            }
            catch (Exception ex)
            {
                ExceptionMonitor.Instance.ExceptionCaught(ex);
                return;
            }

            IoBuffer buf;
            if (ReuseBuffer)
            {
                buf = IoBuffer.Wrap(ctx.receiveBuffer, 0, read);
            }
            else
            {
                buf = IoBuffer.Allocate(read);
                buf.Put(ctx.receiveBuffer, 0, read);
                buf.Flip();
            }
            EndReceive(ctx, buf, remoteEP);
        }

        partial class SocketContext
        {
            public readonly Byte[] receiveBuffer;

            public SocketContext(System.Net.Sockets.Socket socket, IoSessionConfig config)
            {
                _socket = socket;
                receiveBuffer = new Byte[config.ReadBufferSize];
            }

            public void Close()
            {
                _socket.Close();
            }

            private void BeginSend(AsyncDatagramSession session, IoBuffer buf, EndPoint remoteEP)
            {
                ArraySegment<Byte> array = buf.GetRemaining();
                try
                {
                    Socket.BeginSendTo(array.Array, array.Offset, array.Count, SocketFlags.None, remoteEP, SendCallback, session);
                }
                catch (ObjectDisposedException)
                {
                    // ignore
                }
                catch (Exception ex)
                {
                    EndSend(session, ex);
                }
            }

            private void SendCallback(IAsyncResult ar)
            {
                AsyncDatagramSession session = (AsyncDatagramSession)ar.AsyncState;
                Int32 written;
                try
                {
                    written = Socket.EndSend(ar);
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

                EndSend(session, written);
            }
        }
    }
}
