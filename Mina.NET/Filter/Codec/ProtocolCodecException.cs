using System;

namespace Mina.Filter.Codec
{
    /// <summary>
    /// An exception that is thrown when <see cref="ProtocolEncoder"/> or
    /// <see cref="IProtocolDecoder"/> cannot understand or failed to validate
    /// data to process.
    /// </summary>
    [Serializable]
    public class ProtocolCodecException : Exception
    {
        public ProtocolCodecException()
        { }

        public ProtocolCodecException(String message)
            : base(message)
        { }

        public ProtocolCodecException(String message, Exception innerException)
            : base(message, innerException)
        { }

        protected ProtocolCodecException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }

    /// <summary>
    /// An exception that is thrown when <see cref="IProtocolEncoder"/>
    /// cannot understand or failed to validate the specified message object.
    /// </summary>
    [Serializable]
    public class ProtocolEncoderException : ProtocolCodecException
    {
        public ProtocolEncoderException()
        { }

        public ProtocolEncoderException(String message)
            : base(message)
        { }

        public ProtocolEncoderException(String message, Exception innerException)
            : base(message, innerException)
        { }

        protected ProtocolEncoderException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }

    /// <summary>
    /// An exception that is thrown when <see cref="ProtocolDecoder"/>
    /// cannot understand or failed to validate the specified <see cref="IoBuffer"/>
    /// content.
    /// </summary>
    [Serializable]
    public class ProtocolDecoderException : ProtocolCodecException
    {
        private String _hexdump;

        public ProtocolDecoderException()
        { }

        public ProtocolDecoderException(String message)
            : base(message)
        { }

        public ProtocolDecoderException(String message, Exception innerException)
            : base(message, innerException)
        { }

        protected ProtocolDecoderException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }

        public String Hexdump
        {
            get { return _hexdump; }
            set
            {
                if (_hexdump != null)
                    throw new InvalidOperationException("Hexdump cannot be set more than once.");
                _hexdump = value;
            }
        }

        public override void GetObjectData(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }
}
