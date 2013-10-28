using System;

namespace Mina.Core.Session
{
    /// <summary>
    /// A base implementation of <see cref="IoSessionConfig"/>.
    /// </summary>
    public abstract class AbstractIoSessionConfig : IoSessionConfig
    {
        private Int32 _readBufferSize = 2048;
        private Int32 _idleTimeForRead;
        private Int32 _idleTimeForWrite;
        private Int32 _idleTimeForBoth;
        private Int64 _throughputCalculationInterval = 3000L;

        public Int32 ReadBufferSize
        {
            get { return _readBufferSize; }
            set { _readBufferSize = value; }
        }

        public Int64 ThroughputCalculationIntervalInMillis
        {
            get { return _throughputCalculationInterval; }
            set { _throughputCalculationInterval = value; }
        }

        public Int32 GetIdleTime(IdleStatus status)
        {
            switch (status)
            {
                case IdleStatus.ReaderIdle:
                    return _idleTimeForRead;
                case IdleStatus.WriterIdle:
                    return _idleTimeForWrite;
                case IdleStatus.BothIdle:
                    return _idleTimeForBoth;
                default:
                    throw new ArgumentException("status");
            }
        }

        public void SetIdleTime(IdleStatus status, Int32 idleTime)
        {
            switch (status)
            {
                case IdleStatus.ReaderIdle:
                    _idleTimeForRead = idleTime;
                    break;
                case IdleStatus.WriterIdle:
                    _idleTimeForWrite = idleTime;
                    break;
                case IdleStatus.BothIdle:
                    _idleTimeForBoth = idleTime;
                    break;
                default:
                    throw new ArgumentException("status");
            }
        }

        public void SetAll(IoSessionConfig config)
        {
            if (config == null)
                throw new ArgumentNullException("config");
            ReadBufferSize = config.ReadBufferSize;
            SetIdleTime(IdleStatus.BothIdle, config.GetIdleTime(IdleStatus.BothIdle));
            SetIdleTime(IdleStatus.ReaderIdle, config.GetIdleTime(IdleStatus.ReaderIdle));
            SetIdleTime(IdleStatus.WriterIdle, config.GetIdleTime(IdleStatus.WriterIdle));
            DoSetAll(config);
        }

        protected abstract void DoSetAll(IoSessionConfig config);
    }
}
