using System;
using System.Threading.Tasks;

namespace Mina.Filter.Executor
{
    /// <summary>
    /// Executes submitted tasks in a thread pool.
    /// </summary>
    public class ThreadPoolExecutor : IExecutor
    {
        /// <inheritdoc/>
        public void Execute(Action task)
        {
            Task.Factory.StartNew(task);
        }
    }
}
