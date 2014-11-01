using System;
using System.Collections.Generic;
using System.Text;
using Mina.Core.Buffer;
using Mina.Core.Session;

namespace Mina.Filter.Codec.TextLine
{
    /// <summary>
    /// A <see cref="IProtocolDecoder"/> which decodes a text line into a string.
    /// </summary>
    public class TextLineDecoder : IProtocolDecoder
    {
        private readonly AttributeKey CONTEXT;
        private readonly Encoding _encoding;
        private readonly LineDelimiter _delimiter;
        private Int32 _maxLineLength = 1024;
        private Int32 _bufferLength = 128;
        private Byte[] _delimBuf;

        /// <summary>
        /// Instantiates with default <see cref="Encoding.Default"/> and <see cref="LineDelimiter.Auto"/>.
        /// </summary>
        public TextLineDecoder()
            : this(LineDelimiter.Auto)
        { }

        /// <summary>
        /// Instantiates with default <see cref="Encoding.Default"/> and given delimiter.
        /// </summary>
        /// <param name="delimiter">the delimiter string</param>
        public TextLineDecoder(String delimiter)
            : this(new LineDelimiter(delimiter))
        { }

        /// <summary>
        /// Instantiates with default <see cref="Encoding.Default"/> and given delimiter.
        /// </summary>
        /// <param name="delimiter">the <see cref="LineDelimiter"/></param>
        public TextLineDecoder(LineDelimiter delimiter)
            : this(Encoding.Default, delimiter)
        { }

        /// <summary>
        /// Instantiates with given encoding,
        /// and default <see cref="LineDelimiter.Auto"/>.
        /// </summary>
        /// <param name="encoding">the <see cref="Encoding"/></param>
        public TextLineDecoder(Encoding encoding)
            : this(encoding, LineDelimiter.Auto)
        { }

        /// <summary>
        /// Instantiates.
        /// </summary>
        /// <param name="encoding">the <see cref="Encoding"/></param>
        /// <param name="delimiter">the delimiter string</param>
        public TextLineDecoder(Encoding encoding, String delimiter)
            : this(encoding, new LineDelimiter(delimiter))
        { }

        /// <summary>
        /// Instantiates.
        /// </summary>
        /// <param name="encoding">the <see cref="Encoding"/></param>
        /// <param name="delimiter">the <see cref="LineDelimiter"/></param>
        public TextLineDecoder(Encoding encoding, LineDelimiter delimiter)
        {
            if (encoding == null)
                throw new ArgumentNullException("encoding");
            if (delimiter == null)
                throw new ArgumentNullException("delimiter");

            CONTEXT = new AttributeKey(GetType(), "context");
            _encoding = encoding;
            _delimiter = delimiter;

            _delimBuf = encoding.GetBytes(delimiter.Value);
        }

        /// <summary>
        /// Gets or sets the max length allowed for a line.
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

        /// <summary>
        /// Gets or sets the lenght of inner buffer.
        /// </summary>
        public Int32 BufferLength
        {
            get { return _bufferLength; }
            set
            {
                if (value <= 0)
                    throw new ArgumentException("bufferLength (" + value + ") should be a positive value");
                _bufferLength = value;
            }
        }

        /// <inheritdoc/>
        public void Decode(IoSession session, IoBuffer input, IProtocolDecoderOutput output)
        {
            Context ctx = GetContext(session);

            if (LineDelimiter.Auto.Equals(_delimiter))
                DecodeAuto(ctx, session, input, output);
            else
                DecodeNormal(ctx, session, input, output);
        }

        /// <inheritdoc/>
        public void FinishDecode(IoSession session, IProtocolDecoderOutput output)
        {
            // Do nothing
        }

        /// <inheritdoc/>
        public void Dispose(IoSession session)
        {
            session.RemoveAttribute(CONTEXT);
        }

        /// <summary>
        /// By default, this method propagates the decoded line of text to <see cref="IProtocolDecoderOutput"/>.
        /// You may override this method to modify the default behavior.
        /// </summary>
        protected virtual void WriteText(IoSession session, String text, IProtocolDecoderOutput output)
        {
            output.Write(text);
        }

