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

        public WriteRequestFilter()
            : this(new IoEventQueueThrottle())
        { }

        public WriteRequestFilter(IoEventQueueHandler queueHandler)
        {
            if (queueHandler == null)
                throw new ArgumentNullException("queueHandler");
            _queueHandler = queueHandler;
        }

        public IoEventQueueHandler QueueHandler
        {
            get { return _queueHandler; }
        }

        public override void FilterWrite(INextFilter nextFilter, IoSession session, IWriteRequest writeRequest)
        {
            IoEvent e = new IoEvent(IoEventType.Write, session, writeRequest);
            if (_queueHandler.Accept(this, e))
            {
                nextFilter.FilterWrite(session, writeRequest);
                IWriteFuture writeFuture = writeRequest.Future;
                if (writeFuture == null)
                    return;

                // We can track the write request only when it has a future.
                _queueHandler.Offered(this, e);
                writeFuture.Complete += future => _queueHandler.Polled(this, e);
            }
        }
    }
}
