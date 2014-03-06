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
        private Int32 _writeTimeout = 60;
        private Int32 _throughputCalculationInterval = 3;

        /// <inheritdoc/>
        public Int32 ReadBufferSize
        {
            get { return _readBufferSize; }
            set { _readBufferSize = value; }
        }

        /// <inheritdoc/>
        public Int32 ThroughputCalculationInterval
        {
            get { return _throughputCalculationInterval; }
            set { _throughputCalculationInterval = value; }
        }

        /// <inheritdoc/>
        public Int64 ThroughputCalculationIntervalInMillis
        {
            get { return _throughputCalculationInterval * 1000L; }
        }

        /// <inheritdoc/>
        public Int32 WriteTimeout
        {
            get { return _writeTimeout; }
            set { _writeTimeout = value; }
        }

        /// <inheritdoc/>
        public Int64 WriteTimeoutInMillis
        {
            get { return _writeTimeout * 1000L; }
        }

        /// <inheritdoc/>
        public Int32 ReaderIdleTime
        {
            get { return GetIdleTime(IdleStatus.ReaderIdle); }
            set { SetIdleTime(IdleStatus.ReaderIdle, value); }
        }

        /// <inheritdoc/>
        public Int32 WriterIdleTime
        {
            get { return GetIdleTime(IdleStatus.WriterIdle); }
            set { SetIdleTime(IdleStatus.WriterIdle, value); }
        }

        /// <inheritdoc/>
        public Int32 BothIdleTime
        {
            get { return GetIdleTime(IdleStatus.BothIdle); }
            set { SetIdleTime(IdleStatus.BothIdle, value); }
        }

        /// <inheritdoc/>
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
                    throw new ArgumentException("Unknown status", "status");
            }
        }

        /// <inheritdoc/>
        public Int64 GetIdleTimeInMillis(IdleStatus status)
        {
            return GetIdleTime(status) * 1000L;
        }

        /// <inheritdoc/>
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
                    throw new ArgumentException("Unknown status", "status");
            }
        }

        /// <inheritdoc/>
        public void SetAll(IoSessionConfig config)
        {
            if (config == null)
                throw new ArgumentNullException("config");
            ReadBufferSize = config.ReadBufferSize;
            SetIdleTime(IdleStatus.BothIdle, config.GetIdleTime(IdleStatus.BothIdle));
            SetIdleTime(IdleStatus.ReaderIdle, config.GetIdleTime(IdleStatus.ReaderIdle));
            SetIdleTime(IdleStatus.WriterIdle, config.GetIdleTime(IdleStatus.WriterIdle));
            ThroughputCalculationInterval = config.ThroughputCalculationInterval;
            DoSetAll(config);
        }

        protected abstract void DoSetAll(IoSessionConfig config);
    }
}