        private void DecodeAuto(Context ctx, IoSession session, IoBuffer input, IProtocolDecoderOutput output)
        {
            Int32 matchCount = ctx.MatchCount;

            // Try to find a match
            Int32 oldPos = input.Position, oldLimit = input.Limit;

            while (input.HasRemaining)
            {
                Byte b = input.Get();
                Boolean matched = false;
                
                switch (b)
                {
                    case 0x0d: // \r
                        // Might be Mac, but we don't auto-detect Mac EOL
                        // to avoid confusion.
                        matchCount++;
                        break;
                    case 0x0a: // \n
                        // UNIX
                        matchCount++;
                        matched = true;
                        break;
                    default:
                        matchCount = 0;
                        break;
                }

                if (matched)
                {
                    // Found a match.
                    Int32 pos = input.Position;
                    input.Limit = pos;
                    input.Position = oldPos;

                    ctx.Append(input);

                    input.Limit = oldLimit;
                    input.Position = pos;

                    if (ctx.OverflowPosition == 0)
                    {
                        IoBuffer buf = ctx.Buffer;
                        buf.Flip();
                        buf.Limit -= matchCount;
                        ArraySegment<Byte> bytes = buf.GetRemaining();
                        try
                        {
                            String str = _encoding.GetString(bytes.Array, bytes.Offset, bytes.Count);
                            WriteText(session, str, output);
                        }
                        finally
                        {
                            buf.Clear();
                        }
                    }
                    else
                    {
                        Int32 overflowPosition = ctx.OverflowPosition;
                        ctx.Reset();
                        throw new RecoverableProtocolDecoderException("Line is too long: " + overflowPosition);
                    }

                    oldPos = pos;
                    matchCount = 0;
                }
            }

            // Put remainder to buf.
            input.Position = oldPos;
            ctx.Append(input);
            ctx.MatchCount = matchCount;
        }

        private void DecodeNormal(Context ctx, IoSession session, IoBuffer input, IProtocolDecoderOutput output)
        {
            Int32 matchCount = ctx.MatchCount;

            // Try to find a match
            Int32 oldPos = input.Position, oldLimit = input.Limit;

            while (input.HasRemaining)
            {
                Byte b = input.Get();

                if (_delimBuf[matchCount] == b)
                {
                    matchCount++;

                    if (matchCount == _delimBuf.Length)
                    {
                        // Found a match.
                        Int32 pos = input.Position;
                        input.Limit = pos;
                        input.Position = oldPos;

                        ctx.Append(input);

                        input.Limit = oldLimit;
                        input.Position = pos;

                        if (ctx.OverflowPosition == 0)
                        {
                            IoBuffer buf = ctx.Buffer;
                            buf.Flip();
                            buf.Limit -= matchCount;
                            ArraySegment<Byte> bytes = buf.GetRemaining();
                            try
                            {
                                String str = _encoding.GetString(bytes.Array, bytes.Offset, bytes.Count);
                                WriteText(session, str, output);
                            }
                            finally
                            {
                                buf.Clear();
                            }
                        }
                        else
                        {
                            Int32 overflowPosition = ctx.OverflowPosition;
                            ctx.Reset();
                            throw new RecoverableProtocolDecoderException("Line is too long: " + overflowPosition);
                        }

                        oldPos = pos;
                        matchCount = 0;
                    }
                }
                else
                {
                    input.Position = Math.Max(0, input.Position - matchCount);
                    matchCount = 0;
                }
            }

            // Put remainder to buf.
            input.Position = oldPos;
            ctx.Append(input);
            ctx.MatchCount = matchCount;
        }

        private Context GetContext(IoSession session)
        {
            Context ctx = session.GetAttribute<Context>(CONTEXT);
            if (ctx == null)
            {
                ctx = new Context(this);
                session.SetAttribute(CONTEXT, ctx);
            }
            return ctx;
        }

        class Context
        {
            private readonly TextLineDecoder _textLineDecoder;
            private readonly IoBuffer _buf;
            private Int32 _overflowPosition;

            public Context(TextLineDecoder textLineDecoder)
            {
                _textLineDecoder = textLineDecoder;
                _buf = IoBuffer.Allocate(_textLineDecoder.BufferLength);
                _buf.AutoExpand = true;
            }

            public Int32 MatchCount { get; set; }

            public IoBuffer Buffer
            {
                get { return _buf; }
            }

            public Int32 OverflowPosition
            {
                get { return _overflowPosition; }
            }

            public void Reset()
            {
                _overflowPosition = 0;
                MatchCount = 0;
            }

            public void Append(IoBuffer input)
            {
                if (_overflowPosition != 0)
                {
                    Discard(input);
                }
                else if (_buf.Position > _textLineDecoder.MaxLineLength - input.Remaining)
                {
                    _overflowPosition = _buf.Position;
                    _buf.Clear();
                    Discard(input);
                }
                else
                {
                    _buf.Put(input);
                }
            }

            private void Discard(IoBuffer input)
            {
                if (Int32.MaxValue - input.Remaining < _overflowPosition)
                    _overflowPosition = Int32.MaxValue;
                else
                    _overflowPosition += input.Remaining;
                input.Position = input.Limit;
            }
        }
    }
}
