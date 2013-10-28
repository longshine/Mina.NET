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
        /// Returns the interval (milliseconds) between each throughput calculation.
        /// The default value is <tt>3</tt> seconds.
        /// </summary>
        Int64 ThroughputCalculationIntervalInMillis { get; }
        /// <summary>
        /// Returns idle time for the specified type of idleness in seconds.
        /// </summary>
        Int32 GetIdleTime(IdleStatus status);
        /// <summary>
        /// Sets idle time for the specified type of idleness in seconds.
        /// </summary>
        void SetIdleTime(IdleStatus status, Int32 idleTime);
        /// <summary>
        /// Sets all configuration properties retrieved from the specified config.
        /// </summary>
        void SetAll(IoSessionConfig config);
    }
}
