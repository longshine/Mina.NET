using System;

namespace Mina.Core.Session
{
    /// <summary>
    /// The configuration of <see cref="IoSession"/>.
    /// </summary>
    public interface IoSessionConfig
    {
        Int32 ReadBufferSize { get; set; }
        /// <summary>
        /// Gets or sets the interval (seconds) between each throughput calculation.
        /// The default value is 3 seconds.
        /// </summary>
        UInt32 ThroughputCalculationInterval { get; set; }
        /// <summary>
        /// Returns the interval (milliseconds) between each throughput calculation.
        /// The default value is 3 seconds.
        /// </summary>
        UInt64 ThroughputCalculationIntervalInMillis { get; }
        /// <summary>
        /// Returns idle time for the specified type of idleness in seconds.
        /// </summary>
        UInt32 GetIdleTime(IdleStatus status);
        /// <summary>
        /// Returns idle time for the specified type of idleness in milliseconds.
        /// </summary>
        UInt64 GetIdleTimeInMillis(IdleStatus status);
        /// <summary>
        /// Sets idle time for the specified type of idleness in seconds.
        /// </summary>
        void SetIdleTime(IdleStatus status, UInt32 idleTime);
        /// <summary>
        /// Gets or sets idle time for <see cref="IdleStatus.ReaderIdle"/> in seconds.
        /// </summary>
        UInt32 ReaderIdleTime { get; set; }
        /// <summary>
        /// Gets or sets idle time for <see cref="IdleStatus.WriterIdle"/> in seconds.
        /// </summary>
        UInt32 WriterIdleTime { get; set; }
        /// <summary>
        /// Gets or sets idle time for <see cref="IdleStatus.BothIdle"/> in seconds.
        /// </summary>
        UInt32 BothIdleTime { get; set; }
        /// <summary>
        /// Gets or set write timeout in seconds.
        /// </summary>
        UInt32 WriteTimeout { get; set; }
        /// <summary>
        /// Gets write timeout in milliseconds.
        /// </summary>
        UInt64 WriteTimeoutInMillis { get; }
        /// <summary>
        /// Sets all configuration properties retrieved from the specified config.
        /// </summary>
        void SetAll(IoSessionConfig config);
    }
}
