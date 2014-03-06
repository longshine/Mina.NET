using System;
using Mina.Core.Session;

namespace Mina.Transport.Serial
{
    /// <summary>
    /// An <see cref="IoSessionConfig"/> for serial transport type.
    /// </summary>
    public interface ISerialSessionConfig : IoSessionConfig
    {
        /// <summary>
        /// Gets or set read timeout in seconds.
        /// </summary>
        Int32 ReadTimeout { get; set; }
        Int32 WriteBufferSize { get; set; }
        Int32 ReceivedBytesThreshold { get; set; }
    }
}
