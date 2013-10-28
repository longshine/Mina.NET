using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mina.Filter.Codec
{
    /// <summary>
    /// An exception that is thrown when <see cref="ProtocolEncoder"/> or
    /// <see cref="IProtocolDecoder"/> cannot understand or failed to validate
    /// data to process.
    /// </summary>
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
    }

    /// <summary>
    /// An exception that is thrown when <see cref="IProtocolEncoder"/>
    /// cannot understand or failed to validate the specified message object.
    /// </summary>
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
    }

    /// <summary>
    /// An exception that is thrown when <see cref="ProtocolDecoder"/>
    /// cannot understand or failed to validate the specified <see cref="IoBuffer"/>
    /// content.
    /// </summary>
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
    }

    /// <summary>
    /// A special exception that tells the <see cref="IProtocolDecoder"/> can keep
    /// decoding even after this exception is thrown.
    /// </summary>
    public class RecoverableProtocolDecoderException : ProtocolDecoderException
    {
        public RecoverableProtocolDecoderException()
        { }

        public RecoverableProtocolDecoderException(String message)
            : base(message)
        { }

        public RecoverableProtocolDecoderException(String message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
