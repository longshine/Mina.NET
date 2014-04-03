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

        public SocketAsyncEventArgsBuffer Allocate(Int32 capacity)
        {
            if (capacity < 0)
                throw new ArgumentException("Capacity should be >= 0", "capacity");
            return new SocketAsyncEventArgsBuffer(this, capacity, capacity);
        }

        public SocketAsyncEventArgsBuffer Wrap(Byte[] array)
        {
            return Wrap(array, 0, array.Length);
        }

        public SocketAsyncEventArgsBuffer Wrap(Byte[] array, Int32 offset, Int32 length)
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

        IoBuffer IoBufferAllocator.Allocate(Int32 capacity)
        {
            return Allocate(capacity);
        }

        IoBuffer IoBufferAllocator.Wrap(Byte[] array)
        {
            return Wrap(array);
        }

        IoBuffer IoBufferAllocator.Wrap(Byte[] array, Int32 offset, Int32 length)
        {
            return Wrap(array, offset, length);
        }
    }
}
