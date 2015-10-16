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

        private readonly Byte[] _readBuffer;

        public AsyncSocketSession(IoService service, IoProcessor<SocketSession> processor,
            System.Net.Sockets.Socket socket, Boolean reuseBuffer)
            : base(service, processor, new SessionConfigImpl(socket), socket, socket.LocalEndPoint, socket.RemoteEndPoint, reuseBuffer)
        {
            _readBuffer = new Byte[service.SessionConfig.ReadBufferSize];
        }

        /// <inheritdoc/>
        public override ITransportMetadata TransportMetadata
        {
            get { return Metadata; }
        }

        /// <inheritdoc/>
        protected override void BeginReceive()
        {
            try
            {
                Socket.BeginReceive(_readBuffer, 0, _readBuffer.Length, SocketFlags.None, ReceiveCallback, Socket);
            }
            catch (ObjectDisposedException)
            {
                // do nothing
            }
        }

        /// <inheritdoc/>
        protected override void BeginSend(IWriteRequest request, IoBuffer buf)
        {
            ArraySegment<Byte> array = buf.GetRemaining();
            try
            {
                Socket.BeginSend(array.Array, array.Offset, array.Count, SocketFlags.None, SendCallback, new SendingContext(Socket, buf));
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

        /// <inheritdoc/>
        protected override void BeginSendFile(IWriteRequest request, IFileRegion file)
        {
            try
            {
                Socket.BeginSendFile(file.FullName, SendFileCallback, new SendingContext(Socket, file));
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
                read = socket.EndReceive(ar);
            }
            catch (ObjectDisposedException)
            {
                // do nothing
                return;
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode != SocketError.OperationAborted
                    && ex.SocketErrorCode != SocketError.Interrupted
                    && ex.SocketErrorCode != SocketError.ConnectionReset)
                {
                    EndReceive(ex);
                }
                else
                {
                    // closed
                    Processor.Remove(this);
                }
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
            }
            else
            {
                // closed
                //Processor.Remove(this);
                this.FilterChain.FireInputClosed();
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            SendingContext sc = (SendingContext)ar.AsyncState;
            Int32 written;
            try
            {
                written = sc.socket.EndSend(ar);
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

        private void SendFileCallback(IAsyncResult ar)
        {
            SendingContext sc = (SendingContext)ar.AsyncState;
            
            try
            {
                sc.socket.EndSendFile(ar);
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

            // TODO change written bytes to long?
            EndSend((Int32)sc.file.Length);
        }

        internal class SendingContext
        {
            public readonly System.Net.Sockets.Socket socket;
            public readonly IoBuffer buffer;
            public readonly Core.File.IFileRegion file;

            public SendingContext(System.Net.Sockets.Socket s, IoBuffer b)
            {
                socket = s;
                buffer = b;
                file = null;
            }

            public SendingContext(System.Net.Sockets.Socket s, Core.File.IFileRegion f)
            {
                socket = s;
                buffer = null;
                file = f;
            }
        }
    }
}
