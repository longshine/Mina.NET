using System;
using System.Text;

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
        protected Boolean _recapacityAllowed = true;
        private Int32 _minimumCapacity;

        public AbstractIoBuffer(IoBufferAllocator allocator, Int32 mark, Int32 pos, Int32 lim, Int32 cap)
            : base(mark, pos, lim, cap)
        {
            _allocator = allocator;
            _recapacityAllowed = true;
            _derived = false;
            _minimumCapacity = cap;
        }

        public AbstractIoBuffer(AbstractIoBuffer parent, Int32 mark, Int32 pos, Int32 lim, Int32 cap)
            : base(mark, pos, lim, cap)
        {
            _allocator = parent._allocator;
            _recapacityAllowed = false;
            _derived = true;
            _minimumCapacity = parent._minimumCapacity;
        }

        public override ByteOrder Order
        {
            get { return _order; }
            set { _order = value; }
        }

        public override Int32 MinimumCapacity
        {
            get { return _minimumCapacity; }
            set
            {
                if (value < 0)
                    throw new ArgumentException();
                _minimumCapacity = value;
            }
        }

        public override IoBufferAllocator BufferAllocator
        {
            get { return _allocator; }
        }

        public override Int32 Position
        {
            get { return base.Position; }
            set
            {
                base.Position = value;
                AutoExpand0(value, 0);
            }
        }

        public override Int32 Limit
        {
            get { return base.Limit; }
            set
            {
                base.Limit = value;
                AutoExpand0(value, 0);
            }
        }

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

        public override Boolean Derived
        {
            get { return _derived; }
        }

        public override IoBuffer Expand(Int32 expectedRemaining)
        {
            return Expand(Position, expectedRemaining, false);
        }

        public override IoBuffer Expand(Int32 pos, Int32 expectedRemaining)
        {
            return Expand(pos, expectedRemaining, false);
        }

        public override IoBuffer Sweep()
        {
            Clear();
            return FillAndReset(Remaining);
        }

        public override IoBuffer Sweep(Byte value)
        {
            Clear();
            return FillAndReset(value, Remaining);
        }

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

        public override String GetHexDump()
        {
            return GetHexDump(Int32.MaxValue);
        }

        public override String GetHexDump(Int32 lengthLimit)
        {
            return IoBufferHexDumper.GetHexdump(this, lengthLimit);
        }

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
        public override Boolean PrefixedDataAvailable(Int32 prefixLength)
        {
            return PrefixedDataAvailable(prefixLength, Int32.MaxValue);
        }

        /// <summary>
        /// Returns true if this buffer contains a data which has a data
        /// length as a prefix and the buffer has remaining data as enough as
        /// specified in the data length field.
        /// </summary>
        /// <param name="prefixLength">the length of the prefix field (1, 2, or 4)</param>
        /// <param name="maxDataLength">the allowed maximum of the read data length</param>
        /// <returns>true if data available</returns>
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

        public override String GetPrefixedString(Encoding encoding)
        {
            return GetPrefixedString(2, encoding);
        }

        /// <summary>
        /// Reads a string which has a length field before the actual encoded string.
        /// </summary>
        /// <param name="prefixLength">the length of the length field (1, 2, or 4)</param>
        /// <param name="encoding">the encoding of the string</param>
        /// <returns>the prefixed string</returns>
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

        public override IoBuffer PutPrefixedString(String value, Encoding encoding)
        {
            return PutPrefixedString(value, 2, encoding);
        }

        /// <summary>
        /// Writes the string into this buffer which has a prefixLength field
        /// before the actual encoded string.
        /// </summary>
        /// <param name="value">the string to write</param>
        /// <param name="prefixLength">the length of the length field (1, 2, or 4)</param>
        /// <param name="encoding">the encoding of the string</param>
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

        public override IoBuffer Slice()
        {
            _recapacityAllowed = false;
            return Slice0();
        }

        public override IoBuffer Duplicate()
        {
            _recapacityAllowed = false;
            return Duplicate0();
        }

        public override IoBuffer AsReadOnlyBuffer()
        {
            _recapacityAllowed = false;
            return AsReadOnlyBuffer0();
        }

        public override IoBuffer Skip(Int32 size)
        {
            AutoExpand0(size);
            Position = Position + size;
            return this;
        }

        public override IoBuffer Put(Byte b)
        {
            AutoExpand0(1);
            PutInternal(Offset(NextPutIndex()), b);
            return this;
        }

        public override IoBuffer Put(Int32 i, Byte b)
        {
            AutoExpand0(i, 1);
            PutInternal(Offset(CheckIndex(i)), b);
            return this;
        }

        public override IoBuffer Put(Byte[] src, Int32 offset, Int32 length)
        {
            CheckBounds(offset, length, src.Length);
            AutoExpand0(length);

            if (length > Remaining)
                throw new OverflowException();

            PutInternal(src, offset, length);
            return this;
        }

        public override IoBuffer Put(IoBuffer src)
        {
            if (Object.ReferenceEquals(src, this))
                throw new ArgumentException();

            AutoExpand0(src.Remaining);
            PutInternal(src);

            return this;
        }

        public override IoBuffer Put(Byte[] src)
        {
            return Put(src, 0, src.Length);
        }

        public override IoBuffer PutString(String s)
        {
            return PutString(s, Encoding.UTF8);
        }

        public override IoBuffer PutString(String s, Encoding encoding)
        {
            return Put(encoding.GetBytes(s));
        }

        protected virtual Int32 Offset(Int32 i)
        {
            return i;
        }

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

        protected virtual void PutInternal(Byte[] src, Int32 offset, Int32 length)
        {
            Int32 end = offset + length;
            for (Int32 i = offset; i < end; i++)
                Put(src[i]);
        }

        protected abstract Byte GetInternal(Int32 i);

        protected abstract void PutInternal(Int32 i, Byte b);

        protected abstract IoBuffer Slice0();
        protected abstract IoBuffer Duplicate0();
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

        public override Char GetChar()
        {
            return Bits.GetChar(this, Offset(NextGetIndex(2)), Order == ByteOrder.BigEndian);
        }

        public override Char GetChar(Int32 index)
        {
            return Bits.GetChar(this, Offset(CheckIndex(index, 2)), Order == ByteOrder.BigEndian);
        }

        public override IoBuffer PutChar(Char value)
        {
            AutoExpand0(2);
            Bits.PutChar(this, Offset(NextPutIndex(2)), value, Order == ByteOrder.BigEndian);
            return this;
        }

        public override IoBuffer PutChar(Int32 index, Char value)
        {
            AutoExpand0(index, 2);
            Bits.PutChar(this, Offset(CheckIndex(index, 2)), value, Order == ByteOrder.BigEndian);
            return this;
        }

        public override Int16 GetInt16()
        {
            return Bits.GetShort(this, Offset(NextGetIndex(2)), Order == ByteOrder.BigEndian);
        }

        public override Int16 GetInt16(Int32 index)
        {
            return Bits.GetShort(this, Offset(CheckIndex(index, 2)), Order == ByteOrder.BigEndian);
        }

        public override IoBuffer PutInt16(Int16 value)
        {
            AutoExpand0(2);
            Bits.PutShort(this, Offset(NextPutIndex(2)), value, Order == ByteOrder.BigEndian);
            return this;
        }

        public override IoBuffer PutInt16(Int32 index, Int16 value)
        {
            AutoExpand0(index, 2);
            Bits.PutShort(this, Offset(CheckIndex(index, 2)), value, Order == ByteOrder.BigEndian);
            return this;
        }

        public override Int32 GetInt32()
        {
            return Bits.GetInt(this, Offset(NextGetIndex(4)), Order == ByteOrder.BigEndian);
        }

        public override Int32 GetInt32(Int32 index)
        {
            return Bits.GetInt(this, Offset(CheckIndex(index, 4)), Order == ByteOrder.BigEndian);
        }

        public override IoBuffer PutInt32(Int32 value)
        {
            AutoExpand0(4);
            Bits.PutInt(this, Offset(NextPutIndex(4)), value, Order == ByteOrder.BigEndian);
            return this;
        }

        public override IoBuffer PutInt32(Int32 index, Int32 value)
        {
            AutoExpand0(index, 4);
            Bits.PutInt(this, Offset(CheckIndex(index, 4)), value, Order == ByteOrder.BigEndian);
            return this;
        }

        public override Int64 GetInt64()
        {
            return Bits.GetLong(this, Offset(NextGetIndex(8)), Order == ByteOrder.BigEndian);
        }

        public override Int64 GetInt64(Int32 index)
        {
            return Bits.GetLong(this, Offset(CheckIndex(index, 8)), Order == ByteOrder.BigEndian);
        }

        public override IoBuffer PutInt64(Int64 value)
        {
            AutoExpand0(8);
            Bits.PutLong(this, Offset(NextPutIndex(8)), value, Order == ByteOrder.BigEndian);
            return this;
        }

        public override IoBuffer PutInt64(Int32 index, Int64 value)
        {
            AutoExpand0(index, 8);
            Bits.PutLong(this, Offset(CheckIndex(index, 8)), value, Order == ByteOrder.BigEndian);
            return this;
        }

        public override Single GetSingle()
        {
            return Bits.GetFloat(this, Offset(NextGetIndex(4)), Order == ByteOrder.BigEndian);
        }

        public override Single GetSingle(Int32 index)
        {
            return Bits.GetFloat(this, Offset(CheckIndex(index, 4)), Order == ByteOrder.BigEndian);
        }

        public override IoBuffer PutSingle(Single value)
        {
            AutoExpand0(4);
            Bits.PutFloat(this, Offset(NextPutIndex(4)), value, Order == ByteOrder.BigEndian);
            return this;
        }

        public override IoBuffer PutSingle(Int32 index, Single value)
        {
            AutoExpand0(index, 4);
            Bits.PutFloat(this, Offset(CheckIndex(index, 4)), value, Order == ByteOrder.BigEndian);
            return this;
        }

        public override Double GetDouble()
        {
            return Bits.GetDouble(this, Offset(NextGetIndex(8)), Order == ByteOrder.BigEndian);
        }

        public override Double GetDouble(Int32 index)
        {
            return Bits.GetDouble(this, Offset(CheckIndex(index, 8)), Order == ByteOrder.BigEndian);
        }

        public override IoBuffer PutDouble(Double value)
        {
            AutoExpand0(8);
            Bits.PutDouble(this, Offset(NextPutIndex(8)), value, Order == ByteOrder.BigEndian);
            return this;
        }

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
