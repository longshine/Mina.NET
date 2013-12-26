using System;
using System.Net.Sockets;
using Mina.Core.Buffer;
using Mina.Core.Service;
using Mina.Util;

namespace Mina.Transport.Socket
{
    class AsyncSocketSession : SocketSession
    {
        private readonly Byte[] _readBuffer;

        public AsyncSocketSession(IoService service, IoProcessor<SocketSession> processor, System.Net.Sockets.Socket socket)
            : base(service, processor, socket)
        {
            _readBuffer = new Byte[service.SessionConfig.ReadBufferSize];
        }

        protected override void BeginReceive()
        {
            Socket.BeginReceive(_readBuffer, 0, _readBuffer.Length, SocketFlags.None, ReceiveCallback, null);
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            Int32 read = 0;
            try
            {
                read = Socket.EndReceive(ar);
            }
            catch (Exception ex)
            {
                ExceptionMonitor.Instance.ExceptionCaught(ex);
            }

            if (read > 0)
            {
                EndReceive(ByteBufferAllocator.Instance.Wrap(_readBuffer, 0, read));
                return;
            }

            // closed
            Processor.Remove(this);
        }

        protected override void BeginSend(IoBuffer buf)
        {
            ArraySegment<Byte> array = buf.GetRemaining();
            Socket.BeginSend(array.Array, array.Offset, array.Count, SocketFlags.None, SendCallback, null);
        }

        private void SendCallback(IAsyncResult ar)
        {
            Int32 written;
            try
            {
                written = Socket.EndSend(ar);
            }
            catch (Exception ex)
            {
                ExceptionMonitor.Instance.ExceptionCaught(ex);

                // closed
                Processor.Remove(this);

                return;
            }

            EndSend(written);
        }
    }
}
