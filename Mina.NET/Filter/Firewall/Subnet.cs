using System;
using System.Net;

namespace Mina.Filter.Firewall
{
    /// <summary>
    /// A IP subnet using the CIDR notation. Currently, only IPv4 address are supported.
    /// </summary>
    public class Subnet
    {
        private const UInt32 IP_MASK_V4 = 0x80000000;
        private const UInt64 IP_MASK_V6 = 0x8000000000000000L;
        private const Int32 BYTE_MASK = 0xFF;

        private IPAddress _subnet;
        /// <summary>
        /// An int representation of a subnet for IPV4 addresses.
        /// </summary>
        private Int32 _subnetInt;
        /// <summary>
        /// An long representation of a subnet for IPV6 addresses.
        /// </summary>
        private Int64 _subnetLong;
        private Int64 _subnetMask;
        private Int32 _suffix;

        /// <summary>
        /// Creates a subnet from CIDR notation.
        /// For example, the subnet 192.168.0.0/24 would be created using the
        /// <see cref="IPAddress"/> 192.168.0.0 and the mask 24.
        /// </summary>
        /// <param name="subnet">the <see cref="IPAddress"/> of the subnet</param>
        /// <param name="mask">the mask</param>
        public Subnet(IPAddress subnet, Int32 mask)
        {
            if (subnet == null)
                throw new ArgumentNullException("subnet");

            if (subnet.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                if (mask < 0 || mask > 32)
                    throw new ArgumentException("Mask has to be an integer between 0 and 32 for an IPv4 address");
                this._subnet = subnet;
                this._subnetInt = ToInt(subnet);
                this._suffix = mask;
                
                // binary mask for this subnet
                unchecked
                {
                    this._subnetMask = (Int32)IP_MASK_V4 >> (mask - 1);
                }
            }
            else if (subnet.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                if (mask < 0 || mask > 128)
                    throw new ArgumentException("Mask has to be an integer between 0 and 128 for an IPv6 address");
                this._subnet = subnet;
                this._subnetLong = ToLong(subnet);
                this._suffix = mask;

                // binary mask for this subnet
                unchecked
                {
                    this._subnetMask = (Int64)IP_MASK_V6 >> (mask - 1);
                }
            }
            else
            {
                throw new ArgumentException("Unsupported address family: " + subnet.AddressFamily, "subnet");
            }
        }

        /// <summary>
        /// Checks if the <see cref="IPAddress"/> is within this subnet.
        /// </summary>
        /// <param name="address">the <see cref="IPAddress"/> to check</param>
        /// <returns>true if the address is within this subnet, otherwise false</returns>
        public Boolean InSubnet(IPAddress address)
        {
            if (address == null)
                throw new ArgumentNullException("address");
            else if (IPAddress.Any.Equals(address) || IPAddress.IPv6Any.Equals(address))
                return true;
            else if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                return ToSubnet32(address) == _subnetInt;
            else
                return ToSubnet64(address) == _subnetLong;
        }

        /// <inheritdoc/>
        public override String ToString()
        {
            return _subnet + "/" + _suffix;
        }

        /// <inheritdoc/>
        public override Boolean Equals(Object obj)
        {
            Subnet other = obj as Subnet;

            if (other == null)
                return false;

            return other._subnetInt == _subnetInt && other._suffix == _suffix;
        }

        /// <inheritdoc/>
        public override Int32 GetHashCode()
        {
            return 17 * _subnetInt + _suffix;
        }

        private Int32 ToSubnet32(IPAddress address)
        {
            return (Int32)(ToInt(address) & _subnetMask);
        }

        private Int64 ToSubnet64(IPAddress address)
        {
            return ToLong(address) & _subnetMask;
        }

        private static Int32 ToInt(IPAddress addr)
        {
            Byte[] address = addr.GetAddressBytes();
            Int32 result = 0;
            for (Int32 i = 0; i < address.Length; i++)
            {
                result <<= 8;
                result |= address[i] & BYTE_MASK;
            }
            return result;
        }

        private static Int64 ToLong(IPAddress addr)
        {
            Byte[] address = addr.GetAddressBytes();
            Int64 result = 0;

            for (Int32 i = 0; i < address.Length; i++)
            {
                result <<= 8;
                result |= (UInt32)(address[i] & BYTE_MASK);
            }

            return result;
        }
    }
}
