using System;

namespace Mina.Core.Buffer
{
    /// <summary>
    /// A read-only ByteBuffer. 
    /// </summary>
    class ByteBufferR : ByteBuffer
    {
        ByteBufferR(IoBufferAllocator allocator, Int32 cap, Int32 lim)
            : base(allocator, cap, lim)
        {
            _readOnly = true;
        }

        ByteBufferR(IoBufferAllocator allocator, Byte[] buf, Int32 off, Int32 len)
            : base(allocator, buf, off, len)
        {
            _readOnly = true;
        }

        public ByteBufferR(ByteBuffer parent, Byte[] buf, Int32 mark, Int32 pos, Int32 lim, Int32 cap, Int32 off)
            : base(parent, buf, mark, pos, lim, cap, off)
        {
            _readOnly = true;
        }

        public override Boolean ReadOnly
        {
            get { return true; }
        }

        public override IoBuffer Fill(Byte value, Int32 size)
        {
            throw new NotSupportedException();
        }

        public override IoBuffer Fill(Int32 size)
        {
            throw new NotSupportedException();
        }

        public override IoBuffer Compact()
        {
            throw new NotSupportedException();
        }

        protected override IoBuffer Slice0()
        {
            return new ByteBufferR(this, _hb, -1, 0, Remaining, Remaining, Position + _offset);
        }

        protected override IoBuffer Duplicate0()
        {
            return new ByteBufferR(this, _hb, MarkValue, Position, Limit, Capacity, _offset);
        }

        protected override IoBuffer AsReadOnlyBuffer0()
        {
            return Duplicate();
        }

        protected override void PutInternal(Int32 i, Byte b)
        {
            throw new NotSupportedException();
        }

        protected override void PutInternal(Byte[] src, Int32 offset, Int32 length)
        {
            throw new NotSupportedException();
        }

        protected override void PutInternal(IoBuffer src)
        {
            throw new NotSupportedException();
        }
    }
}
