using System;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Core.Filterchain
{
    /// <summary>
    /// A container of <see cref="IoFilter"/>s that forwards <see cref="Core.Service.IoHandler"/> events
    /// to the consisting filters and terminal <see cref="Core.Service.IoHandler"/> sequentially.
    /// Every <see cref="IoSession"/> has its own <see cref="IoFilterChain"/> (1-to-1 relationship).
    /// </summary>
    public interface IoFilterChain : IChain<IoFilter, INextFilter>
    {
        /// <summary>
        /// Gets the parent <see cref="IoSession"/> of this chain.
        /// </summary>
        IoSession Session { get; }
        /// <summary>
        /// Fires a <see cref="Core.Service.IoHandler.SessionCreated(IoSession)"/> event.
        /// </summary>
        void FireSessionCreated();
        /// <summary>
        /// Fires a <see cref="Core.Service.IoHandler.SessionOpened(IoSession)"/> event.
        /// </summary>
        void FireSessionOpened();
        /// <summary>
        /// Fires a <see cref="Core.Service.IoHandler.SessionClosed(IoSession)"/> event.
        /// </summary>
        void FireSessionClosed();
        /// <summary>
        /// Fires a <see cref="Core.Service.IoHandler.SessionIdle(IoSession, IdleStatus)"/> event.
        /// </summary>
        void FireSessionIdle(IdleStatus status);
        /// <summary>
        /// Fires a <see cref="Core.Service.IoHandler.MessageReceived(IoSession, Object)"/> event.
        /// </summary>
        void FireMessageReceived(Object message);
        /// <summary>
        /// Fires a <see cref="Core.Service.IoHandler.MessageSent(IoSession, Object)"/> event.
        /// </summary>
        void FireMessageSent(IWriteRequest request);
        /// <summary>
        /// Fires a <see cref="Core.Service.IoHandler.ExceptionCaught(IoSession, Exception)"/> event.
        /// </summary>
        void FireExceptionCaught(Exception ex);
        /// <summary>
        /// Fires a <see cref="Core.Service.IoHandler.InputClosed(IoSession)"/> event. Most users don't
        /// need to call this method at all. Please use this method only when you
        /// implement a new transport or fire a virtual event.
        /// </summary>
        void FireInputClosed();
        /// <summary>
        /// Fires a <see cref="IoSession.Write(Object)"/> event.
        /// </summary>
        void FireFilterWrite(IWriteRequest writeRequest);
        /// <summary>
        /// Fires a <see cref="IoSession.Close(Boolean)"/> event.
        /// </summary>
        void FireFilterClose();
    }
}
