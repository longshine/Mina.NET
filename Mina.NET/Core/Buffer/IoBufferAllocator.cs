using System;

namespace Mina.Core.Buffer
{
    public interface IoBufferAllocator : IDisposable
    {
        IoBuffer Allocate(Int32 capacity);
        IoBuffer Wrap(Byte[] array);
        IoBuffer Wrap(Byte[] array, Int32 offset, Int32 length);
    }
}
