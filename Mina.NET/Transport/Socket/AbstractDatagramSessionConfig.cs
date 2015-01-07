using System;
using Mina.Core.Session;

namespace Mina.Transport.Socket
{
    abstract class AbstractDatagramSessionConfig : AbstractIoSessionConfig, IDatagramSessionConfig
    {
        protected override void DoSetAll(IoSessionConfig config)
        {
            IDatagramSessionConfig cfg = config as IDatagramSessionConfig;
            if (cfg == null)
                return;

            if (cfg.EnableBroadcast.HasValue)
                EnableBroadcast = cfg.EnableBroadcast;
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
            MulticastOption = cfg.MulticastOption;
        }

        public abstract Boolean? EnableBroadcast { get; set; }

        public abstract Int32? ReceiveBufferSize { get; set; }

        public abstract Int32? SendBufferSize { get; set; }

        public abstract Boolean? ReuseAddress { get; set; }

        public abstract Int32? TrafficClass { get; set; }

        public abstract Boolean? ExclusiveAddressUse { get; set; }

        public abstract System.Net.Sockets.MulticastOption MulticastOption { get; set; }
    }
}
