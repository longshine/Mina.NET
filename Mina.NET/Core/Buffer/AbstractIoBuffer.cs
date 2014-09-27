using System;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

namespace Mina.Core.Buffer
{
    /// <summary>
    /// A base implementation of <see cref="IoBuffer"/>.
    /// </summary>
    public abstract class AbstractIoBuffer : IoBuffer
    {
        private ByteOrder _order = ByteOrder.BigEndian;
        private IoBufferAllocator _allocator;
        private Boolean _derived;
        private Boolean _autoExpand;
        private Boolean _autoShrink;
        private Boolean _recapacityAllowed = true;
        private Int32 _minimumCapacity;

        /// <summary>
        /// 
        /// </summary>
        protected AbstractIoBuffer(IoBufferAllocator allocator, Int32 mark, Int32 pos, Int32 lim, Int32 cap)
            : base(mark, pos, lim, cap)
        {
            _allocator = allocator;
            _recapacityAllowed = true;
            _derived = false;
            _minimumCapacity = cap;
        }

        /// <summary>
        /// 
        /// </summary>
        protected AbstractIoBuffer(AbstractIoBuffer parent, Int32 mark, Int32 pos, Int32 lim, Int32 cap)
            : base(mark, pos, lim, cap)
        {
            _allocator = parent._allocator;
            _recapacityAllowed = false;
            _derived = true;
            _minimumCapacity = parent._minimumCapacity;
        }

        /// <inheritdoc/>
        public override ByteOrder Order
        {
            get { return _order; }
            set { _order = value; }
        }

        /// <inheritdoc/>
        public override Int32 MinimumCapacity
        {
            get { return _minimumCapacity; }
            set
            {
                if (value < 0)
                    throw new ArgumentException("MinimumCapacity");
                _minimumCapacity = value;
            }
        }

        /// <inheritdoc/>
        public override IoBufferAllocator BufferAllocator
        {
            get { return _allocator; }
        }

        /// <inheritdoc/>
        public override Int32 Position
        {
            get { return base.Position; }
            set
            {
                base.Position = value;
                AutoExpand0(value, 0);
            }
        }

        /// <inheritdoc/>
        public override Int32 Limit
        {
            get { return base.Limit; }
            set
            {
                base.Limit = value;
                AutoExpand0(value, 0);
            }
        }

        /// <inheritdoc/>
        public override Boolean AutoExpand
        {
            get { return _autoExpand && _recapacityAllowed; }
            set
            {
                if (!_recapacityAllowed)
                    throw new InvalidOperationException("Derived buffers and their parent can't be expanded.");
                _autoExpand = value;
            }
        }

        /// <inheritdoc/>
        public override Boolean AutoShrink
        {
            get { return _autoShrink && _recapacityAllowed; }
            set
            {
                if (!_recapacityAllowed)
                    throw new InvalidOperationException("Derived buffers and their parent can't be shrinked.");
                _autoShrink = value;
            }
        }

        /// <inheritdoc/>
        public override Boolean Derived
        {
            get { return _derived; }
        }

        /// <inheritdoc/>
        public override IoBuffer Expand(Int32 expectedRemaining)
        {
            return Expand(Position, expectedRemaining, false);
        }

        /// <inheritdoc/>
        public override IoBuffer Expand(Int32 position, Int32 expectedRemaining)
        {
            return Expand(position, expectedRemaining, false);
        }

        /// <inheritdoc/>
        public override IoBuffer Sweep()
        {
            Clear();
            return FillAndReset(Remaining);
        }

        /// <inheritdoc/>
        public override IoBuffer Sweep(Byte value)
        {
            Clear();
            return FillAndReset(value, Remaining);
        }

        /// <inheritdoc/>
        public override IoBuffer FillAndReset(Int32 size)
        {
            AutoExpand0(size);
            Int32 pos = Position;
            try
            {
                Fill(size);
            }
            finally
            {
                Position = pos;
            }
            return this;
        }

        /// <inheritdoc/>
        public override IoBuffer FillAndReset(Byte value, Int32 size)
        {
            AutoExpand0(size);
            Int32 pos = Position;
            try
            {
                Fill(value, size);
            }
            finally
            {
                Position = pos;
            }
            return this;
        }

        /// <inheritdoc/>
        public override IoBuffer Fill(Int32 size)
        {
            AutoExpand0(size);
            Int32 q = size >> 3;
            Int32 r = size & 7;

            for (Int32 i = q; i > 0; i--)
            {
                PutInt64(0L);
            }

            q = r >> 2;
            r = r & 3;

            if (q > 0)
            {
                PutInt32(0);
            }

            q = r >> 1;
            r = r & 1;

            if (q > 0)
            {
                PutInt16((short)0);
            }

            if (r > 0)
            {
                Put((byte)0);
            }

            return this;
        }

