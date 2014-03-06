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
        Boolean RtsEnable { get; set; }
        Boolean DtrEnable { get; set; }
    }
}
