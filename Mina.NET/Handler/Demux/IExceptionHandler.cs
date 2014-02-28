using System;
using Mina.Core.Session;

namespace Mina.Handler.Demux
{
    /// <summary>
    /// A handler interface that <see cref="DemuxingIoHandler"/> forwards
    /// <code>ExceptionCaught</code> events to.
    /// </summary>
    public interface IExceptionHandler
    {
        /// <summary>
        /// Invoked when the specific type of exception is caught from the
        /// specified <code>session</code>.
        /// </summary>
        void ExceptionCaught(IoSession session, Exception cause);
    }

    /// <summary>
    /// A handler interface that <see cref="DemuxingIoHandler"/> forwards
    /// <code>ExceptionCaught</code> events to.
    /// </summary>
    /// <typeparam name="E"></typeparam>
    public interface IExceptionHandler<in E> : IExceptionHandler where E : Exception
    {
        /// <summary>
        /// Invoked when the specific type of exception is caught from the
        /// specified <code>session</code>.
        /// </summary>
        void ExceptionCaught(IoSession session, E cause);
    }
}
