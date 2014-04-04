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
            if (cfg.ReuseAddress.HasValue)
                ReuseAddress = cfg.ReuseAddress;
            if (cfg.TrafficClass.HasValue)
                TrafficClass = cfg.TrafficClass;
            if (cfg.ExclusiveAddressUse.HasValue)
                ExclusiveAddressUse = cfg.ExclusiveAddressUse;
            if (cfg.KeepAlive.HasValue)
                KeepAlive = cfg.KeepAlive;
            if (cfg.OobInline.HasValue)
                OobInline = cfg.OobInline;
            if (cfg.NoDelay.HasValue)
                NoDelay = cfg.NoDelay;
            if (cfg.SoLinger.HasValue)
                SoLinger = cfg.SoLinger;
        }

        public abstract Int32? ReceiveBufferSize { get; set; }

        public abstract Int32? SendBufferSize { get; set; }

        public abstract Boolean? NoDelay { get; set; }

        public abstract Int32? SoLinger { get; set; }

        public abstract Boolean? ExclusiveAddressUse { get; set; }

        public abstract Boolean? ReuseAddress { get; set; }

        public abstract Int32? TrafficClass { get; set; }

        public abstract Boolean? KeepAlive { get; set; }

        public abstract Boolean? OobInline { get; set; }
    }
}
