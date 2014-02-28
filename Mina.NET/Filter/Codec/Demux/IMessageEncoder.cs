using System;
using Mina.Core.Session;

namespace Mina.Filter.Codec.Demux
{
    /// <summary>
    /// Encodes a certain type of messages.
    /// </summary>
    public interface IMessageEncoder
    {
        /// <summary>
        /// Encodes higher-level message objects into binary or protocol-specific data.
        /// </summary>
        /// <remarks>
        /// MINA invokes <code>Encode(IoSession, Object, ProtocolEncoderOutput)</code>
        /// method with message which is popped from the session write queue, and then
        /// the encoder implementation puts encoded <see cref="Core.Buffer.IoBuffer"/>s into
        /// <see cref="IProtocolEncoderOutput"/>.
        /// </remarks>
        void Encode(IoSession session, Object message, IProtocolEncoderOutput output);
    }

    /// <summary>
    /// Encodes a certain type of messages.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IMessageEncoder<T> : IMessageEncoder
    {
        /// <summary>
        /// Encodes higher-level message objects into binary or protocol-specific data.
        /// </summary>
        /// <remarks>
        /// MINA invokes <code>Encode(IoSession, Object, ProtocolEncoderOutput)</code>
        /// method with message which is popped from the session write queue, and then
        /// the encoder implementation puts encoded <see cref="Core.Buffer.IoBuffer"/>s into
        /// <see cref="IProtocolEncoderOutput"/>.
        /// </remarks>
        void Encode(IoSession session, T message, IProtocolEncoderOutput output);
    }
}
