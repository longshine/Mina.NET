using System;
using System.IO;

namespace Mina.Core.Buffer
{
    /// <summary>
    /// Wraps an <see cref="IoBuffer"/> as a stream.
    /// </summary>
    public class IoBufferStream : Stream
    {
        private readonly IoBuffer _buf;

        /// <summary>
        /// </summary>
        public IoBufferStream(IoBuffer buf)
        {
            _buf = buf;
        }

        /// <inheritdoc/>
        public override Boolean CanRead
        {
            get { return true; }
        }

        /// <inheritdoc/>
        public override Boolean CanSeek
        {
            get { return true; }
        }

        /// <inheritdoc/>
        public override Boolean CanWrite
        {
            get { return true; }
        }

        /// <inheritdoc/>
        public override void Flush()
        {
            // do nothing
        }

        /// <inheritdoc/>
        public override Int64 Length
        {
            get { return _buf.Remaining; }
        }

        /// <inheritdoc/>
        public override Int64 Position
        {
            get { return _buf.Position; }
            set { _buf.Position = (Int32)value; }
        }

        /// <inheritdoc/>
        public override Int32 Read(Byte[] buffer, Int32 offset, Int32 count)
        {
            Int32 read = Math.Min(_buf.Remaining, count);
            _buf.Get(buffer, offset, read);
            return read;
        }

        /// <inheritdoc/>
        public override Int64 Seek(Int64 offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = _buf.Remaining - offset;
                    break;
                default:
                    break;
            }
            return Position;
        }

        /// <inheritdoc/>
        public override void SetLength(Int64 value)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override void Write(Byte[] buffer, Int32 offset, Int32 count)
        {
            _buf.Put(buffer, offset, count);
        }
    }
}
