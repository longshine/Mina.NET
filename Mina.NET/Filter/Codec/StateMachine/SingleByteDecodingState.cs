using System;
using Mina.Core.Buffer;

namespace Mina.Filter.Codec.StateMachine
{
    /// <summary>
    /// <see cref="IDecodingState"/> which decodes <code>byte</code> values.
    /// </summary>
    public abstract class SingleByteDecodingState : IDecodingState
    {
        public IDecodingState Decode(IoBuffer input, IProtocolDecoderOutput output)
        {
            if (input.HasRemaining)
                return FinishDecode(input.Get(), output);

            return this;
        }

        public IDecodingState FinishDecode(IProtocolDecoderOutput output)
        {
            throw new ProtocolDecoderException("Unexpected end of session while waiting for a single byte.");
        }

        /// <summary>
        /// Invoked when this state has consumed a complete <code>byte</code>.
        /// </summary>
        /// <param name="b">the byte</param>
        /// <param name="output">the current <see cref="IProtocolDecoderOutput"/> used to write decoded messages.</param>
        /// <returns>
        /// the next state if a state transition was triggered (use 
        /// <code>this</code> for loop transitions) or <code>null</code> if 
        /// the state machine has reached its end.
        /// </returns>
        protected abstract IDecodingState FinishDecode(Byte b, IProtocolDecoderOutput output);
    }
}
