using System;

namespace Mina.Core.Future
{
    /// <summary>
    /// An <see cref="IoFuture"/> for asynchronous write requests.
    /// </summary>
    public interface IWriteFuture : IoFuture
    {
        Boolean Written { get; set; }
        Exception Exception { get; set; }
        new IWriteFuture Await();
    }
}
