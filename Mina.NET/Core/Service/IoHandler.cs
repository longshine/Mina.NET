using System;
using Mina.Core.Session;

namespace Mina.Core.Service
{
    /// <summary>
    /// Handles all I/O events fired by MINA.
    /// </summary>
    public interface IoHandler
    {
        /// <summary>
        /// Invoked from an I/O processor thread when a new connection has been created.
        /// </summary>
        void SessionCreated(IoSession session);
        /// <summary>
        /// Invoked when a connection has been opened.
        /// This method is invoked after <see cref="SessionCreated(IoSession)"/>.
        /// </summary>
        /// <remarks>
        /// The biggest difference from <see cref="SessionCreated(IoSession)"/> is that
        /// it's invoked from other thread than an I/O processor thread once
        /// thread model is configured properly.
        /// </remarks>
        void SessionOpened(IoSession session);
        /// <summary>
        /// Invoked when a connection is closed.
        /// </summary>
        void SessionClosed(IoSession session);
        /// <summary>
        /// Invoked with the related <see cref="IdleStatus"/> when a connection becomes idle.
        /// </summary>
        void SessionIdle(IoSession session, IdleStatus status);
        /// <summary>
        /// Invoked when any exception is thrown by user <see cref="IoHandler"/>
        /// implementation or by Mina.
        /// </summary>
        void ExceptionCaught(IoSession session, Exception cause);
        /// <summary>
        /// Invoked when a message is received.
        /// </summary>
        void MessageReceived(IoSession session, Object message);
        /// <summary>
        /// Invoked when a message written by <see cref="IoSession.Write(Object)"/>
        /// is sent out.
        /// </summary>
        void MessageSent(IoSession session, Object message);
        ///
        /// Handle the closure of an half-duplex channel.
        ///
        void InputClosed(IoSession session);
    }
}
