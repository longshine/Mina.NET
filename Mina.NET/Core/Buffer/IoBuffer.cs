using System;
using System.Text;

namespace Mina.Core.Buffer
{
    /// <summary>
    /// A byte buffer used by MINA applications.
    /// </summary>
    public abstract class IoBuffer : Buffer
    {
        private static IoBufferAllocator allocator = ByteBufferAllocator.Instance;

        public static IoBufferAllocator Allocator
        {
            get { return allocator; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();
                if (allocator != null && allocator != value)
                    allocator.Dispose();
                allocator = value;
            }
        }

        /// <summary>
        /// Returns the direct or heap buffer which is capable to store the specified amount of bytes.
        /// </summary>
        /// <param name="capacity">the capacity of the buffer</param>
        /// <returns>the allocated buffer</returns>
        /// <exception cref="ArgumentException">If the <paramref name="capacity"/> is a negative integer</exception>
        public static IoBuffer Allocate(Int32 capacity)
        {
            return allocator.Allocate(capacity);
        }

        /// <summary>
        /// Wraps the specified byte array into MINA buffer.
        /// </summary>
        public static IoBuffer Wrap(Byte[] array)
        {
            return allocator.Wrap(array);
        }

        /// <summary>
        /// Wraps the specified byte array into MINA buffer.
        /// </summary>
        /// <exception cref="IndexOutOfRangeException">
        /// If the preconditions on the <paramref name="offset"/> and <paramref name="length"/>
        /// parameters do not hold
        /// </exception>
        public static IoBuffer Wrap(Byte[] array, Int32 offset, Int32 length)
        {
            return allocator.Wrap(array, offset, length);
        }

        /// <summary>
        /// Normalizes the specified capacity of the buffer to power of 2, which is
        /// often helpful for optimal memory usage and performance. If it is greater
        /// than or equal to <see cref="Int32.MaxValue"/>, it returns
        /// <see cref="Int32.MaxValue"/>. If it is zero, it returns zero.
        /// </summary>
        /// <param name="requestedCapacity"></param>
        /// <returns></returns>
        public static Int32 NormalizeCapacity(Int32 requestedCapacity)
        {
            if (requestedCapacity < 0)
                return Int32.MaxValue;

            Int32 newCapacity = HighestOneBit(requestedCapacity);
            newCapacity <<= (newCapacity < requestedCapacity ? 1 : 0);
            return newCapacity < 0 ? Int32.MaxValue : newCapacity;
        }

        private static Int32 HighestOneBit(Int32 i)
        {
            i |= (i >> 1);
            i |= (i >> 2);
            i |= (i >> 4);
            i |= (i >> 8);
            i |= (i >> 16);
            return i - (i >> 1);
        }

        protected IoBuffer(Int32 mark, Int32 pos, Int32 lim, Int32 cap)
            : base(mark, pos, lim, cap)
        { }

        public abstract IoBufferAllocator BufferAllocator { get; }

        public abstract ByteOrder Order { get; set; }

        public new virtual Int32 Capacity
        {
            get { return base.Capacity; }
            set { throw new NotSupportedException(); }
        }

        public new virtual Int32 Position
        {
            get { return base.Position; }
            set { base.Position = value; }
        }

        public new virtual Int32 Limit
        {
            get { return base.Limit; }
            set { base.Limit = value; }
        }

        public new virtual Int32 Remaining
        {
            get { return base.Remaining; }
        }

        public new virtual Boolean HasRemaining
        {
            get { return base.HasRemaining; }
        }

        public abstract Boolean AutoExpand { get; set; }

        public abstract Boolean AutoShrink { get; set; }

        public abstract Boolean Derived { get; }

        public abstract Int32 MinimumCapacity { get; set; }

        /// <summary>
        /// Tells whether or not this buffer is backed by an accessible byte array.
        /// </summary>
        public abstract Boolean HasArray { get; }

        public new virtual IoBuffer Mark()
        {
            base.Mark();
            return this;
        }

        public new virtual IoBuffer Reset()
        {
            base.Reset();
            return this;
        }

        public new virtual IoBuffer Clear()
        {
            base.Clear();
            return this;
        }

        public new virtual IoBuffer Flip()
        {
            base.Flip();
            return this;
        }

        public new virtual IoBuffer Rewind()
        {
            base.Rewind();
            return this;
        }

        public abstract IoBuffer Expand(Int32 expectedRemaining);

        public abstract IoBuffer Expand(Int32 pos, Int32 expectedRemaining);

        public abstract IoBuffer Sweep();

        public abstract IoBuffer Sweep(Byte value);

        public abstract IoBuffer FillAndReset(Int32 size);

        public abstract IoBuffer FillAndReset(Byte value, Int32 size);

        public abstract IoBuffer Fill(Int32 size);

        public abstract IoBuffer Fill(Byte value, Int32 size);

        public abstract String GetHexDump();

        public abstract String GetHexDump(Int32 lengthLimit);