        /// <inheritdoc/>
        public override IoBuffer Fill(Byte value, Int32 size)
        {
            AutoExpand0(size);
            Int32 q = size >> 3;
            Int32 r = size & 7;

            if (q > 0)
            {
                Int32 intValue = value | value << 8 | value << 16 | value << 24;
                Int64 longValue = intValue;
                longValue <<= 32;
                longValue |= (UInt32)intValue;

                for (Int32 i = q; i > 0; i--)
                {
                    PutInt64(longValue);
                }
            }

            q = r >> 2;
            r = r & 3;

            if (q > 0)
            {
                Int32 intValue = value | value << 8 | value << 16 | value << 24;
                PutInt32(intValue);
            }

            q = r >> 1;
            r = r & 1;

            if (q > 0)
            {
                Int16 shortValue = (Int16)(value | value << 8);
                PutInt16(shortValue);
            }

            if (r > 0)
            {
                Put(value);
            }

            return this;
        }

        /// <inheritdoc/>
        public override String GetHexDump()
        {
            return GetHexDump(Int32.MaxValue);
        }

        /// <inheritdoc/>
        public override String GetHexDump(Int32 lengthLimit)
        {
            return IoBufferHexDumper.GetHexdump(this, lengthLimit);
        }

        /// <inheritdoc/>
        public override Boolean PrefixedDataAvailable(Int32 prefixLength)
        {
            return PrefixedDataAvailable(prefixLength, Int32.MaxValue);
        }

        /// <inheritdoc/>
        public override Boolean PrefixedDataAvailable(Int32 prefixLength, Int32 maxDataLength)
        {
            if (Remaining < prefixLength)
                return false;

            Int32 dataLength;
            switch (prefixLength)
            {
                case 1:
                    dataLength = Get(Position) & 0xff;
                    break;
                case 2:
                    dataLength = GetInt16(Position) & 0xffff;
                    break;
                case 4:
                    dataLength = GetInt32(Position);
                    break;
                default:
                    throw new ArgumentException("Expect prefixLength (1,2,4) but " + prefixLength);
            }

            if (dataLength < 0 || dataLength > maxDataLength)
            {
                throw new BufferDataException("dataLength: " + dataLength);
            }

            return Remaining - prefixLength >= dataLength;
        }

