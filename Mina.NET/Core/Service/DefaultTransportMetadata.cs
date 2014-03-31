using System;

namespace Mina.Core.Service
{
    /// <summary>
    /// A default immutable implementation of <see cref="ITransportMetadata"/>.
    /// </summary>
    public class DefaultTransportMetadata : ITransportMetadata
    {
        private readonly String _providerName;
        private readonly String _name;
        private readonly Boolean _connectionless;
        private readonly Boolean _hasFragmentation;
        private readonly Type _endpointType;

        /// <summary>
        /// </summary>
        public DefaultTransportMetadata(String providerName, String name,
            Boolean connectionless, Boolean fragmentation, Type endpointType)
        {
            if (providerName == null)
                throw new ArgumentNullException("providerName");
            if (name == null)
                throw new ArgumentNullException("name");

            providerName = providerName.Trim().ToLowerInvariant();
            if (providerName.Length == 0)
                throw new ArgumentException("providerName is empty", "providerName");
            name = name.Trim().ToLowerInvariant();
            if (name.Length == 0)
                throw new ArgumentException("name is empty", "name");

            if (endpointType == null)
                throw new ArgumentNullException("endpointType");

            _providerName = providerName;
            _name = name;
            _connectionless = connectionless;
            _hasFragmentation = fragmentation;
            _endpointType = endpointType;
        }

        /// <inheritdoc/>
        public String ProviderName
        {
            get { return _providerName; }
        }

        /// <inheritdoc/>
        public String Name
        {
            get { return _name; }
        }

        /// <inheritdoc/>
        public Boolean Connectionless
        {
            get { return _connectionless; }
        }

        /// <inheritdoc/>
        public Boolean HasFragmentation
        {
            get { return _hasFragmentation; }
        }

        /// <inheritdoc/>
        public Type EndPointType
        {
            get { return _endpointType; }
        }

        /// <inheritdoc/>
        public override String ToString()
        {
            return _name;
        }
    }
}
