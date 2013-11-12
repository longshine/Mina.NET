using System;
using Mina.Core.Session;

namespace Mina.Filter.Executor
{
    /// <summary>
    /// Listens and filters all event queue operations occurring in
    /// <see cref="OrderedThreadPoolExecutor"/> and <see cref="UnorderedThreadPoolExecutor"/>.
    /// </summary>
    public interface IoEventQueueHandler
    {
        /// <summary>
        /// Returns <tt>true</tt> if and only if the specified <tt>event</tt> is
        /// allowed to be offered to the event queue.  The <tt>event</tt> is dropped
        /// if <tt>false</tt> is returned.
        /// </summary>
        Boolean Accept(Object source, IoEvent ioe);
        /// <summary>
        /// Invoked after the specified <paramref name="ioe"/> has been offered to the event queue.
        /// </summary>
        void Offered(Object source, IoEvent ioe);
        /// <summary>
        /// Invoked after the specified <paramref name="ioe"/> has been polled to the event queue.
        /// </summary>
        void Polled(Object source, IoEvent ioe);
    }

    class NoopIoEventQueueHandler : IoEventQueueHandler
    {
        public static readonly NoopIoEventQueueHandler Instance = new NoopIoEventQueueHandler();

        private NoopIoEventQueueHandler()
        { }

        public Boolean Accept(Object source, IoEvent ioe)
        {
            return true;
        }

        public void Offered(Object source, IoEvent ioe)
        {
            // NOOP
        }

        public void Polled(Object source, IoEvent ioe)
        {
            // NOOP
        }
    }
}
