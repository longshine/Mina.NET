using System;
using Mina.Core.Session;

namespace Mina.Filter.Codec
{
    /// <summary>
    /// Encodes higher-level message objects into binary or protocol-specific data.
    /// </summary>
    public interface IProtocolEncoder
    {
        /// <summary>
        /// Encodes higher-level message objects into binary or protocol-specific data.
        /// </summary>
        void Encode(IoSession session, Object message, IProtocolEncoderOutput output);
        /// <summary>
        /// Releases all resources related with this encoder.
        /// </summary>
        void Dispose(IoSession session);
    }
}
