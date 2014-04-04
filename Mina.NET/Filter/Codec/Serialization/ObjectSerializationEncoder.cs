using System;
using Mina.Core.Buffer;
using Mina.Core.Session;

namespace Mina.Filter.Codec.Serialization
{
    /// <summary>
    /// A <see cref="IProtocolEncoder"/> which serializes <code>Serializable</code> objects,
    /// using <see cref="IoBuffer.PutObject(Object)"/>.
    /// </summary>
    public class ObjectSerializationEncoder : ProtocolEncoderAdapter
    {
        private Int32 _maxObjectSize = Int32.MaxValue; // 2GB

        /// <summary>
        /// Gets or sets the allowed maximum size of the encoded object.
        /// If the size of the encoded object exceeds this value, this encoder
        /// will throw a <see cref="ArgumentException"/>.  The default value
        /// is <see cref="Int32.MaxValue"/>.
        /// </summary>
        public Int32 MaxObjectSize
        {
            get { return _maxObjectSize; }
            set
            {
                if (value <= 0)
                    throw new ArgumentException("MaxObjectSize should be larger than zero.", "value");
                _maxObjectSize = value;
            }
        }

        /// <inheritdoc/>
        public override void Encode(IoSession session, Object message, IProtocolEncoderOutput output)
        {
            if (!message.GetType().IsSerializable)
                throw new System.Runtime.Serialization.SerializationException(message.GetType() + " is not serializable.");

            IoBuffer buf = IoBuffer.Allocate(64);
            buf.AutoExpand = true;
            buf.PutInt32(0);
            buf.PutObject(message);

            Int32 objectSize = buf.Position - 4;
            if (objectSize > _maxObjectSize)
                throw new ArgumentException(String.Format("The encoded object is too big: {0} (> {1})",
                    objectSize, _maxObjectSize), "message");

            buf.PutInt32(0, objectSize);
            buf.Flip();
            output.Write(buf);
        }
    }
}
