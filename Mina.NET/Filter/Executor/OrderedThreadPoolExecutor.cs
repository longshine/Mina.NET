using System;
using System.Collections.Concurrent;
using System.Text;
using Common.Logging;
using Mina.Core.Session;

namespace Mina.Filter.Executor
{
    /// <summary>
    /// A <see cref="IoEventExecutor"/> that maintains the order of <see cref="IoEvent"/>s.
    /// If you don't need to maintain the order of events per session, please use
    /// <see cref="UnorderedThreadPoolExecutor"/>.
    /// </summary>
    public class OrderedThreadPoolExecutor : ThreadPoolExecutor, IoEventExecutor
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(OrderedThreadPoolExecutor));

        /// <summary>
        /// A key stored into the session's attribute for the event tasks being queued
        /// </summary>
        private readonly AttributeKey TASKS_QUEUE = new AttributeKey(typeof(OrderedThreadPoolExecutor), "tasksQueue");
        private readonly IoEventQueueHandler _queueHandler;

        /// <summary>
        /// Instantiates with a <see cref="NoopIoEventQueueHandler"/>.
        /// </summary>
        public OrderedThreadPoolExecutor()
            : this(null)
        { }

        /// <summary>
        /// Instantiates with the given <see cref="IoEventQueueHandler"/>.
        /// </summary>
        /// <param name="queueHandler">the handler</param>
        public OrderedThreadPoolExecutor(IoEventQueueHandler queueHandler)
        {
            _queueHandler = queueHandler == null ? NoopIoEventQueueHandler.Instance : queueHandler;
        }

        /// <summary>
        /// Gets the <see cref="IoEventQueueHandler"/>.
        /// </summary>
        public IoEventQueueHandler QueueHandler
        {
            get { return _queueHandler; }
        }

        /// <inheritdoc/>
        public void Execute(IoEvent ioe)
        {
            IoSession session = ioe.Session;
            SessionTasksQueue sessionTasksQueue = GetSessionTasksQueue(session);
            Boolean exec;

            // propose the new event to the event queue handler. If we
            // use a throttle queue handler, the message may be rejected
            // if the maximum size has been reached.
            Boolean offerEvent = _queueHandler.Accept(this, ioe);

            if (offerEvent)
            {
                lock (sessionTasksQueue.syncRoot)
                {
                    sessionTasksQueue.tasksQueue.Enqueue(ioe);

                    if (sessionTasksQueue.processingCompleted)
                    {
                        sessionTasksQueue.processingCompleted = false;
                        exec = true;
                    }
                    else
                    {
                        exec = false;
                    }

                    if (log.IsDebugEnabled)
                        Print(sessionTasksQueue.tasksQueue, ioe);
                }

                if (exec)
                {
                    Execute(() =>
                    {
                        RunTasks(sessionTasksQueue);
                    });
                }

                _queueHandler.Offered(this, ioe);
            }
        }

        private SessionTasksQueue GetSessionTasksQueue(IoSession session)
        {
            SessionTasksQueue queue = session.GetAttribute<SessionTasksQueue>(TASKS_QUEUE);

            if (queue == null)
            {
                queue = new SessionTasksQueue();
                SessionTasksQueue oldQueue = (SessionTasksQueue)session.SetAttributeIfAbsent(TASKS_QUEUE, queue);
                if (oldQueue != null)
                    queue = oldQueue;
            }

            return queue;
        }

        private void RunTasks(SessionTasksQueue sessionTasksQueue)
        {
            IoEvent ioe;
            while (true)
            {
                lock (sessionTasksQueue.syncRoot)
                {
                    if (!sessionTasksQueue.tasksQueue.TryDequeue(out ioe))
                    {
                        sessionTasksQueue.processingCompleted = true;
                        break;
                    }
                }

                _queueHandler.Polled(this, ioe);
                ioe.Fire();
            }
        }

        private void Print(ConcurrentQueue<IoEvent> queue, IoEvent ioe)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Adding event ")
                .Append(ioe.EventType)
                .Append(" to session ")
                .Append(ioe.Session.Id);
            Boolean first = true;
            sb.Append("\nQueue : [");
            foreach (IoEvent elem in queue)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append(", ");
                }

                sb.Append(((IoEvent)elem).EventType).Append(", ");
            }
            sb.Append("]\n");
            log.Debug(sb.ToString());
        }

        class SessionTasksQueue
        {
            public readonly Object syncRoot = new Byte[0];
            /// <summary>
            /// A queue of ordered event waiting to be processed
            /// </summary>
            public readonly ConcurrentQueue<IoEvent> tasksQueue = new ConcurrentQueue<IoEvent>();
            /// <summary>
            /// The current task state
            /// </summary>
            public Boolean processingCompleted = true;
        }
    }
}
