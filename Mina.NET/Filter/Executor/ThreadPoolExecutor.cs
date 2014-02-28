using System;
using System.Threading.Tasks;

namespace Mina.Filter.Executor
{
    public class ThreadPoolExecutor : IExecutor
    {
        /// <inheritdoc/>
        public void Execute(Action task)
        {
            Task.Factory.StartNew(task);
        }
    }
}
