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

        /// <summary>
        /// Gets or sets the allocator used by new buffers.
        /// </summary>
        public static IoBufferAllocator Allocator
        {
            get { return allocator; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                if (allocator != null && allocator != value && allocator is IDisposable)
                    ((IDisposable)allocator).Dispose();
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

        /// <summary>
        /// 
        /// </summary>
        protected IoBuffer(Int32 mark, Int32 pos, Int32 lim, Int32 cap)
            : base(mark, pos, lim, cap)
        { }

        /// <summary>
        /// Gets the the allocator used by this buffer.
        /// </summary>
        public abstract IoBufferAllocator BufferAllocator { get; }

        /// <summary>
        /// Gets or sets the current byte order.
        /// </summary>
        public abstract ByteOrder Order { get; set; }

        /// <inheritdoc/>
        public new virtual Int32 Capacity
        {
            get { return base.Capacity; }
            set { throw new NotSupportedException(); }
        }

        /// <inheritdoc/>
        public new virtual Int32 Position
        {
            get { return base.Position; }
            set { base.Position = value; }
        }

        /// <inheritdoc/>
        public new virtual Int32 Limit
        {
            get { return base.Limit; }
            set { base.Limit = value; }
        }

        /// <inheritdoc/>
        public new virtual Int32 Remaining
        {
            get { return base.Remaining; }
        }

        /// <inheritdoc/>
        public new virtual Boolean HasRemaining
        {
            get { return base.HasRemaining; }
        }

        /// <summary>
        /// Turns on or off auto-expanding.
        /// </summary>
        public abstract Boolean AutoExpand { get; set; }

        /// <summary>
        /// Turns on or off auto-shrinking.
        /// </summary>
        public abstract Boolean AutoShrink { get; set; }

        /// <summary>
        /// Checks if this buffer is derived from another buffer
        /// via <see cref="Duplicate()"/>, <see cref="Slice()"/> or <see cref="AsReadOnlyBuffer()"/>.
        /// </summary>
        public abstract Boolean Derived { get; }

        /// <summary>
        /// Gets or sets the minimum capacity.
        /// </summary>
        public abstract Int32 MinimumCapacity { get; set; }

        /// <summary>
        /// Tells whether or not this buffer is backed by an accessible byte array.
        /// </summary>
        public abstract Boolean HasArray { get; }

        /// <inheritdoc/>
        public new virtual IoBuffer Mark()
        {
            base.Mark();
            return this;
        }

        /// <inheritdoc/>
        public new virtual IoBuffer Reset()
        {
            base.Reset();
            return this;
        }

        /// <inheritdoc/>
        public new virtual IoBuffer Clear()
        {
            base.Clear();
            return this;
        }

        /// <inheritdoc/>
        public new virtual IoBuffer Flip()
        {
            base.Flip();
            return this;
        }

        /// <inheritdoc/>
        public new virtual IoBuffer Rewind()
        {
            base.Rewind();
            return this;
        }

        /// <summary>
        /// Changes the capacity and limit of this buffer so this buffer get the
        /// specified <paramref name="expectedRemaining"/> room from the current position.
        /// </summary>
        /// <param name="expectedRemaining">the expected remaining room</param>
        /// <returns>itself</returns>
        public abstract IoBuffer Expand(Int32 expectedRemaining);

        /// <summary>
        /// Changes the capacity and limit of this buffer so this buffer get the
        /// specified <paramref name="expectedRemaining"/> room from the specified <paramref name="position"/>.
        /// </summary>
        /// <param name="position">the start position</param>
        /// <param name="expectedRemaining">the expected remaining room</param>
        /// <returns>itself</returns>
        public abstract IoBuffer Expand(Int32 position, Int32 expectedRemaining);

        /// <summary>
        /// Changes the capacity of this buffer so this buffer occupies
        /// as less memory as possible while retaining the position,
        /// limit and the buffer content between the position and limit.
        /// The capacity of the buffer never becomes less than <see cref="MinimumCapacity"/>.
        /// The mark is discarded once the capacity changes.
        /// </summary>
        /// <returns>itself</returns>
        public abstract IoBuffer Shrink();

        /// <summary>
        ///  Clears this buffer and fills its content with zero. The position is
        ///  set to zero, the limit is set to the capacity, and the mark is discarded.
        /// </summary>
        /// <returns>itself</returns>
        public abstract IoBuffer Sweep();

        /// <summary>
        ///  Clears this buffer and fills its content with <paramref name="value"/>. The position is
        ///  set to zero, the limit is set to the capacity, and the mark is discarded.
        /// </summary>
        /// <returns>itself</returns>
        public abstract IoBuffer Sweep(Byte value);

        /// <summary>
        /// Fills this buffer with zero.
        /// This method does not change buffer position.
        /// </summary>
        /// <returns>itself</returns>
        public abstract IoBuffer FillAndReset(Int32 size);

        /// <summary>
        /// Fills this buffer with <paramref name="value"/>.
        /// This method does not change buffer position.
        /// </summary>
        /// <returns>itself</returns>
        public abstract IoBuffer FillAndReset(Byte value, Int32 size);

        /// <summary>
        /// Fills this buffer with zero.
        /// This method moves buffer position forward.
        /// </summary>
        /// <returns>itself</returns>
        public abstract IoBuffer Fill(Int32 size);

        /// <summary>
        /// Fills this buffer with <paramref name="value"/>.
        /// This method moves buffer position forward.
        /// </summary>
        /// <returns>itself</returns>
        public abstract IoBuffer Fill(Byte value, Int32 size);

        /// <summary>
        /// Gets hexdump of this buffer.
        /// </summary>
        /// <returns>hexidecimal representation of this buffer</returns>
        public abstract String GetHexDump();

        /// <summary>
        /// Gets hexdump of this buffer with limited length.
        /// </summary>
        /// <param name="lengthLimit">the maximum number of bytes to dump from the current buffer</param>
        /// <returns>hexidecimal representation of this buffer</returns>
        public abstract String GetHexDump(Int32 lengthLimit);

        /// <summary>
        /// Returns true if this buffer contains a data which has a data
        /// length as a prefix and the buffer has remaining data as enough as
        /// specified in the data length field.
        /// <remarks>
        /// Please notes that using this method can allow DoS (Denial of Service)
        /// attack in case the remote peer sends too big data length value.
        /// It is recommended to use <see cref="PrefixedDataAvailable(Int32, Int32)"/> instead.
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

        /// <summary>
        /// Reads an object from the buffer.
        /// </summary>
        public abstract Object GetObject();

        /// <summary>
        /// Writes the specified object to the buffer.
        /// </summary>
        public abstract IoBuffer PutObject(Object o);

        /// <summary>
        /// Reads the byte at this buffer's current position, and then increments the position. 
        /// </summary>
        /// <returns>the byte at the buffer's current position</returns>
        /// <exception cref="BufferUnderflowException">
        /// the buffer's current position is not smaller than its limit
        /// </exception>
        public abstract Byte Get();
        /// <summary>
        /// Reads the byte at the given index.
        /// </summary>
        /// <param name="index">the index from which the byte will be read</param>
        /// <returns>the byte at the given index</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// the index is negative or not smaller than the buffer's limit
        /// </exception>
        public abstract Byte Get(Int32 index);
        /// <summary>
        /// Reads bytes of <paramref name="length"/> into <paramref name="dst"/> array.
        /// </summary>
        /// <param name="dst">the array into which bytes are to be written</param>
        /// <param name="offset">the offset within the array of the first byte to be written</param>
        /// <param name="length">the maximum number of bytes to be written</param>
        /// <returns>itself</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// the preconditions on the offset and length parameters do not hold
        /// </exception>
        /// <exception cref="BufferUnderflowException">
        /// there are fewer than length bytes remaining in this buffer
        /// </exception>
        public abstract IoBuffer Get(Byte[] dst, Int32 offset, Int32 length);
        /// <summary>
        /// Gets all remaining bytes as an <see cref="ArraySegment&lt;Byte&gt;"/>.
        /// </summary>
        public abstract ArraySegment<Byte> GetRemaining();
        /// <summary>
        /// Declares this buffer and all its derived buffers are not used anymore so
        /// that it can be reused by some implementations.
        /// </summary>
        public abstract void Free();

        /// <summary>
        /// Creates a new byte buffer whose content is a
        /// shared subsequence of this buffer's content.
        /// </summary>
        /// <remarks>
        /// The new buffer's position will be zero, its capacity and its limit
        /// will be the number of bytes remaining in this buffer, and its mark
        /// will be undefined.
        /// </remarks>
        /// <returns>the new buffer</returns>
        public abstract IoBuffer Slice();

        public abstract IoBuffer GetSlice(Int32 index, Int32 length);

        public abstract IoBuffer GetSlice(Int32 length);

        /// <summary>
        /// Creates a new byte buffer that shares this buffer's content. 
        /// </summary>
        /// <remarks>
        /// The two buffers' position, limit, and mark values will be independent.
        /// </remarks>
        /// <returns>the new buffer</returns>
        public abstract IoBuffer Duplicate();

        /// <summary>
        /// Creates a new, read-only byte buffer that shares this buffer's content.
        /// </summary>
        /// <returns>the new, read-only buffer</returns>
        public abstract IoBuffer AsReadOnlyBuffer();

        /// <summary>
        /// Forwards the position of this buffer as the specified <paramref name="size"/> bytes.
        /// </summary>
        /// <returns>itself</returns>
        public abstract IoBuffer Skip(Int32 size);

        /// <summary>
        /// Writes the given byte into this buffer at the current position,
        /// and then increments the position.
        /// </summary>
        /// <param name="b">the byte to be written</param>
        /// <returns>itself</returns>
        public abstract IoBuffer Put(Byte b);

        /// <summary>
        /// Writes the given byte into this buffer at the given index.
        /// </summary>
        /// <param name="i">the index at which the byte will be written</param>
        /// <param name="b">the byte to be written</param>
        /// <returns>itself</returns>
        public abstract IoBuffer Put(Int32 i, Byte b);

        /// <summary>
        /// Writes the given array into this buffer at the current position,
        /// and then increments the position.
        /// </summary>
        /// <param name="src">the array from which bytes are to be read</param>
        /// <param name="offset">the offset within the array of the first byte to be read</param>
        /// <param name="length">the number of bytes to be read from the given array</param>
        /// <returns>itself</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// the preconditions on the offset and length parameters do not hold
        /// </exception>
        /// <exception cref="OverflowException">there is insufficient space in this buffer</exception>
        /// <exception cref="InvalidOperationException">this buffer is read-only</exception>
        public abstract IoBuffer Put(Byte[] src, Int32 offset, Int32 length);

        /// <summary>
        /// Writes the content of the specified <paramref name="src"/> into this buffer.
        /// </summary>
        /// <returns>itself</returns>
        public abstract IoBuffer Put(IoBuffer src);

        /// <summary>
        /// Compacts this buffer.
        /// </summary>
        /// <returns>itself</returns>
        public abstract IoBuffer Compact();

        /// <summary>
        /// Writes the given array into this buffer at the current position,
        /// and then increments the position.
        /// </summary>
        /// <remarks>
        /// This method behaves in exactly the same way as
        /// <example>
        /// Put(src, 0, src.Length)
        /// </example>
        /// </remarks>
        /// <param name="src">the array from which bytes are to be read</param>
        /// <returns>itself</returns>
        /// <exception cref="OverflowException">there is insufficient space in this buffer</exception>
        /// <exception cref="InvalidOperationException">this buffer is read-only</exception>
        public abstract IoBuffer Put(Byte[] src);

        /// <summary>
        /// Writes the content of <paramref name="s"/> into this buffer using <see cref="Encoding.UTF8"/>.
        /// </summary>
        /// <returns>itself</returns>
        /// <exception cref="OverflowException">there is insufficient space in this buffer</exception>
        /// <exception cref="InvalidOperationException">this buffer is read-only</exception>
        public abstract IoBuffer PutString(String s);

        /// <summary>
        /// Writes the content of <paramref name="s"/> into this buffer using
        /// the specified <see cref="Encoding"/>.
        /// This method doesn't terminate string with <tt>NUL</tt>.
        /// You have to do it by yourself.
        /// </summary>
        /// <returns>itself</returns>
        /// <exception cref="OverflowException">there is insufficient space in this buffer</exception>
        /// <exception cref="InvalidOperationException">this buffer is read-only</exception>
        public abstract IoBuffer PutString(String s, Encoding encoding);

        /// <summary>
        /// Writes the content of <paramref name="s"/> into this buffer as a
        /// <code>NUL</code>-terminated string using the specified <see cref="Encoding"/>.
        /// </summary>
        /// <remarks>
        /// If the charset name of the encoder is UTF-16, you cannot specify odd
        /// <code>fieldSize</code>, and this method will append two <code>NUL</code>s
        /// as a terminator.
        /// Please note that this method doesn't terminate with <code>NUL</code> if
        /// the input string is longer than <tt>fieldSize</tt>.
        /// </remarks>
        /// <param name="s"></param>
        /// <param name="fieldSize">the maximum number of bytes to write</param>
        /// <param name="encoding"></param>
        public abstract IoBuffer PutString(String s, Int32 fieldSize, Encoding encoding);

        /// <summary>
        /// Reads a NUL-terminated string from this buffer using the specified encoding.
        /// This method reads until the limit of this buffer if no NUL is found.
        /// </summary>
        public abstract String GetString(Encoding encoding);

        /// <summary>
        /// Reads a NUL-terminated string from this buffer using the specified decoder and returns it.
        /// </summary>
        /// <param name="fieldSize">the maximum number of bytes to read</param>
        /// <param name="encoding"></param>
        public abstract String GetString(Int32 fieldSize, Encoding encoding);

        #region

        /// <summary>
        /// Reads the next two bytes at this buffer's current position,
        /// composing them into a char value according to the current byte order,
        /// and then increments the position by two.
        /// </summary>
        /// <returns>the char value at the buffer's current position</returns>
        /// <exception cref="BufferUnderflowException">there are fewer than two bytes remaining in this buffer</exception>
        public abstract Char GetChar();

        /// <summary>
        /// Reads two bytes at the given index, composing them into a char value
        /// according to the current byte order.
        /// </summary>
        /// <param name="index">the index from which the bytes will be read</param>
        /// <returns>the char value at the given index</returns>
        /// <exception cref="IndexOutOfRangeException">index is negative or not smaller than the buffer's limit, minus one</exception>
        public abstract Char GetChar(Int32 index);

        /// <summary>
        /// Writes two bytes containing the given char value,
        /// in the current byte order, into this buffer at the current position,
        /// and then increments the position by two.
        /// </summary>
        /// <param name="value">the char value to be written</param>
        /// <returns>itself</returns>
        /// <exception cref="OverflowException">there are fewer than two bytes remaining in this buffer</exception>
        /// <exception cref="InvalidOperationException">this buffer is read-only</exception>
        public abstract IoBuffer PutChar(Char value);

        /// <summary>
        /// Writes two bytes containing the given char value, in the current byte order,
        /// into this buffer at the given index.
        /// </summary>
        /// <param name="index">the index at which the bytes will be written</param>
        /// <param name="value">the char value to be written</param>
        /// <returns>itself</returns>
        /// <exception cref="IndexOutOfRangeException">index is negative or not smaller than the buffer's limit, minus one</exception>
        /// <exception cref="InvalidOperationException">this buffer is read-only</exception>
        public abstract IoBuffer PutChar(Int32 index, Char value);

        /// <summary>
        /// Reads the next two bytes at this buffer's current position,
        /// composing them into a short value according to the current byte order,
        /// and then increments the position by two.
        /// </summary>
        /// <returns>the short value at the buffer's current position</returns>
        /// <exception cref="BufferUnderflowException">there are fewer than two bytes remaining in this buffer</exception>
        public abstract Int16 GetInt16();

        /// <summary>
        /// Reads two bytes at the given index, composing them into a short value
        /// according to the current byte order.
        /// </summary>
        /// <param name="index">the index from which the bytes will be read</param>
        /// <returns>the short value at the given index</returns>
        /// <exception cref="IndexOutOfRangeException">index is negative or not smaller than the buffer's limit, minus one</exception>
        public abstract Int16 GetInt16(Int32 index);

        /// <summary>
        /// Writes two bytes containing the given short value,
        /// in the current byte order, into this buffer at the current position,
        /// and then increments the position by two.
        /// </summary>
        /// <param name="value">the short value to be written</param>
        /// <returns>itself</returns>
        /// <exception cref="OverflowException">there are fewer than two bytes remaining in this buffer</exception>
        /// <exception cref="InvalidOperationException">this buffer is read-only</exception>
        public abstract IoBuffer PutInt16(Int16 value);

        /// <summary>
        /// Writes two bytes containing the given short value,
        /// in the current byte order, into this buffer at the given index.
        /// </summary>
        /// <param name="index">the index at which the bytes will be written</param>
        /// <param name="value">the short value to be written</param>
        /// <returns>itself</returns>
        /// <exception cref="IndexOutOfRangeException">index is negative or not smaller than the buffer's limit, minus one</exception>
        /// <exception cref="InvalidOperationException">this buffer is read-only</exception>
        public abstract IoBuffer PutInt16(Int32 index, Int16 value);

        /// <summary>
        /// Reads the next four bytes at this buffer's current position,
        /// composing them into a int value according to the current byte order,
        /// and then increments the position by four.
        /// </summary>
        /// <returns>the int value at the buffer's current position</returns>
        /// <exception cref="BufferUnderflowException">there are fewer than four bytes remaining in this buffer</exception>
        public abstract Int32 GetInt32();

        /// <summary>
        /// Reads four bytes at the given index, composing them into a int value
        /// according to the current byte order.
        /// </summary>
        /// <param name="index">the index from which the bytes will be read</param>
        /// <returns>the int value at the given index</returns>
        /// <exception cref="IndexOutOfRangeException">index is negative or not smaller than the buffer's limit, minus three</exception>
        public abstract Int32 GetInt32(Int32 index);

        /// <summary>
        /// Writes four bytes containing the given int value,
        /// in the current byte order, into this buffer at the current position,
        /// and then increments the position by four.
        /// </summary>
        /// <param name="value">the int value to be written</param>
        /// <returns>itself</returns>
        /// <exception cref="OverflowException">there are fewer than four bytes remaining in this buffer</exception>
        /// <exception cref="InvalidOperationException">this buffer is read-only</exception>
        public abstract IoBuffer PutInt32(Int32 value);

        /// <summary>
        /// Writes four bytes containing the given int value,
        /// in the current byte order, into this buffer at the given index.
        /// </summary>
        /// <param name="index">the index at which the bytes will be written</param>
        /// <param name="value">the int value to be written</param>
        /// <returns>itself</returns>
        /// <exception cref="IndexOutOfRangeException">index is negative or not smaller than the buffer's limit, minus three</exception>
        /// <exception cref="InvalidOperationException">this buffer is read-only</exception>
        public abstract IoBuffer PutInt32(Int32 index, Int32 value);

        /// <summary>
        /// Reads the next eight bytes at this buffer's current position,
        /// composing them into a long value according to the current byte order,
        /// and then increments the position by eight.
        /// </summary>
        /// <returns>the long value at the buffer's current position</returns>
        /// <exception cref="BufferUnderflowException">there are fewer than eight bytes remaining in this buffer</exception>
        public abstract Int64 GetInt64();

        /// <summary>
        /// Reads eight bytes at the given index, composing them into a long value
        /// according to the current byte order.
        /// </summary>
        /// <param name="index">the index from which the bytes will be read</param>
        /// <returns>the long value at the given index</returns>
        /// <exception cref="IndexOutOfRangeException">index is negative or not smaller than the buffer's limit, minus seven</exception>
        public abstract Int64 GetInt64(Int32 index);

        /// <summary>
        /// Writes eight bytes containing the given long value,
        /// in the current byte order, into this buffer at the current position,
        /// and then increments the position by eight.
        /// </summary>
        /// <param name="value">the long value to be written</param>
        /// <returns>itself</returns>
        /// <exception cref="OverflowException">there are fewer than eight bytes remaining in this buffer</exception>
        /// <exception cref="InvalidOperationException">this buffer is read-only</exception>
        public abstract IoBuffer PutInt64(Int64 value);

        /// <summary>
        /// Writes eight bytes containing the given long value,
        /// in the current byte order, into this buffer at the given index.
        /// </summary>
        /// <param name="index">the index at which the bytes will be written</param>
        /// <param name="value">the long value to be written</param>
        /// <returns>itself</returns>
        /// <exception cref="IndexOutOfRangeException">index is negative or not smaller than the buffer's limit, minus seven</exception>
        /// <exception cref="InvalidOperationException">this buffer is read-only</exception>
        public abstract IoBuffer PutInt64(Int32 index, Int64 value);

        /// <summary>
        /// Reads the next four bytes at this buffer's current position,
        /// composing them into a float value according to the current byte order,
        /// and then increments the position by four.
        /// </summary>
        /// <returns>the float value at the buffer's current position</returns>
        /// <exception cref="BufferUnderflowException">there are fewer than four bytes remaining in this buffer</exception>
        public abstract Single GetSingle();

        /// <summary>
        /// Reads four bytes at the given index, composing them into a float value
        /// according to the current byte order.
        /// </summary>
        /// <param name="index">the index from which the bytes will be read</param>
        /// <returns>the float value at the given index</returns>
        /// <exception cref="IndexOutOfRangeException">index is negative or not smaller than the buffer's limit, minus three</exception>
        public abstract Single GetSingle(Int32 index);

        /// <summary>
        /// Writes four bytes containing the given float value,
        /// in the current byte order, into this buffer at the current position,
        /// and then increments the position by four.
        /// </summary>
        /// <param name="value">the float value to be written</param>
        /// <returns>itself</returns>
        /// <exception cref="OverflowException">there are fewer than four bytes remaining in this buffer</exception>
        /// <exception cref="InvalidOperationException">this buffer is read-only</exception>
        public abstract IoBuffer PutSingle(Single value);

        /// <summary>
        /// Writes four bytes containing the given float value,
        /// in the current byte order, into this buffer at the given index.
        /// </summary>
        /// <param name="index">the index at which the bytes will be written</param>
        /// <param name="value">the float value to be written</param>
        /// <returns>itself</returns>
        /// <exception cref="IndexOutOfRangeException">index is negative or not smaller than the buffer's limit, minus three</exception>
        /// <exception cref="InvalidOperationException">this buffer is read-only</exception>
        public abstract IoBuffer PutSingle(Int32 index, Single value);

        /// <summary>
        /// Reads the next eight bytes at this buffer's current position,
        /// composing them into a double value according to the current byte order,
        /// and then increments the position by eight.
        /// </summary>
        /// <returns>the double value at the buffer's current position</returns>
        /// <exception cref="BufferUnderflowException">there are fewer than eight bytes remaining in this buffer</exception>
        public abstract Double GetDouble();

        /// <summary>
        /// Reads eight bytes at the given index, composing them into a double value
        /// according to the current byte order.
        /// </summary>
        /// <param name="index">the index from which the bytes will be read</param>
        /// <returns>the double value at the given index</returns>
        /// <exception cref="IndexOutOfRangeException">index is negative or not smaller than the buffer's limit, minus seven</exception>
        public abstract Double GetDouble(Int32 index);

        /// <summary>
        /// Writes eight bytes containing the given double value,
        /// in the current byte order, into this buffer at the current position,
        /// and then increments the position by eight.
        /// </summary>
        /// <param name="value">the double value to be written</param>
        /// <returns>itself</returns>
        /// <exception cref="OverflowException">there are fewer than eight bytes remaining in this buffer</exception>
        /// <exception cref="InvalidOperationException">this buffer is read-only</exception>
        public abstract IoBuffer PutDouble(Double value);

        /// <summary>
        /// Writes eight bytes containing the given double value,
        /// in the current byte order, into this buffer at the given index.
        /// </summary>
        /// <param name="index">the index at which the bytes will be written</param>
        /// <param name="value">the double value to be written</param>
        /// <returns>itself</returns>
        /// <exception cref="IndexOutOfRangeException">index is negative or not smaller than the buffer's limit, minus seven</exception>
        /// <exception cref="InvalidOperationException">this buffer is read-only</exception>
        public abstract IoBuffer PutDouble(Int32 index, Double value);

        #endregion
    }
}
