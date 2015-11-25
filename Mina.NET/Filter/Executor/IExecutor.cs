using System;

namespace Mina.Filter.Executor
{
    /// <summary>
    /// Provides methods to execute submitted tasks.
    /// </summary>
    public interface IExecutor
    {
        /// <summary>
        /// Executes a task.
        /// </summary>
        /// <param name="task">the task to run</param>
        void Execute(Action task);
    }
}
