using System;
using System.Net.Sockets;

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

        public override Boolean? ExclusiveAddressUse
        {
            get { return _socket.ExclusiveAddressUse; }
            set { if (value.HasValue) _socket.ExclusiveAddressUse = value.Value; }
        }

        public override Boolean? ReuseAddress
        {
            get
            {
                return Convert.ToBoolean(_socket.GetSocketOption(
                    SocketOptionLevel.Socket, SocketOptionName.ReuseAddress));
            }
            set
            {
                if (value.HasValue)
                    _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, value.Value);
            }
        }

        public override Int32? TrafficClass
        {
            get
            {
                return Convert.ToInt32(_socket.GetSocketOption(
                    SocketOptionLevel.Socket, SocketOptionName.TypeOfService));
            }
            set
            {
                if (value.HasValue)
                    _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.TypeOfService, value.Value);
            }
        }

        public override MulticastOption MulticastOption
        {
            get
            {
                return (MulticastOption)_socket.GetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership);
            }
            set
            {
                if (value != null)
                    _socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, value);
            }
        }
    }
}
