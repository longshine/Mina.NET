using System;

namespace Mina.Core.Buffer
{
    /// <summary>
    /// A simplistic <see cref="IoBufferAllocator"/> which simply allocates a new
    /// buffer every time.
    /// </summary>
    public class ByteBufferAllocator : IoBufferAllocator
    {
        /// <summary>
        /// Static instace of <see cref="ByteBufferAllocator"/>.
        /// </summary>
        public static readonly ByteBufferAllocator Instance = new ByteBufferAllocator();

        /// <inheritdoc/>
        public IoBuffer Allocate(Int32 capacity)
        {
            if (capacity < 0)
                throw new ArgumentException("Capacity should be >= 0", "capacity");
            return new ByteBuffer(this, capacity, capacity);
        }

        /// <inheritdoc/>
        public IoBuffer Wrap(Byte[] array, Int32 offset, Int32 length)
        {
            try
            {
                return new ByteBuffer(this, array, offset, length);
            }
            catch (ArgumentException)
            {
                throw new IndexOutOfRangeException();
            }
        }

        /// <inheritdoc/>
        public IoBuffer Wrap(Byte[] array)
        {
            return Wrap(array, 0, array.Length);
        }
    }
}
