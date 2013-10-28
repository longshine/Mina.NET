using System;

namespace Mina.Core.Future
{
    /// <summary>
    /// An <see cref="IoFuture"/> for asynchronous read requests.
    /// </summary>
    public interface IReadFuture : IoFuture
    {
        Object Message { get; set; }
        Boolean Read { get; }
        Boolean Closed { get; set; }
        Exception Exception { get; set; }
        new IReadFuture Await();
    }
}
