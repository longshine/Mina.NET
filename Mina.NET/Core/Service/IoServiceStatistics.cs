using System;
using System.Threading;

namespace Mina.Core.Service
{
    /// <summary>
    /// Provides usage statistics for an <see cref="IoService"/> instance.
    /// </summary>
    public class IoServiceStatistics
    {
        private readonly IoService _service;
        private readonly Object _throughputCalculationLock = new Byte[0];

        private Double _readBytesThroughput;
        private Double _writtenBytesThroughput;
        private Double _readMessagesThroughput;
        private Double _writtenMessagesThroughput;
        private Double _largestReadBytesThroughput;
        private Double _largestWrittenBytesThroughput;
        private Double _largestReadMessagesThroughput;
        private Double _largestWrittenMessagesThroughput;
        private Int64 _readBytes;
        private Int64 _writtenBytes;
        private Int64 _readMessages;
        private Int64 _writtenMessages;
        private DateTime _lastReadTime;
        private DateTime _lastWriteTime;
        private DateTime _lastThroughputCalculationTime;
        private Int64 _lastReadBytes;
        private Int64 _lastWrittenBytes;
        private Int64 _lastReadMessages;
        private Int64 _lastWrittenMessages;
        private Int32 _scheduledWriteBytes;
        private Int32 _scheduledWriteMessages;
        private Int32 _throughputCalculationInterval = 3;

        /// <summary>
        /// Initializes.
        /// </summary>
        public IoServiceStatistics(IoService service)
        {
            _service = service;
        }

        /// <summary>
        /// Gets the time when I/O occurred lastly.
        /// </summary>
        public DateTime LastIoTime
        {
            get { return _lastReadTime > _lastWriteTime ? _lastReadTime : _lastWriteTime; }
        }

        /// <summary>
        /// Gets or sets last time at which a read occurred on the service.
        /// </summary>
        public DateTime LastReadTime
        {
            get { return _lastReadTime; }
            set { _lastReadTime = value; }
        }

        /// <summary>
        /// Gets or sets last time at which a write occurred on the service.
        /// </summary>
        public DateTime LastWriteTime
        {
            get { return _lastWriteTime; }
            set { _lastWriteTime = value; }
        }

        /// <summary>
        /// Gets the number of bytes read by this service.
        /// </summary>
        public Int64 ReadBytes
        {
            get { return _readBytes; }
        }

        /// <summary>
        /// Gets the number of bytes written out by this service.
        /// </summary>
        public Int64 WrittenBytes
        {
            get { return _writtenBytes; }
        }

        /// <summary>
        /// Gets the number of messages this services has read.
        /// </summary>
        public Int64 ReadMessages
        {
            get { return _readMessages; }
        }

        /// <summary>
        /// Gets the number of messages this service has written.
        /// </summary>
        public Int64 WrittenMessages
        {
            get { return _writtenMessages; }
        }

        /// <summary>
        /// Gets the number of read bytes per second.
        /// </summary>
        public Double ReadBytesThroughput
        {
            get
            {
                ResetThroughput();
                return _readBytesThroughput;
            }
        }

        /// <summary>
        /// Gets the number of written bytes per second.
        /// </summary>
        public Double WrittenBytesThroughput
        {
            get
            {
                ResetThroughput();
                return _writtenBytesThroughput;
            }
        }

        /// <summary>
        /// Gets the number of read messages per second.
        /// </summary>
        public Double ReadMessagesThroughput
        {
            get
            {
                ResetThroughput();
                return _readMessagesThroughput;
            }
        }

        /// <summary>
        /// Gets the number of written messages per second.
        /// </summary>
        public Double WrittenMessagesThroughput
        {
            get
            {
                ResetThroughput();
                return _writtenMessagesThroughput;
            }
        }

        /// <summary>
        /// Gets the maximum of the <see cref="ReadBytesThroughput"/>.
        /// </summary>
        public Double LargestReadBytesThroughput
        {
            get { return _largestReadBytesThroughput; }
        }

        /// <summary>
        /// Gets the maximum of the <see cref="WrittenBytesThroughput"/>.
        /// </summary>
        public Double LargestWrittenBytesThroughput
        {
            get { return _largestWrittenBytesThroughput; }
        }

        /// <summary>
        /// Gets the maximum of the <see cref="ReadMessagesThroughput"/>.
        /// </summary>
        public Double LargestReadMessagesThroughput
        {
            get { return _largestReadMessagesThroughput; }
        }

        /// <summary>
        /// Gets the maximum of the <see cref="WrittenMessagesThroughput"/>.
        /// </summary>
        public Double LargestWrittenMessagesThroughput
        {
            get { return _largestWrittenMessagesThroughput; }
        }

        /// <summary>
        /// Gets or sets the interval (seconds) between each throughput calculation. The default value is 3 seconds.
        /// </summary>
        public Int32 ThroughputCalculationInterval
        {
            get { return _throughputCalculationInterval; }
            set
            {
                if (value < 0)
                    throw new ArgumentException("ThroughputCalculationInterval should be greater than 0", "value");
                _throughputCalculationInterval = value;
            }
        }