        /// <summary>
        /// Returns true if this buffer contains a data which has a data
        /// length as a prefix and the buffer has remaining data as enough as
        /// specified in the data length field.
        /// <remarks>
        /// Please notes that using this method can allow DoS (Denial of Service)
        /// attack in case the remote peer sends too big data length value.
        /// It is recommended to use <see cref="PrefixedDataAvailable(Int32 prefixLength, Int32 maxDataLength)"/> instead.
        /// </remarks>
        /// </summary>
        /// <param name="prefixLength">the length of the prefix field (1, 2, or 4)</param>
        /// <returns>true if data available</returns>
        public abstract Boolean PrefixedDataAvailable(Int32 prefixLength);

        /// <summary>
        /// Returns true if this buffer contains a data which has a data
        /// length as a prefix and the buffer has remaining data as enough as
        /// specified in the data length field.
        /// </summary>
        /// <param name="prefixLength">the length of the prefix field (1, 2, or 4)</param>
        /// <param name="maxDataLength">the allowed maximum of the read data length</param>
        /// <returns>true if data available</returns>
        public abstract Boolean PrefixedDataAvailable(Int32 prefixLength, Int32 maxDataLength);

        /// <summary>
        /// Returns the first occurence position of the specified byte from the
        /// current position to the current limit.
        /// </summary>
        /// <param name="b">the byte to find</param>
        /// <returns>-1 if the specified byte is not found</returns>
        public abstract Int32 IndexOf(Byte b);

        /// <summary>
        /// Reads a string which has a 16-bit length field before the actual encoded string.
        /// This method is a shortcut for <code>GetPrefixedString(2, encoding)</code>.
        /// </summary>
        /// <param name="encoding">the encoding of the string</param>
        /// <returns>the prefixed string</returns>
        public abstract String GetPrefixedString(Encoding encoding);

        /// <summary>
        /// Reads a string which has a length field before the actual encoded string.
        /// </summary>
        /// <param name="prefixLength">the length of the length field (1, 2, or 4)</param>
        /// <param name="encoding">the encoding of the string</param>
        /// <returns>the prefixed string</returns>
        public abstract String GetPrefixedString(Int32 prefixLength, Encoding encoding);

        /// <summary>
        /// Writes the string into this buffer which has a 16-bit length field
        /// before the actual encoded string.
        /// This method is a shortcut for <code>PutPrefixedString(value, 2, encoding)</code>.
        /// </summary>
        /// <param name="value">the string to write</param>
        /// <param name="encoding">the encoding of the string</param>
        public abstract IoBuffer PutPrefixedString(String value, Encoding encoding);

        /// <summary>
        /// Writes the string into this buffer which has a prefixLength field
        /// before the actual encoded string.
        /// </summary>
        /// <param name="value">the string to write</param>
        /// <param name="prefixLength">the length of the length field (1, 2, or 4)</param>
        /// <param name="encoding">the encoding of the string</param>
        public abstract IoBuffer PutPrefixedString(String value, Int32 prefixLength, Encoding encoding);

        public abstract Byte Get();
        public abstract Byte Get(Int32 index);
        public abstract IoBuffer Get(Byte[] dst, Int32 offset, Int32 length);
        public abstract ArraySegment<Byte> GetRemaining();
        public abstract void Free();

        public abstract IoBuffer Slice();

        public abstract IoBuffer Duplicate();

        public abstract IoBuffer AsReadOnlyBuffer();

        public abstract IoBuffer Skip(Int32 size);

        public abstract IoBuffer Put(Byte b);

        public abstract IoBuffer Put(Int32 i, Byte b);

        public abstract IoBuffer Put(Byte[] src, Int32 offset, Int32 length);

        public abstract IoBuffer Put(IoBuffer src);

        public abstract IoBuffer Compact();

        public abstract IoBuffer Put(Byte[] src);

        public abstract IoBuffer PutString(String s);

        public abstract IoBuffer PutString(String s, Encoding encoding);

        #region

        public abstract Char GetChar();

        public abstract Char GetChar(Int32 index);

        public abstract IoBuffer PutChar(Char value);

        public abstract IoBuffer PutChar(Int32 index, Char value);

        public abstract Int16 GetInt16();

        public abstract Int16 GetInt16(Int32 index);

        public abstract IoBuffer PutInt16(Int16 value);

        public abstract IoBuffer PutInt16(Int32 index, Int16 value);

        public abstract Int32 GetInt32();

        public abstract Int32 GetInt32(Int32 index);

        public abstract IoBuffer PutInt32(Int32 value);

        public abstract IoBuffer PutInt32(Int32 index, Int32 value);

        public abstract Int64 GetInt64();

        public abstract Int64 GetInt64(Int32 index);

        public abstract IoBuffer PutInt64(Int64 value);

        public abstract IoBuffer PutInt64(Int32 index, Int64 value);

        public abstract Single GetSingle();

        public abstract Single GetSingle(Int32 index);

        public abstract IoBuffer PutSingle(Single value);

        public abstract IoBuffer PutSingle(Int32 index, Single value);

        public abstract Double GetDouble();

        public abstract Double GetDouble(Int32 index);

        public abstract IoBuffer PutDouble(Double value);

        public abstract IoBuffer PutDouble(Int32 index, Double value);

        #endregion
    }
}
