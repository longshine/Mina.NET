using System;
using Mina.Util;
using Mina.Core.Future;

namespace Mina.Filter.Codec
{
    /// <summary>
    /// A <see cref="IProtocolEncoderOutput"/> based on queue.
    /// </summary>
    public abstract class AbstractProtocolEncoderOutput : IProtocolEncoderOutput
    {
        private readonly IQueue<Object> _queue = new ConcurrentQueue<Object>();

        public IQueue<Object> MessageQueue
        {
            get { return _queue; }
        }

        public void Write(Object encodedMessage)
        {
            _queue.Enqueue(encodedMessage);
        }

        public abstract IWriteFuture Flush();
    }
}
