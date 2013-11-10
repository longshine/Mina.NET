using System;

namespace Mina.Core.Buffer
{
    /// <summary>
    /// An exception which is thrown when the data the <see cref="IoBuffer"/> contains is corrupt.
    /// </summary>
    public class BufferDataException : Exception
    {
        public BufferDataException() { }

        public BufferDataException(String message) : base(message) { }

        public BufferDataException(String message, Exception inner) : base(message, inner) { }
    }
}
