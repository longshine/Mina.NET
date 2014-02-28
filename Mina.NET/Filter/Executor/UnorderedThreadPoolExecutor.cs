using System;
using Mina.Core.Session;

namespace Mina.Filter.Executor
{
    /// <summary>
    /// A <see cref="IoEventExecutor"/> that does not maintain the order of <see cref="IoEvent"/>s.
    /// This means more than one event handler methods can be invoked at the same time with mixed order.
    /// If you need to maintain the order of events per session, please use
    /// <see cref="OrderedThreadPoolExecutor"/>.
    /// </summary>
    public class UnorderedThreadPoolExecutor : ThreadPoolExecutor, IoEventExecutor
    {
        private readonly IoEventQueueHandler _queueHandler;

        public UnorderedThreadPoolExecutor()
            : this(null)
        { }

        public UnorderedThreadPoolExecutor(IoEventQueueHandler queueHandler)
        {
            _queueHandler = queueHandler == null ? NoopIoEventQueueHandler.Instance : queueHandler;
        }

        public IoEventQueueHandler QueueHandler
        {
            get { return _queueHandler; }
        }

        /// <inheritdoc/>
        public void Execute(IoEvent ioe)
        {
            Boolean offeredEvent = _queueHandler.Accept(this, ioe);
            if (offeredEvent)
            {
                Execute(() =>
                {
                    _queueHandler.Polled(this, ioe);
                    ioe.Fire();
                });

                _queueHandler.Offered(this, ioe);
            }
        }
    }
}
