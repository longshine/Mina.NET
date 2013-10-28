using System;
using Mina.Core.Session;

namespace Mina.Transport.Socket
{
    /// <summary>
    /// An <see cref="IoSessionConfig"/> for socket transport type.
    /// </summary>
    public interface ISocketSessionConfig : IoSessionConfig
    {
        Int32? ReceiveBufferSize { get; set; }
        Int32? SendBufferSize { get; set; }
        Boolean? NoDelay { get; set; }
        Int32? SoLinger { get; set; }
    }
}
