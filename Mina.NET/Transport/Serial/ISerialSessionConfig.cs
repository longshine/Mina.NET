#if !UNITY
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
        /// <summary>
        /// <seealso cref="System.IO.Ports.SerialPort.WriteBufferSize"/>
        /// </summary>
        Int32 WriteBufferSize { get; set; }
        /// <summary>
        /// <seealso cref="System.IO.Ports.SerialPort.ReceivedBytesThreshold"/>
        /// </summary>
        Int32 ReceivedBytesThreshold { get; set; }
    }
}
#endif