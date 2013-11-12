using Mina.Core.Session;

namespace Mina.Filter.Executor
{
    /// <summary>
    /// Provides methods to execute submitted <see cref="IoEvent"/>.
    /// </summary>
    public interface IoEventExecutor
    {
        void Execute(IoEvent ioe);
    }
}
