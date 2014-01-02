using System;
using System.Text;

namespace Mina.Core.Buffer
{
    /// <summary>
    /// A <see cref="IoBuffer"/> that wraps a buffer and proxies any operations to it.
    /// </summary>
    public class IoBufferWrapper : IoBuffer
    {
        private readonly IoBuffer _buf;

        protected IoBufferWrapper(IoBuffer buf)
            : base(-1, 0, 0, 0)
        {
            if (buf == null)
                throw new ArgumentNullException("buf");
            _buf = buf;
        }

        public override IoBufferAllocator BufferAllocator
        {
            get { return _buf.BufferAllocator; }
        }

        public override ByteOrder Order
        {
            get { return _buf.Order; }
            set { _buf.Order = value; }
        }

        public override Int32 Capacity
        {
            get { return _buf.Capacity; }
            set { _buf.Capacity = value; }
        }

        public override Int32 Position
        {
            get { return _buf.Position; }
            set { _buf.Position = value; }
        }

        public override Int32 Limit
        {
            get { return _buf.Limit; }
            set { _buf.Limit = value; }
        }

        public override Int32 Remaining
        {
            get { return _buf.Remaining; }
        }

        public override Boolean HasRemaining
        {
            get { return _buf.HasRemaining; }
        }

        public override Boolean AutoExpand
        {
            get { return _buf.AutoExpand; }
            set { _buf.AutoExpand = value; }
        }

        public override Boolean AutoShrink
        {
            get { return _buf.AutoShrink; }
            set { _buf.AutoShrink = value; }
        }

        public override Boolean Derived
        {
            get { return _buf.Derived; }
        }

        public override Int32 MinimumCapacity
        {
            get { return _buf.MinimumCapacity; }
            set { _buf.MinimumCapacity = value; }
        }

        public override Boolean HasArray
        {
            get { return _buf.HasArray; }
        }

        public override IoBuffer Expand(Int32 expectedRemaining)
        {
            _buf.Expand(expectedRemaining);
            return this;
        }

        public override IoBuffer Expand(Int32 pos, Int32 expectedRemaining)
        {
            _buf.Expand(pos, expectedRemaining);
            return this;
        }

        public override IoBuffer Sweep()
        {
            _buf.Sweep();
            return this;
        }

        public override IoBuffer Sweep(Byte value)
        {
            _buf.Sweep(value);
            return this;
        }

        public override IoBuffer FillAndReset(Int32 size)
        {
            _buf.FillAndReset(size);
            return this;
        }

        public override IoBuffer FillAndReset(Byte value, Int32 size)
        {
            _buf.FillAndReset(value, size);
            return this;
        }

        public override IoBuffer Fill(Int32 size)
        {
            _buf.Fill(size);
            return this;
        }

        public override IoBuffer Fill(Byte value, Int32 size)
        {
            _buf.Fill(value, size);
            return this;
        }

        public override String GetHexDump()
        {
            return _buf.GetHexDump();
        }

        public override String GetHexDump(Int32 lengthLimit)
        {
            return _buf.GetHexDump(lengthLimit);
        }

        public override Boolean PrefixedDataAvailable(Int32 prefixLength)
        {
            return _buf.PrefixedDataAvailable(prefixLength);
        }

        public override Boolean PrefixedDataAvailable(Int32 prefixLength, Int32 maxDataLength)
        {
            return _buf.PrefixedDataAvailable(prefixLength, maxDataLength);
        }

        public override Int32 IndexOf(Byte b)
        {
            return _buf.IndexOf(b);
        }

        public override String GetPrefixedString(Encoding encoding)
        {
            return _buf.GetPrefixedString(encoding);
        }

        public override String GetPrefixedString(Int32 prefixLength, Encoding encoding)
        {
            return _buf.GetPrefixedString(prefixLength, encoding);
        }

        public override IoBuffer PutPrefixedString(String value, Encoding encoding)
        {
            return _buf.PutPrefixedString(value, encoding);
        }

        public override IoBuffer PutPrefixedString(String value, Int32 prefixLength, Encoding encoding)
        {
            _buf.PutPrefixedString(value, prefixLength, encoding);
            return this;
        }

        public override Byte Get()
        {
            return _buf.Get();
        }

        public override Byte Get(Int32 index)
        {
            return _buf.Get(index);
        }

