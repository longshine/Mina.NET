using System;
using Mina.Core.Buffer;

namespace Mina.Transport.Socket
{
    public class SocketAsyncEventArgsBufferAllocator : IoBufferAllocator
    {
        public static readonly SocketAsyncEventArgsBufferAllocator Instance = new SocketAsyncEventArgsBufferAllocator();

        public IoBuffer Allocate(Int32 capacity)
        {
            if (capacity < 0)
                throw new ArgumentException();
            return new SocketAsyncEventArgsBuffer(this, capacity, capacity);
        }

        public IoBuffer Wrap(Byte[] array)
        {
            return Wrap(array, 0, array.Length);
        }

        public IoBuffer Wrap(Byte[] array, Int32 offset, Int32 length)
        {
            try
            {
                return new SocketAsyncEventArgsBuffer(this, array, offset, length);
            }
            catch (ArgumentException)
            {
                throw new IndexOutOfRangeException();
            }
        }
    }
}
