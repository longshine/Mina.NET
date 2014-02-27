using System;
using Mina.Core.Buffer;

namespace Mina.Filter.Codec.StateMachine
{
    /// <summary>
    /// <see cref="IDecodingState"/> which consumes all received bytes until the session is closed.
    /// </summary>
    public abstract class ConsumeToEndOfSessionDecodingState : IDecodingState
    {
        private readonly Int32 _maxLength;
        private IoBuffer _buffer;

        /// <summary>
        /// Creates a new instance using the specified maximum length.
        /// </summary>
        /// <param name="maxLength">the maximum number of bytes which will be consumed</param>
        /// <remarks>
        /// If the max is reached a <see cref="ProtocolDecoderException"/> will be 
        /// thrown by <code>Decode(IoBuffer, IProtocolDecoderOutput)</code>
        /// </remarks>
        protected ConsumeToEndOfSessionDecodingState(Int32 maxLength)
        {
            _maxLength = maxLength;
        }

        public IDecodingState Decode(IoBuffer input, IProtocolDecoderOutput output)
        {
            if (_buffer == null)
            {
                _buffer = IoBuffer.Allocate(256);
                _buffer.AutoExpand = true;
            }

            if (_buffer.Position + input.Remaining > _maxLength)
                throw new ProtocolDecoderException("Received data exceeds " + _maxLength + " byte(s).");
         
            _buffer.Put(input);
            return this;
        }

        public IDecodingState FinishDecode(IProtocolDecoderOutput output)
        {
            try
            {
                if (_buffer == null)
                {
                    _buffer = IoBuffer.Allocate(0);
                }
                _buffer.Flip();
                return FinishDecode(_buffer, output);
            }
            finally
            {
                _buffer = null;
            }
        }

        /// <summary>
        /// Invoked when this state has consumed all bytes until the session is closed.
        /// </summary>
        /// <param name="product">the bytes read</param>
        /// <param name="output">the current <see cref="IProtocolDecoderOutput"/> used to write decoded messages.</param>
        /// <returns>
        /// the next state if a state transition was triggered (use 
        /// <code>this</code> for loop transitions) or <code>null</code> if 
        /// the state machine has reached its end.
        /// </returns>
        protected abstract IDecodingState FinishDecode(IoBuffer product, IProtocolDecoderOutput output);
    }
}
