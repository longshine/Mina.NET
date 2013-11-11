using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Mina.Core.Buffer;
using Mina.Core.Filterchain;
using Mina.Core.Service;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Transport.Socket
{
    public class SocketSession : AbstractIoSession
    {
        private readonly System.Net.Sockets.Socket _socket;
        private readonly SocketAsyncEventArgsBuffer _readBuffer;
        private readonly IoProcessor<SocketSession> _processor;
        private readonly IoFilterChain _filterChain;
        private Int32 _writing;

        public SocketSession(IoService service, IoProcessor<SocketSession> processor, System.Net.Sockets.Socket socket, SocketAsyncEventArgsBuffer readBuffer)
            : base(service)
        {
            _socket = socket;
            _readBuffer = readBuffer;
            _config = new SessionConfigImpl(socket);
            if (service.SessionConfig != null)
                _config.SetAll(service.SessionConfig);
            _processor = processor;
            _filterChain = new DefaultIoFilterChain(this);

            _readBuffer.SocketAsyncEventArgs.Completed += new EventHandler<SocketAsyncEventArgs>(SocketAsyncEventArgs_Completed);
        }

        void SocketAsyncEventArgs_Completed(Object sender, SocketAsyncEventArgs e)
        {
            ProcessReceive(e);
        }

        public override IoProcessor Processor
        {
            get { return _processor; }
        }

        public override IoFilterChain FilterChain
        {
            get { return _filterChain; }
        }

        public override EndPoint LocalEndPoint
        {
            get { return _socket.LocalEndPoint; }
        }

        public override EndPoint RemoteEndPoint
        {
            get { return _socket.RemoteEndPoint; }
        }

        public System.Net.Sockets.Socket Socket
        {
            get { return _socket; }
        }

        public void Start()
        {
            BeginReceive();
        }

        public void Flush()
        { 
            if (Interlocked.CompareExchange(ref _writing, 1, 0) > 0)
                return;
            BeginSend();
        }

        private void BeginSend()
        {
            IWriteRequestQueue writeRequestQueue = WriteRequestQueue;
            IWriteRequest req = writeRequestQueue.Poll(this);

            if (req == null)
            {
                Interlocked.Exchange(ref _writing, 0);
                return;
            }

            IoBuffer buf = req.Message as IoBuffer;

            if (buf == null)
            {
                throw new InvalidOperationException("Don't know how to handle message of type '"
                            + req.Message.GetType().Name + "'.  Are you missing a protocol encoder?");
            }
            else
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

                Boolean willRaiseEvent = _socket.SendAsync(saea);
                if (!willRaiseEvent)
                {
                    ProcessSend(saea);
                }
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
                this.IncreaseWrittenBytes(e.BytesTransferred, DateTime.Now);
                // TODO e.BytesTransferred == 0
                BeginSend();
            }
        }

        private void BeginReceive()
        {
            _readBuffer.Clear();
            Boolean willRaiseEvent = _socket.ReceiveAsync(_readBuffer.SocketAsyncEventArgs);
            if (!willRaiseEvent)
            {
                ProcessReceive(_readBuffer.SocketAsyncEventArgs);
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
                    FilterChain.FireMessageReceived(_readBuffer);

                    BeginReceive();
                }
                else
                {
                    // closed
                    Processor.Remove(this);
                }
            }
        }

        class SessionConfigImpl : AbstractSocketSessionConfig
        {
            private System.Net.Sockets.Socket _socket;

            public SessionConfigImpl(System.Net.Sockets.Socket socket)
            {
                _socket = socket;
            }

            public override Int32? ReceiveBufferSize
            {
                get { return _socket.ReceiveBufferSize; }
                set { if (value.HasValue) _socket.ReceiveBufferSize = value.Value; }
            }

            public override Int32? SendBufferSize
            {
                get { return _socket.SendBufferSize; }
                set { if (value.HasValue) _socket.SendBufferSize = value.Value; }
            }

            public override Boolean? NoDelay
            {
                get { return _socket.NoDelay; }
                set { if (value.HasValue) _socket.NoDelay = value.Value; }
            }

            public override Int32? SoLinger
            {
                get { return _socket.LingerState.LingerTime; }
                set
                {
                    if (value.HasValue)
                    {
                        if (value < 0)
                        {
                            _socket.LingerState.Enabled = false;
                            _socket.LingerState.LingerTime = 0;
                        }
                        else
                        {
                            _socket.LingerState.Enabled = true;
                            _socket.LingerState.LingerTime = value.Value;
                        }
                    }
                }
            }
        }
    }
}
