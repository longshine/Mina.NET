using System;
using Mina.Core.Filterchain;
using Mina.Core.Session;
using Mina.Util;

namespace Mina.Filter.Codec
{
    /// <summary>
    /// A <see cref="IProtocolDecoderOutput"/> based on queue.
    /// </summary>
    public abstract class AbstractProtocolDecoderOutput : IProtocolDecoderOutput
    {
        private readonly IQueue<Object> _queue = new ConcurrentQueue<Object>();

        public IQueue<Object> MessageQueue
        {
            get { return _queue; }
        }

        public void Write(Object message)
        {
            if (message == null)
                throw new ArgumentNullException("message");
            _queue.Enqueue(message);
        }

        public abstract void Flush(INextFilter nextFilter, IoSession session);
    }
}
