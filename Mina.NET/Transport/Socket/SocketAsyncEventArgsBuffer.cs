using System;
using System.Net.Sockets;
using Mina.Core.Buffer;

namespace Mina.Transport.Socket
{
    public class SocketAsyncEventArgsBuffer : IoBuffer
    {
        private readonly SocketAsyncEventArgs _socketAsyncEventArgs;

        public SocketAsyncEventArgsBuffer(SocketAsyncEventArgs socketAsyncEventArgs)
            : base((IoBufferAllocator)null, -1,0, socketAsyncEventArgs.Count, socketAsyncEventArgs.Count)
        {
            _socketAsyncEventArgs = socketAsyncEventArgs;
        }

        public SocketAsyncEventArgsBuffer(IoBufferAllocator allocator, Int32 cap, Int32 lim)
            : this(allocator, new Byte[cap], 0, lim)
        { }

        public SocketAsyncEventArgsBuffer(IoBufferAllocator allocator, Byte[] buffer, Int32 offset, Int32 count)
            : base(allocator, -1, 0, count, buffer.Length)
        {
            _socketAsyncEventArgs = new SocketAsyncEventArgs();
            _socketAsyncEventArgs.SetBuffer(buffer, offset, count);
        }

        public SocketAsyncEventArgs SocketAsyncEventArgs
        {
            get { return _socketAsyncEventArgs; }
        }

        public override Boolean IsReadOnly
        {
            get { return false; }
        }

        public override Int32 Capacity
        {
            get { return _socketAsyncEventArgs.Count; }
            set
            {
                base.Capacity = value;
            }
        }

        public override Byte Get()
        {
            return _socketAsyncEventArgs.Buffer[Offset(NextGetIndex())];
        }

        public override IoBuffer Get(Byte[] dst, Int32 offset, Int32 length)
        {
            CheckBounds(offset, length, dst.Length);
            if (length > Remaining)
                throw new BufferUnderflowException();
            Array.Copy(_socketAsyncEventArgs.Buffer, Offset(Position), dst, offset, length);
            Position += length;
            return this;
        }

        public override Byte Get(Int32 index)
        {
            return _socketAsyncEventArgs.Buffer[Offset(CheckIndex(index))];
        }

        public override ArraySegment<Byte> GetRemaining()
        {
            return new ArraySegment<Byte>(_socketAsyncEventArgs.Buffer, _socketAsyncEventArgs.Offset, Limit);
        }

        protected override Int32 Offset(Int32 pos)
        {
            return _socketAsyncEventArgs.Offset + pos;
        }

        protected override Byte GetInternal(Int32 i)
        {
            throw new NotImplementedException();
        }

        protected override void PutInternal(Int32 i, Byte b)
        {
            throw new NotImplementedException();
        }

        public override IoBuffer Compact()
        {
            throw new NotImplementedException();
        }

        public override void Free()
        {
            // TODO free buffer?
        }

        protected override IoBuffer Slice0()
        {
            throw new NotImplementedException();
        }

        protected override IoBuffer AsReadOnlyBuffer0()
        {
            throw new NotImplementedException();
        }

        protected override IoBuffer Duplicate0()
        {
            throw new NotImplementedException();
        }
    }
}
