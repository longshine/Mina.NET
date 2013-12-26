using System.Threading;

namespace Mina.Filter.Executor
{
    public class ThreadPoolExecutor : IExecutor
    {
        public void Execute(Action task)
        {
            ThreadPool.QueueUserWorkItem(o => task());
        }
    }
}
