using System;

namespace Mina.Filter.Codec
{
    /// <summary>
    /// An exception that is thrown when <see cref="IProtocolEncoder"/> or
    /// <see cref="IProtocolDecoder"/> cannot understand or failed to validate
    /// data to process.
    /// </summary>
    [Serializable]
    public class ProtocolCodecException : Exception
    {
        /// <summary>
        /// </summary>
        public ProtocolCodecException()
        { }

        /// <summary>
        /// </summary>
        public ProtocolCodecException(String message)
            : base(message)
        { }

        /// <summary>
        /// </summary>
        public ProtocolCodecException(String message, Exception innerException)
            : base(message, innerException)
        { }

        /// <summary>
        /// </summary>
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
        /// <summary>
        /// </summary>
        public ProtocolEncoderException()
        { }

        /// <summary>
        /// </summary>
        public ProtocolEncoderException(String message)
            : base(message)
        { }

        /// <summary>
        /// </summary>
        public ProtocolEncoderException(String message, Exception innerException)
            : base(message, innerException)
        { }

        /// <summary>
        /// </summary>
        protected ProtocolEncoderException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }

    /// <summary>
    /// An exception that is thrown when <see cref="IProtocolDecoder"/>
    /// cannot understand or failed to validate the specified <see cref="Core.Buffer.IoBuffer"/>
    /// content.
    /// </summary>
    [Serializable]
    public class ProtocolDecoderException : ProtocolCodecException
    {
        private String _hexdump;

        /// <summary>
        /// </summary>
        public ProtocolDecoderException()
        { }

        /// <summary>
        /// </summary>
        public ProtocolDecoderException(String message)
            : base(message)
        { }

        /// <summary>
        /// </summary>
        public ProtocolDecoderException(String message, Exception innerException)
            : base(message, innerException)
        { }

        /// <summary>
        /// </summary>
        protected ProtocolDecoderException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }

        /// <summary>
        /// Gets the current data in hex.
        /// </summary>
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

        /// <summary>
        /// </summary>
        public override void GetObjectData(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
        {
            base.GetObjectData(info, context);
        }
    }
}
