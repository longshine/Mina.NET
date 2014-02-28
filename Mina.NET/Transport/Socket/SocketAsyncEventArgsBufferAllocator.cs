using System;
using Mina.Core.Buffer;

namespace Mina.Transport.Socket
{
    /// <summary>
    /// An <see cref="IoBufferAllocator"/> which allocates <see cref="SocketAsyncEventArgsBuffer"/>.
    /// </summary>
    public class SocketAsyncEventArgsBufferAllocator : IoBufferAllocator
    {
        /// <summary>
        /// Static instance.
        /// </summary>
        public static readonly SocketAsyncEventArgsBufferAllocator Instance = new SocketAsyncEventArgsBufferAllocator();

        /// <inheritdoc/>
        public IoBuffer Allocate(Int32 capacity)
        {
            if (capacity < 0)
                throw new ArgumentException("Capacity should be >= 0", "capacity");
            return new SocketAsyncEventArgsBuffer(this, capacity, capacity);
        }

        /// <inheritdoc/>
        public IoBuffer Wrap(Byte[] array)
        {
            return Wrap(array, 0, array.Length);
        }

        /// <inheritdoc/>
        public IoBuffer Wrap(Byte[] array, Int32 offset, Int32 length)
        {
            try
            {
                return new SocketAsyncEventArgsBuffer(this, array, offset, length);
            }
            catch (ArgumentException)
            {
                throw new IndexOutOfRangeException();
            }
        }
    }
}
