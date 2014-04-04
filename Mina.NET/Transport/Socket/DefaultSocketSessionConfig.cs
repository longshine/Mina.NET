using System;

namespace Mina.Transport.Socket
{
    class DefaultSocketSessionConfig : AbstractSocketSessionConfig
    {
        public override Int32? ReceiveBufferSize { get; set; }

        public override Int32? SendBufferSize { get; set; }

        public override Boolean? NoDelay { get; set; }

        public override Int32? SoLinger { get; set; }

        public override Boolean? ExclusiveAddressUse { get; set; }

        public override Boolean? ReuseAddress { get; set; }

        public override Int32? TrafficClass { get; set; }

        public override Boolean? KeepAlive { get; set; }

        public override Boolean? OobInline { get; set; }
    }
}
