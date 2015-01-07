using System;

namespace Mina.Transport.Socket
{
    class DefaultDatagramSessionConfig : AbstractDatagramSessionConfig
    {
        public override Boolean? EnableBroadcast { get; set; }

        public override Int32? ReceiveBufferSize { get; set; }

        public override Int32? SendBufferSize { get; set; }

        public override Boolean? ExclusiveAddressUse { get; set; }

        public override Boolean? ReuseAddress { get; set; }

        public override Int32? TrafficClass { get; set; }

        public override System.Net.Sockets.MulticastOption MulticastOption { get; set; }
    }
}
