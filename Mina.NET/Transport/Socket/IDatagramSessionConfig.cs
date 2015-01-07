using System;
using Mina.Core.Session;

namespace Mina.Transport.Socket
{
    /// <summary>
    /// An <see cref="IoSessionConfig"/> for datagram transport type.
    /// </summary>
    public interface IDatagramSessionConfig : IoSessionConfig
    {
        /// <summary>
        /// <see cref="System.Net.Sockets.Socket.EnableBroadcast"/>
        /// </summary>
        Boolean? EnableBroadcast { get; set; }
        /// <summary>
        /// <see cref="System.Net.Sockets.Socket.ExclusiveAddressUse"/>
        /// </summary>
        Boolean? ExclusiveAddressUse { get; set; }
        /// <summary>
        /// Gets or sets if <see cref="System.Net.Sockets.SocketOptionName.ReuseAddress"/> is enabled.
        /// </summary>
        Boolean? ReuseAddress { get; set; }
        /// <summary>
        /// <see cref="System.Net.Sockets.Socket.ReceiveBufferSize"/>
        /// </summary>
        Int32? ReceiveBufferSize { get; set; }
        /// <summary>
        /// <see cref="System.Net.Sockets.Socket.SendBufferSize"/>
        /// </summary>
        Int32? SendBufferSize { get; set; }
        /// <summary>
        /// Gets or sets traffic class or <see cref="System.Net.Sockets.SocketOptionName.TypeOfService"/> in the IP datagram header.
        /// </summary>
        Int32? TrafficClass { get; set; }
        /// <summary>
        /// Gets or sets <see cref="System.Net.Sockets.MulticastOption"/>.
        /// </summary>
        System.Net.Sockets.MulticastOption MulticastOption { get; set; }
    }
}
