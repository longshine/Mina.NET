using System;
using Mina.Core.Filterchain;
using Mina.Core.Session;

namespace Mina.Filter.Codec
{
    /// <summary>
    /// Callback for <see cref="IProtocolDecoder"/> to generate decoded messages.
    /// <see cref="IProtocolDecoder"/> must call write(Object) for each decoded
    /// messages.
    /// </summary>
    public interface IProtocolDecoderOutput
    {
        /// <summary>
        /// Callback for <see cref="IProtocolDecoder"/> to generate decoded messages.
        /// <see cref="IProtocolDecoder"/> must call write(Object) for each decoded
        /// messages.
        /// </summary>
        void Write(Object message);
        /// <summary>
        /// Flushes all messages you wrote via write(Object) to
        /// the next filter.
        /// </summary>
        void Flush(INextFilter nextFilter, IoSession session);
    }
}
