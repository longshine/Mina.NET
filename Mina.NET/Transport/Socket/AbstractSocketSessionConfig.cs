using System;
using Mina.Core.Session;

namespace Mina.Transport.Socket
{
    abstract class AbstractSocketSessionConfig : AbstractIoSessionConfig, ISocketSessionConfig
    {
        protected override void DoSetAll(IoSessionConfig config)
        {
            ISocketSessionConfig cfg = config as ISocketSessionConfig;
            if (cfg == null)
                return;

            if (cfg.ReceiveBufferSize.HasValue)
                ReceiveBufferSize = cfg.ReceiveBufferSize;
            if (cfg.SendBufferSize.HasValue)
                SendBufferSize = cfg.SendBufferSize;
            if (cfg.NoDelay.HasValue)
                NoDelay = cfg.NoDelay;
            if (cfg.SoLinger.HasValue)
                SoLinger = cfg.SoLinger;
        }

        public abstract Int32? ReceiveBufferSize { get; set; }

        public abstract Int32? SendBufferSize { get; set; }

        public abstract Boolean? NoDelay { get; set; }

        public abstract Int32? SoLinger { get; set; }
    }
}
