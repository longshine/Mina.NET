using System;

namespace Mina.Core.Buffer
{
    /// <summary>
    /// Allocates <see cref="IoBuffer"/>s and manages them.
    /// Please implement this interface if you need more advanced memory management scheme.
    /// </summary>
    public interface IoBufferAllocator
    {
        /// <summary>
        /// Returns the buffer which is capable of the specified size.
        /// </summary>
        /// <param name="capacity">the capacity of the buffer</param>
        /// <returns>the allocated buffer</returns>
        /// <exception cref="ArgumentException">If the <paramref name="capacity"/> is a negative integer</exception>
        IoBuffer Allocate(Int32 capacity);
        /// <summary>
        /// Wraps the specified byte array into Mina.NET buffer.
        /// </summary>
        IoBuffer Wrap(Byte[] array);
        /// <summary>
        /// Wraps the specified byte array into Mina.NET buffer.
        /// </summary>
        /// <exception cref="IndexOutOfRangeException">
        /// If the preconditions on the <paramref name="offset"/> and <paramref name="length"/>
        /// parameters do not hold
        /// </exception>
        IoBuffer Wrap(Byte[] array, Int32 offset, Int32 length);
    }
}
