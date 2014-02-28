using System;
using System.Net;

namespace Mina.Filter.Firewall
{
    /// <summary>
    /// A IP subnet using the CIDR notation. Currently, only IPv4 address are supported.
    /// </summary>
    public class Subnet
    {
        private const uint IP_MASK = 0x80000000;
        private const int BYTE_MASK = 0xFF;

        private IPAddress _subnet;
        private int _subnetInt;
        private int _subnetMask;
        private int _suffix;

        public Subnet(IPAddress subnet, int mask)
        {
            if (subnet == null)
                throw new ArgumentNullException("subnet");
            if (subnet.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                throw new ArgumentException("Only IPv4 supported");
            if (mask < 0 || mask > 32)
                throw new ArgumentException("Mask has to be an integer between 0 and 32");

            this._subnet = subnet;
            this._subnetInt = ToInt(subnet);
            this._suffix = mask;

            // binary mask for this subnet
            unchecked
            {
                this._subnetMask = (int)IP_MASK >> (mask - 1);
            }
        }

        public Boolean InSubnet(IPAddress address)
        {
            return ToSubnet(address) == _subnetInt;
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

        private static Int32 ToInt(IPAddress inetAddress)
        {
            Byte[] address = inetAddress.GetAddressBytes();
            Int32 result = 0;
            for (Int32 i = 0; i < address.Length; i++)
            {
                result <<= 8;
                result |= address[i] & BYTE_MASK;
            }
            return result;
        }

        private Int32 ToSubnet(IPAddress address)
        {
            return ToInt(address) & _subnetMask;
        }
    }
}
