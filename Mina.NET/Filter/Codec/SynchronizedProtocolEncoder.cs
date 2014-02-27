using System;
using Mina.Core.Session;

namespace Mina.Filter.Codec
{
    /// <summary>
    /// A <see cref="IProtocolEncoder"/> implementation which decorates an existing encoder
    /// to be thread-safe.  Please be careful if you're going to use this decorator
    /// because it can be a root of performance degradation in a multi-thread
    /// environment.  Please use this decorator only when you need to synchronize
    /// on a per-encoder basis instead of on a per-session basis, which is not
    /// common.
    /// </summary>
    public class SynchronizedProtocolEncoder : IProtocolEncoder
    {
        private readonly IProtocolEncoder _encoder;

        public SynchronizedProtocolEncoder(IProtocolEncoder encoder)
        {
            if (encoder == null)
                throw new ArgumentNullException("encoder");
            _encoder = encoder;
        } 

        public void Encode(IoSession session, Object message, IProtocolEncoderOutput output)
        {
            lock (_encoder)
            {
                _encoder.Encode(session, message, output);
            }
        }

        public void Dispose(IoSession session)
        {
            lock (_encoder)
            {
                _encoder.Dispose(session);
            }
        }
    }
}
