using Mina.Core.Buffer;

namespace Mina.Filter.Codec.StateMachine
{
    /// <summary>
    /// Represents a state in a decoder state machine used by <see cref="DecodingStateMachine"/>.
    /// </summary>
    public interface IDecodingState
    {
        /// <summary>
        /// Invoked when data is available for this state.
        /// </summary>
        /// <param name="input">the data to be decoded</param>
        /// <param name="output">used to write decoded objects</param>
        /// <returns>
        /// the next state if a state transition was triggered (use 
        /// <code>this</code> for loop transitions) or <code>null</code> if 
        /// the state machine has reached its end.
        /// </returns>
        IDecodingState Decode(IoBuffer input, IProtocolDecoderOutput output);

        /// <summary>
        /// Invoked when the associated <see cref="Core.Session.IoSession"/> is closed.
        /// This method is 
        /// useful when you deal with protocols which don't specify the length of a 
        /// message (e.g. HTTP responses without <tt>content-length</tt> header). 
        /// Implement this method to process the remaining data that 
        /// <code>Decode(IoBuffer, IProtocolDecoderOutput)</code> method didn't process 
        /// completely.
        /// </summary>
        /// <param name="output">used to write decoded objects</param>
        /// <returns>
        /// the next state if a state transition was triggered (use 
        /// <code>this</code> for loop transitions) or <code>null</code> if 
        /// the state machine has reached its end.
        /// </returns>
        IDecodingState FinishDecode(IProtocolDecoderOutput output);
    }
}
