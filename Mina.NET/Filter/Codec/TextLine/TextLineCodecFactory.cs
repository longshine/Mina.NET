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

        /// <summary>
        /// Instantiates with <see cref="Encoding.Default"/> for encoding,
        /// <see cref="LineDelimiter.Unix"/> for encoding delimiter, and
        /// <see cref="LineDelimiter.Auto"/> for decoding delimiter.
        /// </summary>
        public TextLineCodecFactory()
            : this(Encoding.Default)
        { }

        /// <summary>
        /// Instantiates with given encoding,
        /// <see cref="LineDelimiter.Unix"/> for encoding delimiter, and
        /// <see cref="LineDelimiter.Auto"/> for decoding delimiter.
        /// </summary>
        /// <param name="encoding">the <see cref="Encoding"/></param>
        public TextLineCodecFactory(Encoding encoding)
            : this(encoding, LineDelimiter.Unix, LineDelimiter.Auto)
        { }

        /// <summary>
        /// Instantiates.
        /// </summary>
        /// <param name="encoding">the <see cref="Encoding"/></param>
        /// <param name="encodingDelimiter">the encoding delimiter string</param>
        /// <param name="decodingDelimiter">the decoding delimiter string</param>
        public TextLineCodecFactory(Encoding encoding, String encodingDelimiter, String decodingDelimiter)
        {
            _encoder = new TextLineEncoder(encoding, encodingDelimiter);
            _decoder = new TextLineDecoder(encoding, decodingDelimiter);
        }

        /// <summary>
        /// Instantiates.
        /// </summary>
        /// <param name="encoding">the <see cref="Encoding"/></param>
        /// <param name="encodingDelimiter">the encoding <see cref="LineDelimiter"/></param>
        /// <param name="decodingDelimiter">the decoding <see cref="LineDelimiter"/></param>
        public TextLineCodecFactory(Encoding encoding, LineDelimiter encodingDelimiter, LineDelimiter decodingDelimiter)
        {
            _encoder = new TextLineEncoder(encoding, encodingDelimiter);
            _decoder = new TextLineDecoder(encoding, decodingDelimiter);
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