        public override IoBuffer Get(Byte[] dst, Int32 offset, Int32 length)
        {
            _buf.Get(dst, offset, length);
            return this;
        }

        public override ArraySegment<Byte> GetRemaining()
        {
            return _buf.GetRemaining();
        }

        public override void Free()
        {
            _buf.Free();
        }

        public override IoBuffer Slice()
        {
            return _buf.Slice();
        }

        public override IoBuffer Duplicate()
        {
            return _buf.Duplicate();
        }

        public override IoBuffer AsReadOnlyBuffer()
        {
            return _buf.AsReadOnlyBuffer();
        }

        public override IoBuffer Skip(Int32 size)
        {
            _buf.Skip(size);
            return this;
        }

        public override IoBuffer Put(Byte b)
        {
            _buf.Put(b);
            return this;
        }

        public override IoBuffer Put(Int32 i, Byte b)
        {
            _buf.Put(i, b);
            return this;
        }

        public override IoBuffer Put(Byte[] src, Int32 offset, Int32 length)
        {
            _buf.Put(src, offset, length);
            return this;
        }

        public override IoBuffer Put(IoBuffer src)
        {
            _buf.Put(src);
            return this;
        }

        public override IoBuffer Compact()
        {
            _buf.Compact();
            return this;
        }

        public override IoBuffer Put(Byte[] src)
        {
            _buf.Put(src);
            return this;
        }

        public override IoBuffer PutString(String s)
        {
            _buf.PutString(s);
            return this;
        }

        public override IoBuffer PutString(String s, Encoding encoding)
        {
            _buf.PutString(s, encoding);
            return this;
        }

        public override Char GetChar()
        {
            return _buf.GetChar();
        }

        public override Char GetChar(Int32 index)
        {
            return _buf.GetChar(index);
        }

        public override IoBuffer PutChar(Char value)
        {
            _buf.PutChar(value);
            return this;
        }

        public override IoBuffer PutChar(Int32 index, Char value)
        {
            _buf.PutChar(index, value);
            return this;
        }

        public override Int16 GetInt16()
        {
            return _buf.GetInt16();
        }

        public override Int16 GetInt16(Int32 index)
        {
            return _buf.GetInt16(index);
        }

        public override IoBuffer PutInt16(Int16 value)
        {
            _buf.PutInt16(value);
            return this;
        }

        public override IoBuffer PutInt16(Int32 index, Int16 value)
        {
            _buf.PutInt16(index, value);
            return this;
        }

        public override Int32 GetInt32()
        {
            return _buf.GetInt32();
        }

        public override Int32 GetInt32(Int32 index)
        {
            return _buf.GetInt32(index);
        }

        public override IoBuffer PutInt32(Int32 value)
        {
            _buf.PutInt32(value);
            return this;
        }

        public override IoBuffer PutInt32(Int32 index, Int32 value)
        {
            _buf.PutInt32(index, value);
            return this;
        }

        public override Int64 GetInt64()
        {
            return _buf.GetInt64();
        }

        public override Int64 GetInt64(Int32 index)
        {
            return _buf.GetInt64(index);
        }

        public override IoBuffer PutInt64(Int64 value)
        {
            _buf.PutInt64(value);
            return this;
        }

        public override IoBuffer PutInt64(Int32 index, Int64 value)
        {
            _buf.PutInt64(index, value);
            return this;
        }

        public override Single GetSingle()
        {
            return _buf.GetSingle();
        }

        public override Single GetSingle(Int32 index)
        {
            return _buf.GetSingle(index);
        }

        public override IoBuffer PutSingle(Single value)
        {
            _buf.PutSingle(value);
            return this;
        }

        public override IoBuffer PutSingle(Int32 index, Single value)
        {
            _buf.PutSingle(index, value);
            return this;
        }

        public override Double GetDouble()
        {
            return _buf.GetDouble();
        }

        public override Double GetDouble(Int32 index)
        {
            return _buf.GetDouble(index);
        }

        public override IoBuffer PutDouble(Double value)
        {
            _buf.PutDouble(value);
            return this;
        }

        public override IoBuffer PutDouble(Int32 index, Double value)
        {
            _buf.PutDouble(index, value);
            return this;
        }

        public override Boolean ReadOnly
        {
            get { return _buf.ReadOnly; }
        }
    }
}
