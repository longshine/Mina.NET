using System;
using System.Net.Sockets;
using Mina.Core.Buffer;
using Mina.Core.Service;
using Mina.Util;

namespace Mina.Transport.Socket
{
    public class AsyncSocketSession : SocketSession
    {
        private readonly Byte[] _readBuffer;

        public AsyncSocketSession(IoService service, IoProcessor<SocketSession> processor,
            System.Net.Sockets.Socket socket, Boolean reuseBuffer)
            : base(service, processor, socket)
        {
            _readBuffer = new Byte[service.SessionConfig.ReadBufferSize];
            ReuseBuffer = reuseBuffer;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to reuse the internal
        /// <see cref="ReadBuffer"/> as the buffer sent to <see cref="SocketSession.FilterChain"/>
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

        /// <inheritdoc/>
        protected override void BeginReceive()
        {
            Socket.BeginReceive(_readBuffer, 0, _readBuffer.Length, SocketFlags.None, ReceiveCallback, Socket);
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            System.Net.Sockets.Socket socket = (System.Net.Sockets.Socket)ar.AsyncState;
            Int32 read = 0;
            try
            {
                read = socket.EndReceive(ar);
            }
            catch (Exception ex)
            {
                ExceptionMonitor.Instance.ExceptionCaught(ex);
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

        /// <inheritdoc/>
        protected override void BeginSend(IoBuffer buf)
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
                ExceptionMonitor.Instance.ExceptionCaught(ex);
            }
        }

        /// <inheritdoc/>
        protected override void BeginSendFile(System.IO.FileInfo file)
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
                ExceptionMonitor.Instance.ExceptionCaught(ex);
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
            catch (Exception ex)
            {
                ExceptionMonitor.Instance.ExceptionCaught(ex);

                // closed
                Processor.Remove(this);

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
            catch (Exception ex)
            {
                ExceptionMonitor.Instance.ExceptionCaught(ex);

                // closed
                Processor.Remove(this);

                return;
            }

            // TODO change written bytes to long?
            EndSend((Int32)sc.file.Length);
        }

        class SendingContext
        {
            public readonly System.Net.Sockets.Socket socket;
            public readonly IoBuffer buffer;
            public readonly System.IO.FileInfo file;

            public SendingContext(System.Net.Sockets.Socket s, IoBuffer b)
            {
                socket = s;
                buffer = b;
                file = null;
            }

            public SendingContext(System.Net.Sockets.Socket s, System.IO.FileInfo f)
            {
                socket = s;
                buffer = null;
                file = f;
            }
        }
    }
}
