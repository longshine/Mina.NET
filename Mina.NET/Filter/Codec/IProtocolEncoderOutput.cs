using System;
using Mina.Core.Future;

namespace Mina.Filter.Codec
{
    /// <summary>
    /// Callback for <see cref="IProtocolEncoder"/> to generate encoded messages such as
    /// <see cref="IoBuffer"/>s.  <see cref="IProtocolEncoder"/> must call #write(Object)
    /// for each encoded message.
    /// </summary>
    public interface IProtocolEncoderOutput
    {
        /// <summary>
        /// Callback for <see cref="IProtocolEncoder"/> to generate encoded messages such as
        /// <see cref="IoBuffer"/>s.  <see cref="IProtocolEncoder"/> must call #write(Object)
        /// for each encoded message.
        /// </summary>
        void Write(Object encodedMessage);
        IWriteFuture Flush();
    }
}
