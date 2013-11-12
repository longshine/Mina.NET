using System;

namespace Mina.Core.Buffer
{
    /// <summary>
    /// A Byte buffer. 
    /// </summary>
    class ByteBuffer : IoBuffer
    {
        protected Byte[] _hb;
        protected Int32 _offset;
        protected Boolean _isReadOnly;

        public static IoBuffer Allocate(Int32 capacity)
        {
            return ByteBufferAllocator.Instance.Allocate(capacity);
        }

        /// <summary>
        /// Creates a new buffer with the given mark, position, limit, capacity,
        /// backing array, and array offset
        /// </summary>
        public ByteBuffer(IoBufferAllocator allocator, Int32 mark, Int32 pos, Int32 lim, Int32 cap, Byte[] hb, Int32 offset)
            : base(allocator, mark, pos, lim, cap)
        {
            this._hb = hb;
            this._offset = offset;
        }

        public ByteBuffer(IoBuffer parent, Int32 mark, Int32 pos, Int32 lim, Int32 cap, Byte[] hb, Int32 offset)
            : base(parent, mark, pos, lim, cap)
        {
            this._hb = hb;
            this._offset = offset;
        }

        public ByteBuffer(IoBufferAllocator allocator, Int32 cap, Int32 lim)
            : this(allocator, -1, 0, lim, cap, new Byte[cap], 0)
        { }

        public ByteBuffer(IoBufferAllocator allocator, Byte[] buf, Int32 off, Int32 len)
            : this(allocator, -1, off, off + len, buf.Length, buf, 0)
        { }

        public ByteBuffer(IoBuffer parent, Byte[] buf, Int32 mark, Int32 pos, Int32 lim, Int32 cap, Int32 off)
            : this(parent, mark, pos, lim, cap, buf, off)
        { }

        public override Int32 Capacity
        {
            get { return base.Capacity; }
            set
            {
                if (!_recapacityAllowed)
                    throw new InvalidOperationException("Derived buffers and their parent can't be expanded.");
                
                // Allocate a new buffer and transfer all settings to it.
                Int32 capacity = base.Capacity;
                if (value > capacity)
                { 
                    // Reallocate.
                    Byte[] newHb = new Byte[value];
                    Array.Copy(_hb, Offset(0), newHb, 0, capacity);

                    _hb = newHb;
                    _offset = 0;

                    Recapacity(value);
                }
            }
        }

        public override Byte Get()
        {
            return _hb[Offset(NextGetIndex())];
        }

        public override Byte Get(Int32 index)
        {
            return _hb[Offset(CheckIndex(index))];
        }

        public override IoBuffer Get(Byte[] dst, Int32 offset, Int32 length)
        {
            CheckBounds(offset, length, dst.Length);
            if (length > Remaining)
                throw new BufferUnderflowException();
            Array.Copy(_hb, Offset(Position), dst, offset, length);
            Position += length;
            return this;
        }

        public override ArraySegment<Byte> GetRemaining()
        {
            return new ArraySegment<Byte>(_hb, Offset(Position), Remaining);
        }

        public override Boolean IsReadOnly
        {
            get { return false; }
        }

        public override IoBuffer Compact()
        {
            Int32 remaining = Remaining;
            Int32 capacity = Capacity;

            if (capacity == 0)
                return this;

            if (AutoShrink && remaining <= (capacity >> 2) && capacity > MinimumCapacity)
            {
                Int32 newCapacity = capacity;
                Int32 minCapacity = Math.Max(MinimumCapacity, Remaining << 1);
                for (; ; )
                {
                    if ((newCapacity >> 1) < minCapacity)
                        break;
                    newCapacity >>= 1;
                }
                newCapacity = Math.Max(minCapacity, newCapacity);
                if (newCapacity == capacity)
                    return this;

                // Shrink and compact:
                // Sanity check.
                if (remaining > newCapacity)
                    throw new InvalidOperationException("The amount of the remaining bytes is greater than the new capacity.");

                // Reallocate.
                Byte[] newHb = new Byte[newCapacity];
                Array.Copy(_hb, Offset(Position), newHb, 0, remaining);

                _hb = newHb;
                _offset = 0;

                Recapacity(newCapacity);
            }
            else
            {
                Array.Copy(_hb, Offset(Position), _hb, Offset(0), Remaining);
            }

            Position = Remaining;
            Limit = Capacity;
            return this;
        }

        public override void Free()
        {
            // do nothing
        }

        protected override IoBuffer Slice0()
        {
            return new ByteBuffer(this, _hb, -1, 0, Remaining, Remaining, Position + _offset);
        }

        protected override IoBuffer Duplicate0()
        {
            return new ByteBuffer(this, _hb, MarkValue, Position, Limit, Capacity, _offset);
        }

        protected override IoBuffer AsReadOnlyBuffer0()
        {
            return new ByteBufferR(this, _hb, MarkValue, Position, Limit, Capacity, _offset);
        }

        protected override void PutInternal(Byte[] src, Int32 offset, Int32 length)
        {
            Array.Copy(src, offset, _hb, Offset(Position), length);
            Position += length;
        }

        protected override void PutInternal(IoBuffer src)
        {
            ByteBuffer bb = src as ByteBuffer;
            if (bb == null)
            {
                base.PutInternal(src);
            }
            else
            {
                Int32 n = bb.Remaining;
                if (n > Remaining)
                    throw new OverflowException();
                Array.Copy(bb._hb, bb.Offset(bb.Position), _hb, Offset(Position), n);
                bb.Position += n;
                Position += n;
            }
        }

        protected override Byte GetInternal(Int32 i)
        {
            return _hb[i];
        }

        protected override void PutInternal(Int32 i, Byte b)
        {
            _hb[i] = b;
        }

        protected override Int32 Offset(Int32 i)
        {
            return i + _offset;
        }
    }
}
