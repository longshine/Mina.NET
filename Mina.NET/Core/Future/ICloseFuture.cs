using System;

namespace Mina.Core.Future
{
    /// <summary>
    /// An <see cref="IoFuture"/> for asynchronous close requests.
    /// </summary>
    public interface ICloseFuture : IoFuture
    {
        Boolean Closed { get; set; }
        new ICloseFuture Await();
    }
}
