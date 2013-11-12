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
        public void Execute(IoEvent ioe)
        {
            Execute(() => ioe.Fire());
        }
    }
}
