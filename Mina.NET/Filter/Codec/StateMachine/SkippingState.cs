using System;
using Mina.Core.Buffer;

namespace Mina.Filter.Codec.StateMachine
{
    /// <summary>
    /// <see cref="IDecodingState"/> which skips data until <code>CanSkip(byte)</code> returns false.
    /// </summary>
    public abstract class SkippingState : IDecodingState
    {
        private Int32 _skippedBytes;

        public IDecodingState Decode(IoBuffer input, IProtocolDecoderOutput output)
        {
            Int32 beginPos = input.Position;
            Int32 limit = input.Limit;
            for (Int32 i = beginPos; i < limit; i++)
            {
                Byte b = input.Get(i);
                if (!CanSkip(b))
                {
                    input.Position = i;
                    Int32 answer = _skippedBytes;
                    _skippedBytes = 0;
                    return FinishDecode(answer);
                }

                _skippedBytes++;
            }

            input.Position = limit;
            return this;
        }

        public IDecodingState FinishDecode(IProtocolDecoderOutput output)
        {
            return FinishDecode(_skippedBytes);
        }

        /// <summary>
        /// Called to determine whether the specified byte can be skipped.
        /// </summary>
        /// <param name="b">the byte to check</param>
        /// <returns><code>true</code> if the byte can be skipped</returns>
        protected abstract Boolean CanSkip(Byte b);

        /// <summary>
        /// Invoked when this state cannot skip any more bytes.
        /// </summary>
        /// <param name="skippedBytes">the number of bytes skipped</param>
        /// <returns>the next state if a state transition was triggered (use 
        /// <code>this</code> for loop transitions) or <code>null</code> if 
        /// the state machine has reached its end.</returns>
        protected abstract IDecodingState FinishDecode(Int32 skippedBytes);
    }
}
