#if !UNITY
using System;
using Mina.Core.Session;

namespace Mina.Transport.Serial
{
    /// <summary>
    /// The default configuration for a serial session 
    /// </summary>
    class DefaultSerialSessionConfig : AbstractIoSessionConfig, ISerialSessionConfig
    {
        private Int32 _readTimeout;
        private Int32 _writeBufferSize;
        private Int32 _receivedBytesThreshold;

        public DefaultSerialSessionConfig()
        {
            // reset configs
            ReadBufferSize = 0;
            WriteTimeout = 0;
        }

        protected override void DoSetAll(IoSessionConfig config)
        {
            ISerialSessionConfig cfg = config as ISerialSessionConfig;
            if (cfg != null)
            {
                ReadTimeout = cfg.ReadTimeout;
                WriteBufferSize = cfg.WriteBufferSize;
                ReceivedBytesThreshold = cfg.ReceivedBytesThreshold;
            }
        }

        public Int32 ReadTimeout
        {
            get { return _readTimeout; }
            set { _readTimeout = value; }
        }

        public Int32 WriteBufferSize
        {
            get { return _writeBufferSize; }
            set { _writeBufferSize = value; }
        }

        public Int32 ReceivedBytesThreshold
        {
            get { return _receivedBytesThreshold; }
            set { _receivedBytesThreshold = value; }
        }
    }
}
#endif