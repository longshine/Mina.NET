using System;
using System.Net;

namespace Mina.Transport.Loopback
{
    /// <summary>
    /// An endpoint which represents loopback port number.
    /// </summary>
    public class LoopbackEndPoint : EndPoint, IComparable<LoopbackEndPoint>
    {
        private readonly Int32 _port;

        /// <summary>
        /// Creates a new instance with the specifid port number.
        /// </summary>
        public LoopbackEndPoint(Int32 port)
        {
            _port = port;
        }

        /// <summary>
        /// Gets the port number.
        /// </summary>
        public Int32 Port
        {
            get { return _port; }
        }

        /// <inheritdoc/>
        public Int32 CompareTo(LoopbackEndPoint other)
        {
            return this._port.CompareTo(other._port);
        }

        /// <inheritdoc/>
        public override Int32 GetHashCode()
        {
            return _port.GetHashCode();
        }

        /// <inheritdoc/>
        public override Boolean Equals(Object obj)
        {
            if (obj == null)
                return false;
            if (Object.ReferenceEquals(this, obj))
                return true;
            LoopbackEndPoint other = obj as LoopbackEndPoint;
            return obj != null && this._port == other._port;
        }

        /// <inheritdoc/>
        public override String ToString()
        {
            return _port >= 0 ? ("vm:server:" + _port) : ("vm:client:" + -_port);
        }
    }
}
