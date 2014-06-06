using System;
using System.Net;
using System.Net.Sockets;
using Mina.Core.Buffer;
using Mina.Core.Service;
using SendingContext = Mina.Transport.Socket.AsyncSocketSession.SendingContext;

namespace Mina.Transport.Socket
{
    partial class AsyncDatagramSession : SocketSession
    {
        private readonly Byte[] _readBuffer;

        /// <summary>
        /// Creates a new connector-side session instance.
        /// </summary>
        public AsyncDatagramSession(IoService service, IoProcessor<SocketSession> processor,
            System.Net.Sockets.Socket socket, EndPoint remoteEP, Boolean reuseBuffer)
            : base(service, processor, new DatagramSessionConfigImpl(socket), socket, socket.LocalEndPoint, socket.RemoteEndPoint, reuseBuffer)
        {
            _readBuffer = new Byte[service.SessionConfig.ReadBufferSize];
        }

        /// <inheritdoc/>
        protected override void BeginReceive()
        {
            try
            {
                EndPoint remoteEP = Socket.RemoteEndPoint;
                Socket.BeginReceiveFrom(_readBuffer, 0, _readBuffer.Length, SocketFlags.None, ref remoteEP, ReceiveCallback, Socket);
            }
            catch (ObjectDisposedException)
            {
                // do nothing
            }
        }

        private void BeginSend(IoBuffer buf, EndPoint destination)
        {
            ArraySegment<Byte> array = buf.GetRemaining();
            try
            {
                Socket.BeginSendTo(array.Array, array.Offset, array.Count, SocketFlags.None, destination, SendCallback, new SendingContext(Socket, buf));
            }
            catch (ObjectDisposedException)
            {
                // ignore
            }
            catch (Exception ex)
            {
                EndSend(ex);
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            System.Net.Sockets.Socket socket = (System.Net.Sockets.Socket)ar.AsyncState;
            Int32 read = 0;
            try
            {
                EndPoint remoteEP = Socket.RemoteEndPoint; 
                read = socket.EndReceiveFrom(ar, ref remoteEP);
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

            if (read > 0)
            {
                if (ReuseBuffer)
                {
                    EndReceive(IoBuffer.Wrap(_readBuffer, 0, read));
                }
                else
                {
                    IoBuffer buf = IoBuffer.Allocate(read);
                    buf.Put(_readBuffer, 0, read);
                    buf.Flip();
                    EndReceive(buf);
                }
                return;
            }

            // closed
            Processor.Remove(this);
        }

        private void SendCallback(IAsyncResult ar)
        {
            SendingContext sc = (SendingContext)ar.AsyncState;
            Int32 written;
            try
            {
                written = sc.socket.EndSendTo(ar);
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

            sc.buffer.Position += written;
            EndSend(written);
        }
    }
}
