using Mina.Core.Session;

namespace Mina.Filter.Executor
{
    /// <summary>
    /// Provides methods to execute submitted <see cref="IoEvent"/>.
    /// </summary>
    public interface IoEventExecutor
    {
        /// <summary>
        /// Executes an event.
        /// </summary>
        /// <param name="ioe">the event to run</param>
        void Execute(IoEvent ioe);
    }
}
