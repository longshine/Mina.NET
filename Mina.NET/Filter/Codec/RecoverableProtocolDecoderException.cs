using System;

namespace Mina.Filter.Codec
{
    /// <summary>
    /// A special exception that tells the <see cref="IProtocolDecoder"/> can keep
    /// decoding even after this exception is thrown.
    /// </summary>
    [Serializable]
    public class RecoverableProtocolDecoderException : ProtocolDecoderException
    {
        public RecoverableProtocolDecoderException() { }

        public RecoverableProtocolDecoderException(String message)
            : base(message) { }

        public RecoverableProtocolDecoderException(String message, Exception innerException)
            : base(message, innerException) { }

        protected RecoverableProtocolDecoderException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
