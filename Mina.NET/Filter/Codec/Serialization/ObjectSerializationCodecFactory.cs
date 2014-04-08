using System;
using Mina.Core.Session;

namespace Mina.Filter.Codec.Serialization
{
    /// <summary>
    /// A <see cref="IProtocolCodecFactory"/> that serializes and deserializes objects.
    /// </summary>
    public class ObjectSerializationCodecFactory : IProtocolCodecFactory
    {
        private readonly ObjectSerializationEncoder _encoder = new ObjectSerializationEncoder();
        private readonly ObjectSerializationDecoder _decoder = new ObjectSerializationDecoder();

        /// <inheritdoc/>
        public IProtocolEncoder GetEncoder(IoSession session)
        {
            return _encoder;
        }

        /// <inheritdoc/>
        public IProtocolDecoder GetDecoder(IoSession session)
        {
            return _decoder;
        }

        /// <summary>
        /// Gets or sets the allowed maximum size of the encoded object.
        /// If the size of the encoded object exceeds this value, this encoder
        /// will throw a <see cref="ArgumentException"/>.  The default value
        /// is <see cref="Int32.MaxValue"/>.
        /// </summary>
        public Int32 EncoderMaxObjectSize
        {
            get { return _encoder.MaxObjectSize; }
            set { _encoder.MaxObjectSize = value; }
        }

        /// <summary>
        /// Gets or sets the allowed maximum size of the object to be decoded.
        /// If the size of the object to be decoded exceeds this value, this encoder
        /// will throw a <see cref="Core.Buffer.BufferDataException"/>.  The default value
        /// is <code>1048576</code> (1MB).
        /// </summary>
        public Int32 DecoderMaxObjectSize
        {
            get { return _decoder.MaxObjectSize; }
            set { _decoder.MaxObjectSize = value; }
        }
    }
}
