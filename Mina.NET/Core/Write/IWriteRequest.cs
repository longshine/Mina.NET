using System;
using System.Net;
using Mina.Core.Future;

namespace Mina.Core.Write
{
    /// <summary>
    /// Represents write request fired by <see cref="Core.Session.IoSession.Write(Object)"/>.
    /// </summary>
    public interface IWriteRequest
    {
        /// <summary>
        /// Gets the <see cref="IWriteRequest"/> which was requested originally,
        /// which is not transformed by any <see cref="Core.Filterchain.IoFilter"/>.
        /// </summary>
        IWriteRequest OriginalRequest { get; }
        /// <summary>
        /// Gets the message object to be written.
        /// </summary>
        Object Message { get; }
        /// <summary>
        /// Gets the destination of this write request.
        /// </summary>
        EndPoint Destination { get; }
        /// <summary>
        /// Tells if the current message has been encoded.
        /// </summary>
        Boolean Encoded { get; }
        /// <summary>
        /// Gets <see cref="IWriteFuture"/> that is associated with this write request.
        /// </summary>
        IWriteFuture Future { get; }
    }
}
