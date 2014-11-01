using System;

namespace Mina.Core.Service
{
    /// <summary>
    /// Provides meta-information that describes an <see cref="IoService"/>.
    /// </summary>
    public interface ITransportMetadata
    {
        /// <summary>
        /// Gets the name of the service provider.
        /// </summary>
        String ProviderName { get; }
        /// <summary>
        /// Gets the name of the service.
        /// </summary>
        String Name { get; }
        /// <summary>
        /// Returns <code>true</code> if the session of this transport type is
        /// <a href="http://en.wikipedia.org/wiki/Connectionless">connectionless</a>.
        /// </summary>
        Boolean Connectionless { get; }
        /// <summary>
        /// Returns <code>true</code> if the messages exchanged by the service can be
        /// <a href="http://en.wikipedia.org/wiki/IPv4#Fragmentation_and_reassembly">fragmented
        /// or reassembled</a> by its underlying transport.
        /// </summary>
        Boolean HasFragmentation { get; }
        /// <summary>
        /// Gets the type of the endpoint in this transport.
        /// </summary>
        Type EndPointType { get; }
    }
}
