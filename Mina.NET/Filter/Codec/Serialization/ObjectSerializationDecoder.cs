using System;
using Mina.Core.Buffer;
using Mina.Core.Session;

namespace Mina.Filter.Codec.Serialization
{
    /// <summary>
    /// A <see cref="IProtocolDecoder"/> which deserializes <code>Serializable</code> objects,
    /// using <see cref="IoBuffer.GetObject()"/>.
    /// </summary>
    public class ObjectSerializationDecoder : CumulativeProtocolDecoder
    {
        private Int32 _maxObjectSize = 1048576; // 1MB

        /// <summary>
        /// Gets or sets the allowed maximum size of the object to be decoded.
        /// If the size of the object to be decoded exceeds this value, this encoder
        /// will throw a <see cref="BufferDataException"/>.  The default value
        /// is <code>1048576</code> (1MB).
        /// </summary>
        public Int32 MaxObjectSize
        {
            get { return _maxObjectSize; }
            set
            {
                if (value <= 0)
                    throw new ArgumentException("MaxObjectSize should be larger than zero.", "value");
                _maxObjectSize = value;
            }
        }

        /// <inheritdoc/>
        protected override Boolean DoDecode(IoSession session, IoBuffer input, IProtocolDecoderOutput output)
        {
            if (!input.PrefixedDataAvailable(4, _maxObjectSize))
                return false;

            input.GetInt32();
            output.Write(input.GetObject());
            return true;
        }
    }
}
