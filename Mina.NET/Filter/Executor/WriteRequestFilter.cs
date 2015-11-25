using System;
using Mina.Core.Filterchain;
using Mina.Core.Future;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Filter.Executor
{
    /// <summary>
    /// Attaches an <see cref="IoEventQueueHandler"/> to an <see cref="IoSession"/>'s
    /// <see cref="IWriteRequest"/> queue to provide accurate write queue status tracking.
    /// </summary>
    public class WriteRequestFilter : IoFilterAdapter
    {
        private readonly IoEventQueueHandler _queueHandler;

        /// <summary>
        /// Instantiates with an <see cref="IoEventQueueThrottle"/>.
        /// </summary>
        public WriteRequestFilter()
            : this(new IoEventQueueThrottle())
        { }

        /// <summary>
        /// Instantiates with the given <see cref="IoEventQueueHandler"/>.
        /// </summary>
        /// <param name="queueHandler">the handler</param>
        public WriteRequestFilter(IoEventQueueHandler queueHandler)
        {
            if (queueHandler == null)
                throw new ArgumentNullException("queueHandler");
            _queueHandler = queueHandler;
        }

        /// <summary>
        /// Gets the <see cref="IoEventQueueHandler"/>.
        /// </summary>
        public IoEventQueueHandler QueueHandler
        {
            get { return _queueHandler; }
        }

        /// <inheritdoc/>
        public override void FilterWrite(INextFilter nextFilter, IoSession session, IWriteRequest writeRequest)
        {
            IoEvent ioe = new IoEvent(IoEventType.Write, session, writeRequest);
            if (_queueHandler.Accept(this, ioe))
            {
                nextFilter.FilterWrite(session, writeRequest);
                IWriteFuture writeFuture = writeRequest.Future;
                if (writeFuture == null)
                    return;

                // We can track the write request only when it has a future.
                _queueHandler.Offered(this, ioe);
                writeFuture.Complete += (s, e) => _queueHandler.Polled(this, ioe);
            }
        }
    }
}
