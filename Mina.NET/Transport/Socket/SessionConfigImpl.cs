using System;

namespace Mina.Transport.Socket
{
    class SessionConfigImpl : AbstractSocketSessionConfig
    {
        private readonly System.Net.Sockets.Socket _socket;

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
