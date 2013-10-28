using System;
using Mina.Core.Session;

namespace Mina.Core.Future
{
    /// <summary>
    /// An <see cref="IoFuture"/> for asynchronous connect requests.
    /// </summary>
    public interface IConnectFuture : IoFuture
    {
        Boolean Connected { get; }
        Boolean Canceled { get; }
        Exception Exception { get; set; }
        void SetSession(IoSession session);
        void Cancel();
        new IConnectFuture Await();
    }
}
