using System;

namespace Mina.Transport.Socket
{
    class DatagramSessionConfigImpl : AbstractDatagramSessionConfig
    {
        private readonly System.Net.Sockets.Socket _socket;

        public DatagramSessionConfigImpl(System.Net.Sockets.Socket socket)
        {
            _socket = socket;
        }

        public override Boolean? EnableBroadcast
        {
            get { return _socket.EnableBroadcast; }
            set { if (value.HasValue) _socket.EnableBroadcast = value.Value; }
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
    }
}
