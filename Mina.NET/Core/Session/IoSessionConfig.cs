using System;

namespace Mina.Core.Session
{
    /// <summary>
    /// The configuration of <see cref="IoSession"/>.
    /// </summary>
    public interface IoSessionConfig
    {
        /// <summary>
        /// Gets or sets the size for the read buffer.
        /// <remarks>
        /// The default value depends on the transport.
        /// For socket transport it is 2048.
        /// For serial transport it is 0, indicating the system's default buffer size.
        /// </remarks>
        /// </summary>
        Int32 ReadBufferSize { get; set; }
        /// <summary>
        /// Gets or sets the interval (seconds) between each throughput calculation.
        /// The default value is 3 seconds.
        /// </summary>
        Int32 ThroughputCalculationInterval { get; set; }
        /// <summary>
        /// Returns the interval (milliseconds) between each throughput calculation.
        /// The default value is 3 seconds.
        /// </summary>
        Int64 ThroughputCalculationIntervalInMillis { get; }
        /// <summary>
        /// Returns idle time for the specified type of idleness in seconds.
        /// </summary>
        Int32 GetIdleTime(IdleStatus status);
        /// <summary>
        /// Returns idle time for the specified type of idleness in milliseconds.
        /// </summary>
        Int64 GetIdleTimeInMillis(IdleStatus status);
        /// <summary>
        /// Sets idle time for the specified type of idleness in seconds.
        /// </summary>
        void SetIdleTime(IdleStatus status, Int32 idleTime);
        /// <summary>
        /// Gets or sets idle time for <see cref="IdleStatus.ReaderIdle"/> in seconds.
        /// </summary>
        Int32 ReaderIdleTime { get; set; }
        /// <summary>
        /// Gets or sets idle time for <see cref="IdleStatus.WriterIdle"/> in seconds.
        /// </summary>
        Int32 WriterIdleTime { get; set; }
        /// <summary>
        /// Gets or sets idle time for <see cref="IdleStatus.BothIdle"/> in seconds.
        /// </summary>
        Int32 BothIdleTime { get; set; }
        /// <summary>
        /// Gets or set write timeout in seconds.
        /// </summary>
        Int32 WriteTimeout { get; set; }
        /// <summary>
        /// Gets write timeout in milliseconds.
        /// </summary>
        Int64 WriteTimeoutInMillis { get; }
        /// <summary>
        /// Sets all configuration properties retrieved from the specified config.
        /// </summary>
        void SetAll(IoSessionConfig config);
    }
}
