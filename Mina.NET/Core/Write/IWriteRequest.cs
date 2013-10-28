using System;
using Mina.Core.Future;

namespace Mina.Core.Write
{
    public interface IWriteRequest
    {
        IWriteRequest OriginalRequest { get; }
        Object Message { get; }
        Boolean Encoded { get; }
        IWriteFuture Future { get; }
    }
}
