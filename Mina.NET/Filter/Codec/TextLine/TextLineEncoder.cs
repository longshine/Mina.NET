using System;
using System.Text;
using Mina.Core.Session;
using Mina.Core.Buffer;

namespace Mina.Filter.Codec.TextLine
{
    /// <summary>
    /// A <see cref="IProtocolEncoder"/> which encodes a string into a text line
    /// which ends with the delimiter.
    /// </summary>
    public class TextLineEncoder : IProtocolEncoder
    {
        private readonly Encoding _encoding;
        private readonly LineDelimiter _delimiter;
        private Int32 _maxLineLength = Int32.MaxValue;

        /// <summary>
        /// Instantiates with default <see cref="Encoding.Default"/> and <see cref="LineDelimiter.Unix"/>.
        /// </summary>
        public TextLineEncoder()
            : this(LineDelimiter.Unix)
        { }

        /// <summary>
        /// Instantiates with default <see cref="Encoding.Default"/> and given delimiter.
        /// </summary>
        /// <param name="delimiter">the delimiter string</param>
        public TextLineEncoder(String delimiter)
            : this(new LineDelimiter(delimiter))
        { }

        /// <summary>
        /// Instantiates with default <see cref="Encoding.Default"/> and given delimiter.
        /// </summary>
        /// <param name="delimiter">the <see cref="LineDelimiter"/></param>
        public TextLineEncoder(LineDelimiter delimiter)
            : this(Encoding.Default, delimiter)
        { }

        /// <summary>
        /// Instantiates with given encoding,
        /// and default <see cref="LineDelimiter.Unix"/>.
        /// </summary>
        /// <param name="encoding">the <see cref="Encoding"/></param>
        public TextLineEncoder(Encoding encoding)
            : this(encoding, LineDelimiter.Unix)
        { }

        /// <summary>
        /// Instantiates.
        /// </summary>
        /// <param name="encoding">the <see cref="Encoding"/></param>
        /// <param name="delimiter">the delimiter string</param>
        public TextLineEncoder(Encoding encoding, String delimiter)
            : this(encoding, new LineDelimiter(delimiter))
        { }

        /// <summary>
        /// Instantiates.
        /// </summary>
        /// <param name="encoding">the <see cref="Encoding"/></param>
        /// <param name="delimiter">the <see cref="LineDelimiter"/></param>
        public TextLineEncoder(Encoding encoding, LineDelimiter delimiter)
        {
            if (encoding == null)
                throw new ArgumentNullException("encoding");
            if (delimiter == null)
                throw new ArgumentNullException("delimiter");
            if (LineDelimiter.Auto.Equals(delimiter))
                throw new ArgumentException("AUTO delimiter is not allowed for encoder.");

            _encoding = encoding;
            _delimiter = delimiter;
        }

        /// <summary>
        /// Gets or sets the allowed maximum size of the encoded line.
        /// </summary>
        public Int32 MaxLineLength
        {
            get { return _maxLineLength; }
            set
            {
                if (value <= 0)
                    throw new ArgumentException("maxLineLength (" + value + ") should be a positive value");
                _maxLineLength = value;
            }
        }

        /// <inheritdoc/>
        public void Encode(IoSession session, Object message, IProtocolEncoderOutput output)
        {
            String value = message == null ? String.Empty : message.ToString();
            value += _delimiter.Value;
            Byte[] bytes = _encoding.GetBytes(value);
            if (bytes.Length > _maxLineLength)
                throw new ArgumentException("Line too long: " + bytes.Length);

            // TODO BufferAllocator
            IoBuffer buf = IoBuffer.Wrap(bytes);
            output.Write(buf);
        }

        /// <inheritdoc/>
        public void Dispose(IoSession session)
        {
            // Do nothing
        }
    }
}
