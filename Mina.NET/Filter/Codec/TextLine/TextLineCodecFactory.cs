using System;
using System.Text;
using Mina.Core.Session;

namespace Mina.Filter.Codec.TextLine
{
    /// <summary>
    /// A <see cref="IProtocolCodecFactory"/> that performs encoding and decoding between
    /// a text line data and a Java string object.  This codec is useful especially
    /// when you work with a text-based protocols such as SMTP and IMAP.
    /// </summary>
    public class TextLineCodecFactory : IProtocolCodecFactory
    {
        private readonly TextLineEncoder _encoder;
        private readonly TextLineDecoder _decoder;

        public TextLineCodecFactory()
            : this(Encoding.Default)
        { }

        public TextLineCodecFactory(Encoding encoding)
            : this(encoding, LineDelimiter.Unix, LineDelimiter.Auto)
        { }

        public TextLineCodecFactory(Encoding encoding, String encodingDelimiter, String decodingDelimiter)
        {
            _encoder = new TextLineEncoder(encoding, encodingDelimiter);
            _decoder = new TextLineDecoder(encoding, decodingDelimiter);
        }

        public TextLineCodecFactory(Encoding encoding, LineDelimiter encodingDelimiter, LineDelimiter decodingDelimiter)
        {
            _encoder = new TextLineEncoder(encoding, encodingDelimiter);
            _decoder = new TextLineDecoder(encoding, decodingDelimiter);
        }

        public IProtocolEncoder GetEncoder(IoSession session)
        {
            return _encoder;
        }

        public IProtocolDecoder GetDecoder(IoSession session)
        {
            return _decoder;
        }

        /// <summary>
        /// Gets or sets the allowed maximum size of the encoded line.
        /// </summary>
        public Int32 EncoderMaxLineLength
        {
            get { return _encoder.MaxLineLength; }
            set { _encoder.MaxLineLength = value; }
        }

        /// <summary>
        /// Gets or sets the allowed maximum size of the line to be decoded.
        /// </summary>
        public Int32 DecoderMaxLineLength
        {
            get { return _decoder.MaxLineLength; }
            set { _decoder.MaxLineLength = value; }
        }
    }
}
