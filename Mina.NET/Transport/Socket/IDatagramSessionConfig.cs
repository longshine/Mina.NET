using System;
using Mina.Core.Session;

namespace Mina.Transport.Socket
{
    /// <summary>
    /// An <see cref="IoSessionConfig"/> for datagram transport type.
    /// </summary>
    public interface IDatagramSessionConfig : IoSessionConfig
    {
        Boolean? EnableBroadcast { get; set; }
        Int32? ReceiveBufferSize { get; set; }
        Int32? SendBufferSize { get; set; }
    }
}
