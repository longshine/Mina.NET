using System;
using System.Net.Sockets;
using Mina.Core.Buffer;

namespace Mina.Transport.Socket
{
    /// <summary>
    /// <see cref="IoBuffer"/> that use <see cref="SocketAsyncEventArgs"/>
    /// as internal implementation.
    /// </summary>
    public class SocketAsyncEventArgsBuffer : AbstractIoBuffer, IDisposable
    {
        private readonly SocketAsyncEventArgs _socketAsyncEventArgs;

        /// <summary>
        /// </summary>
        public SocketAsyncEventArgsBuffer(SocketAsyncEventArgs socketAsyncEventArgs)
            : base((IoBufferAllocator)null, -1,0, socketAsyncEventArgs.Count, socketAsyncEventArgs.Count)
        {
            _socketAsyncEventArgs = socketAsyncEventArgs;
        }

        /// <summary>
        /// </summary>
        public SocketAsyncEventArgsBuffer(IoBufferAllocator allocator, Int32 cap, Int32 lim)
            : this(allocator, new Byte[cap], 0, lim)
        { }

        /// <summary>
        /// </summary>
        public SocketAsyncEventArgsBuffer(IoBufferAllocator allocator, Byte[] buffer, Int32 offset, Int32 count)
            : base(allocator, -1, 0, count, buffer.Length)
        {
            _socketAsyncEventArgs = new SocketAsyncEventArgs();
            _socketAsyncEventArgs.SetBuffer(buffer, offset, count);
        }

        /// <summary>
        /// Gets the inner <see cref="SocketAsyncEventArgs"/>.
        /// </summary>
        public SocketAsyncEventArgs SocketAsyncEventArgs
        {
            get { return _socketAsyncEventArgs; }
        }

        /// <inheritdoc/>
        public override Boolean ReadOnly
        {
            get { return false; }
        }

        /// <inheritdoc/>
        public override Boolean HasArray
        {
            get { return true; }
        }

        /// <summary>
        /// Sets data buffer for inner <see cref="SocketAsyncEventArgs"/>.
        /// </summary>
        public void SetBuffer()
        {
            if (_socketAsyncEventArgs.Count != Limit)
                _socketAsyncEventArgs.SetBuffer(_socketAsyncEventArgs.Offset, Limit);
        }

        /// <inheritdoc/>
        public override Byte Get()
        {
            return _socketAsyncEventArgs.Buffer[Offset(NextGetIndex())];
        }

        /// <inheritdoc/>
        public override IoBuffer Get(Byte[] dst, Int32 offset, Int32 length)
        {
            CheckBounds(offset, length, dst.Length);
            if (length > Remaining)
                throw new BufferUnderflowException();
            Array.Copy(_socketAsyncEventArgs.Buffer, Offset(Position), dst, offset, length);
            Position += length;
            return this;
        }

        /// <inheritdoc/>
        public override Byte Get(Int32 index)
        {
            return _socketAsyncEventArgs.Buffer[Offset(CheckIndex(index))];
        }

        /// <inheritdoc/>
        public override ArraySegment<Byte> GetRemaining()
        {
            return new ArraySegment<Byte>(_socketAsyncEventArgs.Buffer, _socketAsyncEventArgs.Offset, Limit);
        }

        /// <inheritdoc/>
        public override IoBuffer Shrink()
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        protected override Int32 Offset(Int32 pos)
        {
            return _socketAsyncEventArgs.Offset + pos;
        }

        /// <inheritdoc/>
        protected override Byte GetInternal(Int32 i)
        {
            return _socketAsyncEventArgs.Buffer[i];
        }

        /// <inheritdoc/>
        protected override void PutInternal(Int32 i, Byte b)
        {
            _socketAsyncEventArgs.Buffer[i] = b;
        }

        /// <inheritdoc/>
        protected override void PutInternal(Byte[] src, Int32 offset, Int32 length)
        {
            System.Buffer.BlockCopy(src, offset, _socketAsyncEventArgs.Buffer, Offset(Position), length);
            Position += length;
        }

        /// <inheritdoc/>
        protected override void PutInternal(IoBuffer src)
        {
            ArraySegment<Byte> array = src.GetRemaining();
            if (array.Count > Remaining)
                throw new OverflowException();
            PutInternal(array.Array, array.Offset, array.Count);
            src.Position += array.Count;
        }

        /// <inheritdoc/>
        public override IoBuffer Compact()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override void Free()
        {
            // TODO free buffer?
        }

        /// <summary>
        /// Dispose resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose resources.
        /// </summary>
        protected virtual void Dispose(Boolean disposing)
        {
            if (disposing)
            {
                _socketAsyncEventArgs.Dispose();
            }
        }

        /// <inheritdoc/>
        protected override IoBuffer Slice0()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        protected override IoBuffer AsReadOnlyBuffer0()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        protected override IoBuffer Duplicate0()
        {
            throw new NotImplementedException();
        }
    }
}
