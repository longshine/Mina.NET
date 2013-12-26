using System;
using System.Net.Sockets;
using System.Threading;
using Mina.Core.Buffer;
using Mina.Core.Service;
using Mina.Core.Write;
using Mina.Util;

namespace Mina.Transport.Socket
{
    public class AsyncSocketSession : SocketSession
    {
        private readonly SocketAsyncEventArgsBuffer _readBuffer;
        private Int32 _writing;

        public AsyncSocketSession(IoService service, IoProcessor<SocketSession> processor, System.Net.Sockets.Socket socket, SocketAsyncEventArgsBuffer readBuffer)
            : base(service, processor, socket)
        {
            _readBuffer = readBuffer;
            _readBuffer.SocketAsyncEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(SocketAsyncEventArgs_Completed);
        }

        void SocketAsyncEventArgs_Completed(Object sender, SocketAsyncEventArgs e)
        {
            ProcessReceive(e);
        }

        protected override void BeginSend(IoBuffer buf)
        {
            SocketAsyncEventArgs saea;
            SocketAsyncEventArgsBuffer saeaBuf = buf as SocketAsyncEventArgsBuffer;
            if (saeaBuf == null)
            {
                saea = new SocketAsyncEventArgs();
                ArraySegment<Byte> array = buf.GetRemaining();
                saea.SetBuffer(array.Array, array.Offset, array.Count);
                saea.Completed += new EventHandler<SocketAsyncEventArgs>(saea_Completed);
            }
            else
            {
                saea = saeaBuf.SocketAsyncEventArgs;
                saea.Completed += new EventHandler<SocketAsyncEventArgs>(saea_Completed);
            }

            try
            {
                Boolean willRaiseEvent = Socket.SendAsync(saea);
                if (!willRaiseEvent)
                {
                    ProcessSend(saea);
                }
            }
            catch (Exception ex)
            {
                ExceptionMonitor.Instance.ExceptionCaught(ex);
            }
        }

        void saea_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessSend(e);
        }

        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                EndSend(e.BytesTransferred);
                // TODO e.BytesTransferred == 0
            }
            else
            {
                ExceptionMonitor.Instance.ExceptionCaught(new SocketException((Int32)e.SocketError));

                // closed
                Processor.Remove(this);
            }
        }

        protected override void BeginReceive()
        {
            _readBuffer.Clear();
            try
            {
                Boolean willRaiseEvent = Socket.ReceiveAsync(_readBuffer.SocketAsyncEventArgs);
                if (!willRaiseEvent)
                {
                    ProcessReceive(_readBuffer.SocketAsyncEventArgs);
                }
            }
            catch (Exception ex)
            {
                ExceptionMonitor.Instance.ExceptionCaught(ex);
            }
        }

        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                if (e.BytesTransferred > 0)
                {
                    _readBuffer.Position = e.BytesTransferred;
                    _readBuffer.Flip();

                    EndReceive(_readBuffer);
                    return;
                }
            }
            else
            {
                ExceptionMonitor.Instance.ExceptionCaught(new SocketException((Int32)e.SocketError));
            }

            // closed
            Processor.Remove(this);
        }
    }
}
