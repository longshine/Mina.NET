using System;

namespace Mina.Transport.Socket
{
    class DefaultDatagramSessionConfig : AbstractDatagramSessionConfig
    {
        public override Boolean? EnableBroadcast { get; set; }

        public override Int32? ReceiveBufferSize { get; set; }

        public override Int32? SendBufferSize { get; set; }
    }
}
