using System;

namespace Mina.Filter.Executor
{
    /// <summary>
    /// Provides methods to execute submitted tasks.
    /// </summary>
    public interface IExecutor
    {
        void Execute(Action task);
    }
}
