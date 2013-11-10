using System;
using System.Text;
using Mina.Core.Buffer;
using Mina.Core.Session;

namespace Mina.Filter.Codec.PrefixedString
{
    /// <summary>
    /// A <see cref="IProtocolDecoder"/> which decodes a String using a fixed-length length prefix.
    /// </summary>
    public class PrefixedStringDecoder : CumulativeProtocolDecoder
    {
        public PrefixedStringDecoder(Encoding encoding)
            : this(encoding, PrefixedStringCodecFactory.DefaultPrefixLength, PrefixedStringCodecFactory.DefaultMaxDataLength)
        { }

        public PrefixedStringDecoder(Encoding encoding, Int32 prefixLength)
            : this(encoding, prefixLength, PrefixedStringCodecFactory.DefaultMaxDataLength)
        { }

        public PrefixedStringDecoder(Encoding encoding, Int32 prefixLength, Int32 maxDataLength)
        {
            Encoding = encoding;
            PrefixLength = prefixLength;
            MaxDataLength = maxDataLength;
        }

        /// <summary>
        /// Gets or sets the length of the length prefix (1, 2, or 4).
        /// </summary>
        public Int32 PrefixLength { get; set; }

        /// <summary>
        /// Gets or sets the maximum allowed value specified as data length in the incoming data.
        /// </summary>
        public Int32 MaxDataLength { get; set; }

        /// <summary>
        /// Gets or set the text encoding.
        /// </summary>
        public Encoding Encoding { get; set; }

        protected override Boolean DoDecode(IoSession session, IoBuffer input, IProtocolDecoderOutput output)
        {
            if (input.PrefixedDataAvailable(PrefixLength, MaxDataLength))
            {
                String msg = input.GetPrefixedString(PrefixLength, Encoding);
                output.Write(msg);
                return true;
            }

            return false;
        }
    }
}