        /// <inheritdoc/>
        public override Int32 IndexOf(Byte b)
        {
            if (HasArray)
            {
                ArraySegment<Byte> array = GetRemaining();
                for (Int32 i = 0; i < array.Count; i++)
                {
                    if (array.Array[i + array.Offset] == b)
                    {
                        return i + Position;
                    }
                }
            }
            else
            {
                Int32 beginPos = Position;
                Int32 limit = Limit;

                for (Int32 i = beginPos; i < limit; i++)
                {
                    if (Get(i) == b)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        /// <inheritdoc/>
        public override String GetPrefixedString(Encoding encoding)
        {
            return GetPrefixedString(2, encoding);
        }

        /// <inheritdoc/>
        public override String GetPrefixedString(Int32 prefixLength, Encoding encoding)
        {
            if (!PrefixedDataAvailable(prefixLength))
                throw new BufferUnderflowException();

            Int32 dataLength = 0;
            switch (prefixLength)
            {
                case 1:
                    dataLength = Get() & 0xff;
                    break;
                case 2:
                    dataLength = GetInt16() & 0xffff;
                    break;
                case 4:
                    dataLength = GetInt32();
                    break;
            }

            if (dataLength == 0)
                return String.Empty;

            Byte[] bytes = new Byte[dataLength];
            Get(bytes, 0, dataLength);
            return encoding.GetString(bytes, 0, dataLength);
        }

        /// <inheritdoc/>
        public override IoBuffer PutPrefixedString(String value, Encoding encoding)
        {
            return PutPrefixedString(value, 2, encoding);
        }

        /// <inheritdoc/>
        public override IoBuffer PutPrefixedString(String value, Int32 prefixLength, Encoding encoding)
        {
            Int32 maxLength;
            switch (prefixLength)
            {
                case 1:
                    maxLength = 255;
                    break;
                case 2:
                    maxLength = 65535;
                    break;
                case 4:
                    maxLength = Int32.MaxValue;
                    break;
                default:
                    throw new ArgumentException("prefixLength: " + prefixLength);
            }

            if (value.Length > maxLength)
                throw new ArgumentException("The specified string is too long.");

            if (value.Length == 0)
            {
                switch (prefixLength)
                {
                    case 1:
                        Put((Byte)0);
                        break;
                    case 2:
                        PutInt16((Int16)0);
                        break;
                    case 4:
                        PutInt32(0);
                        break;
                }
                return this;
            }

            Byte[] bytes = encoding.GetBytes(value);
            switch (prefixLength)
            {
                case 1:
                    Put((Byte)bytes.Length);
                    break;
                case 2:
                    PutInt16((Int16)bytes.Length);
                    break;
                case 4:
                    PutInt32(bytes.Length);
                    break;
            }
            Put(bytes);
            return this;
        }

        /// <inheritdoc/>
        public override Object GetObject()
        {
            IFormatter formatter = new BinaryFormatter();
            return formatter.Deserialize(new IoBufferStream(this));
        }

        /// <inheritdoc/>
        public override IoBuffer PutObject(Object o)
        {
            IFormatter formatter = new BinaryFormatter();
            formatter.Serialize(new IoBufferStream(this), o);
            return this;
        }

        /// <inheritdoc/>
        public override IoBuffer Slice()
        {
            _recapacityAllowed = false;
            return Slice0();
        }

        /// <inheritdoc/>
        public override IoBuffer GetSlice(Int32 index, Int32 length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException("length");

            Int32 pos = Position;
            Int32 limit = Limit;

            if (index > limit)
                throw new ArgumentOutOfRangeException("index");

            Int32 endIndex = index + length;

            if (endIndex > limit)
                throw new IndexOutOfRangeException("index + length (" + endIndex + ") is greater "
                    + "than limit (" + limit + ").");

            Clear();
            Limit = endIndex;
            Position = index;

            IoBuffer slice = Slice();
            Limit = limit;
            Position = pos;

            return slice;
        }

        /// <inheritdoc/>
        public override IoBuffer GetSlice(Int32 length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException("length");

            Int32 pos = Position;
            Int32 limit = Limit;
            Int32 nextPos = pos + length;
            if (limit < nextPos)
                throw new IndexOutOfRangeException("position + length (" + nextPos + ") is greater "
                    + "than limit (" + limit + ").");

            Limit = pos + length;
            IoBuffer slice = Slice();
            Position = nextPos;
            Limit = limit;
            return slice;
        }

        /// <inheritdoc/>
        public override IoBuffer Duplicate()
        {
            _recapacityAllowed = false;
            return Duplicate0();
        }

        /// <inheritdoc/>
        public override IoBuffer AsReadOnlyBuffer()
        {
            _recapacityAllowed = false;
            return AsReadOnlyBuffer0();
        }

        /// <inheritdoc/>
        public override IoBuffer Skip(Int32 size)
        {
            AutoExpand0(size);
            Position = Position + size;
            return this;
        }

        /// <inheritdoc/>
        public override IoBuffer Put(Byte b)
        {
            AutoExpand0(1);
            PutInternal(Offset(NextPutIndex()), b);
            return this;
        }

        /// <inheritdoc/>
        public override IoBuffer Put(Int32 i, Byte b)
        {
            AutoExpand0(i, 1);
            PutInternal(Offset(CheckIndex(i)), b);
            return this;
        }

        /// <inheritdoc/>
        public override IoBuffer Put(Byte[] src, Int32 offset, Int32 length)
        {
            CheckBounds(offset, length, src.Length);
            AutoExpand0(length);

            if (length > Remaining)
                throw new OverflowException();

            PutInternal(src, offset, length);
            return this;
        }

        /// <inheritdoc/>
        public override IoBuffer Put(IoBuffer src)
        {
            if (Object.ReferenceEquals(src, this))
                throw new ArgumentException("Cannot put myself", "src");

            AutoExpand0(src.Remaining);
            PutInternal(src);

            return this;
        }

        /// <inheritdoc/>
        public override IoBuffer Put(Byte[] src)
        {
            return Put(src, 0, src.Length);
        }

        /// <inheritdoc/>
        public override IoBuffer PutString(String s)
        {
            return PutString(s, Encoding.UTF8);
        }

        /// <inheritdoc/>
        public override IoBuffer PutString(String s, Encoding encoding)
        {
            if (String.IsNullOrEmpty(s))
                return this;
            else
                return Put(encoding.GetBytes(s));
        }

        /// <inheritdoc/>
        public override IoBuffer PutString(String s, Int32 fieldSize, Encoding encoding)
        {
            if (fieldSize < 0)
                throw new ArgumentException("fieldSize cannot be negative: " + fieldSize, "fieldSize");
            else if (fieldSize == 0)
                return this;

            AutoExpand0(fieldSize);
            Boolean utf16 = encoding.WebName.StartsWith("utf-16", StringComparison.OrdinalIgnoreCase);
         
            if (utf16 && (fieldSize & 1) != 0)
                throw new ArgumentException("fieldSize is not even.", "fieldSize");

            Int32 oldLimit = Limit;
            Int32 end = Position + fieldSize;

            if (oldLimit < end)
                throw new OverflowException();

            if (!String.IsNullOrEmpty(s))
            {
                Byte[] bytes = encoding.GetBytes(s);
                Put(bytes, 0, fieldSize < bytes.Length ? fieldSize : bytes.Length);
            }

            if (Position < end)
            {
                if (utf16)
                {
                    Put((Byte)0x00);
                    Put((Byte)0x00);
                }
                else
                {
                    Put((Byte)0x00);
                }
            }

            Position = end;
            return this;
        }

        /// <inheritdoc/>
        public override String GetString(Encoding encoding)
        {
            if (!HasRemaining)
                return String.Empty;

            Boolean utf16 = encoding.WebName.StartsWith("utf-16", StringComparison.OrdinalIgnoreCase);

            Int32 oldPos = Position;
            Int32 oldLimit = Limit;
            Int32 end = -1;
            Int32 newPos;

            if (utf16)
            {
                Int32 i = oldPos;
                while (true)
                {
                    Boolean wasZero = Get(i) == 0;
                    i++;

                    if (i >= oldLimit)
                        break;

                    if (Get(i) != 0)
                    {
                        i++;
                        if (i >= oldLimit)
                            break;
                        continue;
                    }

                    if (wasZero)
                    {
                        end = i - 1;
                        break;
                    }
                }

                if (end < 0)
                    newPos = end = oldPos + (Int32)(oldLimit - oldPos & 0xFFFFFFFE);
                else if (end + 2 <= oldLimit)
                    newPos = end + 2;
                else
                    newPos = end;
            }
            else
            {
                end = IndexOf(0x00);
                if (end < 0)
                    newPos = end = oldLimit;
                else
                    newPos = end + 1;
            }

            if (oldPos == end)
            {
                Position = newPos;
                return String.Empty;
            }

            Limit = end;

            String result;
            if (HasArray)
            {
                ArraySegment<Byte> array = GetRemaining();
                result = encoding.GetString(array.Array, array.Offset, array.Count);
            }
            else
            {
                Byte[] bytes = new Byte[Remaining];
                Get(bytes, 0, bytes.Length);
                result = encoding.GetString(bytes, 0, bytes.Length);
            }

            Limit = oldLimit;
            Position = newPos;
            return result;
        }

        /// <inheritdoc/>
        public override String GetString(Int32 fieldSize, Encoding encoding)
        {
            if (fieldSize < 0)
                throw new ArgumentException("fieldSize cannot be negative: " + fieldSize, "fieldSize");
            if (fieldSize == 0 || !HasRemaining)
                return String.Empty;

            Boolean utf16 = encoding.WebName.StartsWith("utf-16", StringComparison.OrdinalIgnoreCase);

            if (utf16 && (fieldSize & 1) != 0)
                throw new ArgumentException("fieldSize is not even.", "fieldSize");

            Int32 oldPos = Position;
            Int32 oldLimit = Limit;
            Int32 end = oldPos + fieldSize;

            if (oldLimit < end)
                throw new BufferUnderflowException();

            Int32 i;

            if (utf16)
            {
                for (i = oldPos; i < end; i += 2)
                {
                    if (Get(i) == 0 && Get(i + 1) == 0)
                        break;
                }

                Limit = i;
            }
            else
            {
                for (i = oldPos; i < end; i++)
                {
                    if (Get(i) == 0)
                        break;
                }

                Limit = i;
            }

            if (!HasRemaining)
            {
                Limit = oldLimit;
                Position = end;
                return String.Empty;
            }

            String result;
            if (HasArray)
            {
                ArraySegment<Byte> array = GetRemaining();
                result = encoding.GetString(array.Array, array.Offset, array.Count);
            }
            else
            {
                Byte[] bytes = new Byte[Remaining];
                Get(bytes, 0, bytes.Length);
                result = encoding.GetString(bytes, 0, bytes.Length);
            }

            Limit = oldLimit;
            Position = end;
            return result;
        }
        
        /// <summary>
        /// Indicating whether recapacity is allowed.
        /// </summary>
        protected Boolean RecapacityAllowed
        {
            get { return _recapacityAllowed; }
        }

        /// <summary>
        /// Gets the actual position in internal buffer of the given index.
        /// </summary>
        protected virtual Int32 Offset(Int32 i)
        {
            return i;
        }

        /// <summary>
        /// Writes an <see cref="IoBuffer"/>. Override this method for better implementation.
        /// </summary>
        protected virtual void PutInternal(IoBuffer src)
        {
            Int32 n = src.Remaining;
            if (n > Remaining)
                throw new OverflowException();
            for (Int32 i = 0; i < n; i++)
            {
                Put(src.Get());
            }
        }

        /// <summary>
        /// Writes an array of bytes. Override this method for better implementation.
        /// </summary>
        protected virtual void PutInternal(Byte[] src, Int32 offset, Int32 length)
        {
            Int32 end = offset + length;
            for (Int32 i = offset; i < end; i++)
                Put(src[i]);
        }

        /// <summary>
        /// Gets the byte at the given index in internal buffer.
        /// </summary>
        /// <param name="i">the index from which the byte will be read</param>
        /// <returns>the byte at the given index</returns>
        protected abstract Byte GetInternal(Int32 i);

        /// <summary>
        /// Pus the given byte into internal buffer at the given index.
        /// </summary>
        /// <param name="i">the index at which the byte will be written</param>
        /// <param name="b">the byte to be written</param>
        protected abstract void PutInternal(Int32 i, Byte b);

        /// <summary>
        /// <see cref="Slice()"/>
        /// </summary>
        protected abstract IoBuffer Slice0();
        /// <summary>
        /// <see cref="Duplicate()"/>
        /// </summary>
        protected abstract IoBuffer Duplicate0();
        /// <summary>
        /// <see cref="AsReadOnlyBuffer()"/>
        /// </summary>
        protected abstract IoBuffer AsReadOnlyBuffer0();

        private IoBuffer Expand(Int32 expectedRemaining, Boolean autoExpand)
        {
            return Expand(Position, expectedRemaining, autoExpand);
        }

        private IoBuffer Expand(Int32 pos, Int32 expectedRemaining, Boolean autoExpand)
        {
            if (!_recapacityAllowed)
                throw new InvalidOperationException("Derived buffers and their parent can't be expanded.");

            Int32 end = pos + expectedRemaining;
            Int32 newCapacity;
            if (autoExpand)
                newCapacity = NormalizeCapacity(end);
            else
                newCapacity = end;

            if (newCapacity > Capacity)
            {
                // The buffer needs expansion.
                Capacity = newCapacity;
            }

            if (end > Limit)
            {
                // We call base.Limit directly to prevent StackOverflowError
                base.Limit = end;
            }

            return this;
        }

        private void AutoExpand0(int expectedRemaining)
        {
            if (AutoExpand)
                Expand(expectedRemaining, true);
        }

        private void AutoExpand0(int pos, int expectedRemaining)
        {
            if (AutoExpand)
                Expand(pos, expectedRemaining, true);
        }

        #region

        /// <inheritdoc/>
        public override Char GetChar()
        {
            return Bits.GetChar(this, Offset(NextGetIndex(2)), Order == ByteOrder.BigEndian);
        }

        /// <inheritdoc/>
        public override Char GetChar(Int32 index)
        {
            return Bits.GetChar(this, Offset(CheckIndex(index, 2)), Order == ByteOrder.BigEndian);
        }

        /// <inheritdoc/>
        public override IoBuffer PutChar(Char value)
        {
            AutoExpand0(2);
            Bits.PutChar(this, Offset(NextPutIndex(2)), value, Order == ByteOrder.BigEndian);
            return this;
        }

        /// <inheritdoc/>
        public override IoBuffer PutChar(Int32 index, Char value)
        {
            AutoExpand0(index, 2);
            Bits.PutChar(this, Offset(CheckIndex(index, 2)), value, Order == ByteOrder.BigEndian);
            return this;
        }

        /// <inheritdoc/>
        public override Int16 GetInt16()
        {
            return Bits.GetShort(this, Offset(NextGetIndex(2)), Order == ByteOrder.BigEndian);
        }

        /// <inheritdoc/>
        public override Int16 GetInt16(Int32 index)
        {
            return Bits.GetShort(this, Offset(CheckIndex(index, 2)), Order == ByteOrder.BigEndian);
        }

        /// <inheritdoc/>
        public override IoBuffer PutInt16(Int16 value)
        {
            AutoExpand0(2);
            Bits.PutShort(this, Offset(NextPutIndex(2)), value, Order == ByteOrder.BigEndian);
            return this;
        }

        /// <inheritdoc/>
        public override IoBuffer PutInt16(Int32 index, Int16 value)
        {
            AutoExpand0(index, 2);
            Bits.PutShort(this, Offset(CheckIndex(index, 2)), value, Order == ByteOrder.BigEndian);
            return this;
        }

        /// <inheritdoc/>
        public override Int32 GetInt32()
        {
            return Bits.GetInt(this, Offset(NextGetIndex(4)), Order == ByteOrder.BigEndian);
        }

        /// <inheritdoc/>
        public override Int32 GetInt32(Int32 index)
        {
            return Bits.GetInt(this, Offset(CheckIndex(index, 4)), Order == ByteOrder.BigEndian);
        }

        /// <inheritdoc/>
        public override IoBuffer PutInt32(Int32 value)
        {
            AutoExpand0(4);
            Bits.PutInt(this, Offset(NextPutIndex(4)), value, Order == ByteOrder.BigEndian);
            return this;
        }

        /// <inheritdoc/>
        public override IoBuffer PutInt32(Int32 index, Int32 value)
        {
            AutoExpand0(index, 4);
            Bits.PutInt(this, Offset(CheckIndex(index, 4)), value, Order == ByteOrder.BigEndian);
            return this;
        }

        /// <inheritdoc/>
        public override Int64 GetInt64()
        {
            return Bits.GetLong(this, Offset(NextGetIndex(8)), Order == ByteOrder.BigEndian);
        }

        /// <inheritdoc/>
        public override Int64 GetInt64(Int32 index)
        {
            return Bits.GetLong(this, Offset(CheckIndex(index, 8)), Order == ByteOrder.BigEndian);
        }

        /// <inheritdoc/>
        public override IoBuffer PutInt64(Int64 value)
        {
            AutoExpand0(8);
            Bits.PutLong(this, Offset(NextPutIndex(8)), value, Order == ByteOrder.BigEndian);
            return this;
        }

        /// <inheritdoc/>
        public override IoBuffer PutInt64(Int32 index, Int64 value)
        {
            AutoExpand0(index, 8);
            Bits.PutLong(this, Offset(CheckIndex(index, 8)), value, Order == ByteOrder.BigEndian);
            return this;
        }

        /// <inheritdoc/>
        public override Single GetSingle()
        {
            return Bits.GetFloat(this, Offset(NextGetIndex(4)), Order == ByteOrder.BigEndian);
        }

        /// <inheritdoc/>
        public override Single GetSingle(Int32 index)
        {
            return Bits.GetFloat(this, Offset(CheckIndex(index, 4)), Order == ByteOrder.BigEndian);
        }

        /// <inheritdoc/>
        public override IoBuffer PutSingle(Single value)
        {
            AutoExpand0(4);
            Bits.PutFloat(this, Offset(NextPutIndex(4)), value, Order == ByteOrder.BigEndian);
            return this;
        }

        /// <inheritdoc/>
        public override IoBuffer PutSingle(Int32 index, Single value)
        {
            AutoExpand0(index, 4);
            Bits.PutFloat(this, Offset(CheckIndex(index, 4)), value, Order == ByteOrder.BigEndian);
            return this;
        }

        /// <inheritdoc/>
        public override Double GetDouble()
        {
            return Bits.GetDouble(this, Offset(NextGetIndex(8)), Order == ByteOrder.BigEndian);
        }

        /// <inheritdoc/>
        public override Double GetDouble(Int32 index)
        {
            return Bits.GetDouble(this, Offset(CheckIndex(index, 8)), Order == ByteOrder.BigEndian);
        }

        /// <inheritdoc/>
        public override IoBuffer PutDouble(Double value)
        {
            AutoExpand0(8);
            Bits.PutDouble(this, Offset(NextPutIndex(8)), value, Order == ByteOrder.BigEndian);
            return this;
        }

        /// <inheritdoc/>
        public override IoBuffer PutDouble(Int32 index, Double value)
        {
            AutoExpand0(index, 8);
            Bits.PutDouble(this, Offset(CheckIndex(index, 8)), value, Order == ByteOrder.BigEndian);
            return this;
        }

        static class Bits
        {
            public static short Swap(short x)
            {
                return (short)((x << 8) |
                           ((x >> 8) & 0xff));
            }

            public static char Swap(char x)
            {
                return (char)((x << 8) |
                          ((x >> 8) & 0xff));
            }

            public static int Swap(int x)
            {
                return (int)((Swap((short)x) << 16) |
                         (Swap((short)(x >> 16)) & 0xffff));
            }

            public static long Swap(long x)
            {
                return (long)(((long)Swap((int)(x)) << 32) |
                          ((long)Swap((int)(x >> 32)) & 0xffffffffL));
            }

            // -- get/put char --

            private static char MakeChar(byte b1, byte b0)
            {
                return (char)((b1 << 8) | (b0 & 0xff));
            }

            public static char GetCharL(AbstractIoBuffer bb, int bi)
            {
                return MakeChar(bb.GetInternal(bi + 1),
                        bb.GetInternal(bi + 0));
            }

            public static char GetCharB(AbstractIoBuffer bb, int bi)
            {
                return MakeChar(bb.GetInternal(bi + 0),
                        bb.GetInternal(bi + 1));
            }

            public static char GetChar(AbstractIoBuffer bb, int bi, Boolean bigEndian)
            {
                return (bigEndian ? GetCharB(bb, bi) : GetCharL(bb, bi));
            }

            private static byte Char1(char x) { return (byte)(x >> 8); }
            private static byte Char0(char x) { return (byte)(x >> 0); }

            public static void PutCharL(AbstractIoBuffer bb, int bi, char x)
            {
                bb.PutInternal(bi + 0, Char0(x));
                bb.PutInternal(bi + 1, Char1(x));
            }

            public static void PutCharB(AbstractIoBuffer bb, int bi, char x)
            {
                bb.PutInternal(bi + 0, Char1(x));
                bb.PutInternal(bi + 1, Char0(x));
            }

            public static void PutChar(AbstractIoBuffer bb, int bi, char x, Boolean bigEndian)
            {
                if (bigEndian)
                    PutCharB(bb, bi, x);
                else
                    PutCharL(bb, bi, x);
            }

            // -- get/put short --

            private static short MakeShort(byte b1, byte b0)
            {
                return (short)((b1 << 8) | (b0 & 0xff));
            }

            public static short GetShortL(AbstractIoBuffer bb, int bi)
            {
                return MakeShort(bb.GetInternal(bi + 1),
                         bb.GetInternal(bi + 0));
            }

            public static short GetShortB(AbstractIoBuffer bb, int bi)
            {
                return MakeShort(bb.GetInternal(bi + 0),
                         bb.GetInternal(bi + 1));
            }

            public static short GetShort(AbstractIoBuffer bb, int bi, Boolean bigEndian)
            {
                return (bigEndian ? GetShortB(bb, bi) : GetShortL(bb, bi));
            }

            private static byte Short1(short x) { return (byte)(x >> 8); }
            private static byte Short0(short x) { return (byte)(x >> 0); }

            public static void PutShortL(AbstractIoBuffer bb, int bi, short x)
            {
                bb.PutInternal(bi + 0, Short0(x));
                bb.PutInternal(bi + 1, Short1(x));
            }

            public static void PutShortB(AbstractIoBuffer bb, int bi, short x)
            {
                bb.PutInternal(bi + 0, Short1(x));
                bb.PutInternal(bi + 1, Short0(x));
            }

            public static void PutShort(AbstractIoBuffer bb, int bi, short x, Boolean bigEndian)
            {
                if (bigEndian)
                    PutShortB(bb, bi, x);
                else
                    PutShortL(bb, bi, x);
            }

            // -- get/put int --

            private static int MakeInt(byte b3, byte b2, byte b1, byte b0)
            {
                return (int)((((b3 & 0xff) << 24) |
                          ((b2 & 0xff) << 16) |
                          ((b1 & 0xff) << 8) |
                          ((b0 & 0xff) << 0)));
            }

            public static int GetIntL(AbstractIoBuffer bb, int bi)
            {
                return MakeInt(bb.GetInternal(bi + 3),
                           bb.GetInternal(bi + 2),
                           bb.GetInternal(bi + 1),
                           bb.GetInternal(bi + 0));
            }

            public static int GetIntB(AbstractIoBuffer bb, int bi)
            {
                return MakeInt(bb.GetInternal(bi + 0),
                           bb.GetInternal(bi + 1),
                           bb.GetInternal(bi + 2),
                           bb.GetInternal(bi + 3));
            }

            public static int GetInt(AbstractIoBuffer bb, int bi, Boolean bigEndian)
            {
                return (bigEndian ? GetIntB(bb, bi) : GetIntL(bb, bi));
            }

            private static byte Int3(int x) { return (byte)(x >> 24); }
            private static byte Int2(int x) { return (byte)(x >> 16); }
            private static byte Int1(int x) { return (byte)(x >> 8); }
            private static byte Int0(int x) { return (byte)(x >> 0); }

            public static void PutIntL(AbstractIoBuffer bb, int bi, int x)
            {
                bb.PutInternal(bi + 3, Int3(x));
                bb.PutInternal(bi + 2, Int2(x));
                bb.PutInternal(bi + 1, Int1(x));
                bb.PutInternal(bi + 0, Int0(x));
            }

            public static void PutIntB(AbstractIoBuffer bb, int bi, int x)
            {
                bb.PutInternal(bi + 0, Int3(x));
                bb.PutInternal(bi + 1, Int2(x));
                bb.PutInternal(bi + 2, Int1(x));
                bb.PutInternal(bi + 3, Int0(x));
            }

            public static void PutInt(AbstractIoBuffer bb, int bi, int x, Boolean bigEndian)
            {
                if (bigEndian)
                    PutIntB(bb, bi, x);
                else
                    PutIntL(bb, bi, x);
            }

            // -- get/put long --

            private static long MakeLong(byte b7, byte b6, byte b5, byte b4,
                         byte b3, byte b2, byte b1, byte b0)
            {
                return ((((long)b7 & 0xff) << 56) |
                    (((long)b6 & 0xff) << 48) |
                    (((long)b5 & 0xff) << 40) |
                    (((long)b4 & 0xff) << 32) |
                    (((long)b3 & 0xff) << 24) |
                    (((long)b2 & 0xff) << 16) |
                    (((long)b1 & 0xff) << 8) |
                    (((long)b0 & 0xff) << 0));
            }

            public static long GetLongL(AbstractIoBuffer bb, int bi)
            {
                return MakeLong(bb.GetInternal(bi + 7),
                        bb.GetInternal(bi + 6),
                        bb.GetInternal(bi + 5),
                        bb.GetInternal(bi + 4),
                        bb.GetInternal(bi + 3),
                        bb.GetInternal(bi + 2),
                        bb.GetInternal(bi + 1),
                        bb.GetInternal(bi + 0));
            }

            public static long GetLongB(AbstractIoBuffer bb, int bi)
            {
                return MakeLong(bb.GetInternal(bi + 0),
                        bb.GetInternal(bi + 1),
                        bb.GetInternal(bi + 2),
                        bb.GetInternal(bi + 3),
                        bb.GetInternal(bi + 4),
                        bb.GetInternal(bi + 5),
                        bb.GetInternal(bi + 6),
                        bb.GetInternal(bi + 7));
            }

            public static long GetLong(AbstractIoBuffer bb, int bi, Boolean bigEndian)
            {
                return (bigEndian ? GetLongB(bb, bi) : GetLongL(bb, bi));
            }

            private static byte Long7(long x) { return (byte)(x >> 56); }
            private static byte Long6(long x) { return (byte)(x >> 48); }
            private static byte Long5(long x) { return (byte)(x >> 40); }
            private static byte Long4(long x) { return (byte)(x >> 32); }
            private static byte Long3(long x) { return (byte)(x >> 24); }
            private static byte Long2(long x) { return (byte)(x >> 16); }
            private static byte Long1(long x) { return (byte)(x >> 8); }
            private static byte Long0(long x) { return (byte)(x >> 0); }

            public static void PutLongL(AbstractIoBuffer bb, int bi, long x)
            {
                bb.PutInternal(bi + 7, Long7(x));
                bb.PutInternal(bi + 6, Long6(x));
                bb.PutInternal(bi + 5, Long5(x));
                bb.PutInternal(bi + 4, Long4(x));
                bb.PutInternal(bi + 3, Long3(x));
                bb.PutInternal(bi + 2, Long2(x));
                bb.PutInternal(bi + 1, Long1(x));
                bb.PutInternal(bi + 0, Long0(x));
            }

            public static void PutLongB(AbstractIoBuffer bb, int bi, long x)
            {
                bb.PutInternal(bi + 0, Long7(x));
                bb.PutInternal(bi + 1, Long6(x));
                bb.PutInternal(bi + 2, Long5(x));
                bb.PutInternal(bi + 3, Long4(x));
                bb.PutInternal(bi + 4, Long3(x));
                bb.PutInternal(bi + 5, Long2(x));
                bb.PutInternal(bi + 6, Long1(x));
                bb.PutInternal(bi + 7, Long0(x));
            }

            public static void PutLong(AbstractIoBuffer bb, int bi, long x, Boolean bigEndian)
            {
                if (bigEndian)
                    PutLongB(bb, bi, x);
                else
                    PutLongL(bb, bi, x);
            }

            // -- get/put float --

            public static float GetFloatL(AbstractIoBuffer bb, int bi)
            {
                return Int32BitsToSingle(GetIntL(bb, bi));
            }

            public static float GetFloatB(AbstractIoBuffer bb, int bi)
            {
                return Int32BitsToSingle(GetIntB(bb, bi));
            }

            public static float GetFloat(AbstractIoBuffer bb, int bi, Boolean bigEndian)
            {
                return (bigEndian ? GetFloatB(bb, bi) : GetFloatL(bb, bi));
            }

            public static void PutFloatL(AbstractIoBuffer bb, int bi, float x)
            {
                PutIntL(bb, bi, SingleToInt32Bits(x));
            }

            public static void PutFloatB(AbstractIoBuffer bb, int bi, float x)
            {
                PutIntB(bb, bi, SingleToInt32Bits(x));
            }

            public static void PutFloat(AbstractIoBuffer bb, int bi, float x, Boolean bigEndian)
            {
                if (bigEndian)
                    PutFloatB(bb, bi, x);
                else
                    PutFloatL(bb, bi, x);
            }

            // -- get/put double --

            public static double GetDoubleL(AbstractIoBuffer bb, int bi)
            {
                return BitConverter.Int64BitsToDouble(GetLongL(bb, bi));
            }

            public static double GetDoubleB(AbstractIoBuffer bb, int bi)
            {
                return BitConverter.Int64BitsToDouble(GetLongB(bb, bi));
            }

            public static double GetDouble(AbstractIoBuffer bb, int bi, Boolean bigEndian)
            {
                return (bigEndian ? GetDoubleB(bb, bi) : GetDoubleL(bb, bi));
            }

            public static void PutDoubleL(AbstractIoBuffer bb, int bi, double x)
            {
                PutLongL(bb, bi, BitConverter.DoubleToInt64Bits(x));
            }

            public static void PutDoubleB(AbstractIoBuffer bb, int bi, double x)
            {
                PutLongB(bb, bi, BitConverter.DoubleToInt64Bits(x));
            }

            public static void PutDouble(AbstractIoBuffer bb, int bi, double x, Boolean bigEndian)
            {
                if (bigEndian)
                    PutDoubleB(bb, bi, x);
                else
                    PutDoubleL(bb, bi, x);
            }

            private static int SingleToInt32Bits(float f)
            {
                byte[] bytes = BitConverter.GetBytes(f);
                return BitConverter.ToInt32(bytes, 0);
            }

            private static float Int32BitsToSingle(int i)
            {
                byte[] bytes = BitConverter.GetBytes(i);
                return BitConverter.ToSingle(bytes, 0);
            }
        }

        #endregion
    }
}
