#if !UNITY
using System;
using Mina.Core.Session;

namespace Mina.Transport.Serial
{
    /// <summary>
    /// An <see cref="IoSession"/> for serial communication transport.
    /// </summary>
    public interface ISerialSession : IoSession
    {
        /// <inheritdoc/>
        new ISerialSessionConfig Config { get; }
        /// <summary>
        /// <seealso cref="System.IO.Ports.SerialPort.RtsEnable"/>
        /// </summary>
        Boolean RtsEnable { get; set; }
        /// <summary>
        /// <seealso cref="System.IO.Ports.SerialPort.DtrEnable"/>
        /// </summary>
        Boolean DtrEnable { get; set; }
    }
}
#endif