        /// <summary>
        /// Gets the interval (milliseconds) between each throughput calculation.
        /// </summary>
        public Int64 ThroughputCalculationIntervalInMillis
        {
            get { return _throughputCalculationInterval * 1000L; }
        }

        internal DateTime LastThroughputCalculationTime
        {
            get { return _lastThroughputCalculationTime; }
            set { _lastThroughputCalculationTime = value; }
        }

        /// <summary>
        /// Gets the count of bytes scheduled for write.
        /// </summary>
        public Int32 ScheduledWriteBytes
        {
            get { return _scheduledWriteBytes; }
        }

        /// <summary>
        /// Gets the count of messages scheduled for write.
        /// </summary>
        public Int32 ScheduledWriteMessages
        {
            get { return _scheduledWriteMessages; }
        }

        /// <summary>
        /// Updates the throughput counters.
        /// </summary>
        public void UpdateThroughput(DateTime currentTime)
        {
            lock (_throughputCalculationLock)
            {
                Int64 interval = (Int64)(currentTime - _lastThroughputCalculationTime).TotalMilliseconds;
                Int64 minInterval = ThroughputCalculationIntervalInMillis;
                if (minInterval == 0 || interval < minInterval)
                {
                    return;
                }

                Int64 readBytes = _readBytes;
                Int64 writtenBytes = _writtenBytes;
                Int64 readMessages = _readMessages;
                Int64 writtenMessages = _writtenMessages;

                _readBytesThroughput = (readBytes - _lastReadBytes) * 1000.0 / interval;
                _writtenBytesThroughput = (writtenBytes - _lastWrittenBytes) * 1000.0 / interval;
                _readMessagesThroughput = (readMessages - _lastReadMessages) * 1000.0 / interval;
                _writtenMessagesThroughput = (writtenMessages - _lastWrittenMessages) * 1000.0 / interval;

                if (_readBytesThroughput > _largestReadBytesThroughput)
                {
                    _largestReadBytesThroughput = _readBytesThroughput;
                }
                if (_writtenBytesThroughput > _largestWrittenBytesThroughput)
                {
                    _largestWrittenBytesThroughput = _writtenBytesThroughput;
                }
                if (_readMessagesThroughput > _largestReadMessagesThroughput)
                {
                    _largestReadMessagesThroughput = _readMessagesThroughput;
                }
                if (_writtenMessagesThroughput > _largestWrittenMessagesThroughput)
                {
                    _largestWrittenMessagesThroughput = _writtenMessagesThroughput;
                }

                _lastReadBytes = readBytes;
                _lastWrittenBytes = writtenBytes;
                _lastReadMessages = readMessages;
                _lastWrittenMessages = writtenMessages;

                _lastThroughputCalculationTime = currentTime;
            }
        }

        /// <summary>
        /// Increases the count of read bytes.
        /// </summary>
        /// <param name="increment">the number of bytes read</param>
        /// <param name="currentTime">current time</param>
        public void IncreaseReadBytes(Int64 increment, DateTime currentTime)
        {
            Interlocked.Add(ref _readBytes, increment);
            _lastReadTime = currentTime;
        }

        /// <summary>
        /// Increases the count of read messages by 1 and sets the last read time to current time.
        /// </summary>
        /// <param name="currentTime">current time</param>
        public void IncreaseReadMessages(DateTime currentTime)
        {
            Interlocked.Increment(ref _readMessages);
            _lastReadTime = currentTime;
        }

        /// <summary>
        /// Increases the count of written bytes.
        /// </summary>
        /// <param name="increment">the number of bytes written</param>
        /// <param name="currentTime">current time</param>
        public void IncreaseWrittenBytes(Int32 increment, DateTime currentTime)
        {
            Interlocked.Add(ref _writtenBytes, increment);
            _lastWriteTime = currentTime;
        }

        /// <summary>
        /// Increases the count of written messages by 1 and sets the last write time to current time.
        /// </summary>
        /// <param name="currentTime">current time</param>
        public void IncreaseWrittenMessages(DateTime currentTime)
        {
            Interlocked.Increment(ref _writtenMessages);
            _lastWriteTime = currentTime;
        }

        /// <summary>
        /// Increments by <code>increment</code> the count of bytes scheduled for write.
        /// </summary>
        public void IncreaseScheduledWriteBytes(Int32 increment)
        {
            Interlocked.Add(ref _scheduledWriteBytes, increment);
        }

        /// <summary>
        /// Increments by 1 the count of messages scheduled for write.
        /// </summary>
        public void IncreaseScheduledWriteMessages()
        {
            Interlocked.Increment(ref _scheduledWriteMessages);
        }

        /// <summary>
        /// Decrements by 1 the count of messages scheduled for write.
        /// </summary>
        public void DecreaseScheduledWriteMessages()
        {
            Interlocked.Decrement(ref _scheduledWriteMessages);
        }

        private void ResetThroughput()
        {
            if (_service.ManagedSessions.Count == 0)
            {
                _readBytesThroughput = 0;
                _writtenBytesThroughput = 0;
                _readMessagesThroughput = 0;
                _writtenMessagesThroughput = 0;
            }
        }
    }
}
