using System;
using System.Text;
using Mina.Core.Session;

namespace Mina.Filter.Codec.PrefixedString
{
    /// <summary>
    /// A <see cref="IProtocolCodecFactory"/> that performs encoding and decoding
    /// of a string using a fixed-length length prefix.
    /// </summary>
    public class PrefixedStringCodecFactory : IProtocolCodecFactory
    {
        public const Int32 DefaultPrefixLength = 4;
        public const Int32 DefaultMaxDataLength = 2048;

        private readonly PrefixedStringEncoder _encoder;
        private readonly PrefixedStringDecoder _decoder;

        public PrefixedStringCodecFactory()
            : this(Encoding.Default)
        { }

        public PrefixedStringCodecFactory(Encoding encoding)
        {
            _encoder = new PrefixedStringEncoder(encoding);
            _decoder = new PrefixedStringDecoder(encoding);
        }

        /// <summary>
        /// Gets or sets the length of the length prefix (1, 2, or 4) used by the encoder.
        /// </summary>
        public Int32 EncoderPrefixLength
        {
            get { return _encoder.PrefixLength; }
            set { _encoder.PrefixLength = value; }
        }

        /// <summary>
        /// Gets or sets the allowed maximum size of an encoded String.
        /// <remarks>
        /// If the size of the encoded String exceeds this value, the encoder
        /// will throw a <see cref="ArgumentException"/>.
        /// The default value is <see cref="PrefixedStringCodecFactory.DefaultMaxDataLength"/>.
        /// </remarks>
        /// </summary>
        public Int32 EncoderMaxDataLength
        {
            get { return _encoder.MaxDataLength; }
            set { _encoder.MaxDataLength = value; }
        }

        /// <summary>
        /// Gets or sets the length of the length prefix (1, 2, or 4) used by the decoder.
        /// </summary>
        public Int32 DecoderPrefixLength
        {
            get { return _decoder.PrefixLength; }
            set { _decoder.PrefixLength = value; }
        }

        /// <summary>
        /// Gets or sets the maximum allowed value specified as data length in the decoded data.
        /// <remarks>
        /// Useful for preventing an OutOfMemory attack by the peer.
        /// The decoder will throw a <see cref="Core.Buffer.BufferDataException"/> when data length
        /// specified in the incoming data is greater than MaxDataLength.
        /// The default value is <see cref="PrefixedStringCodecFactory.DefaultMaxDataLength"/>.
        /// </remarks>
        /// </summary>
        public Int32 DecoderMaxDataLength
        {
            get { return _decoder.MaxDataLength; }
            set { _decoder.MaxDataLength = value; }
        }

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
    }
}
