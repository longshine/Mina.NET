using System;
using Mina.Core.Session;
using Mina.Core.Write;
using System.Collections.Generic;

namespace Mina.Core.Filterchain
{
    /// <summary>
    /// A container of <see cref="IoFilter"/>s that forwards <see cref="IoHandler"/> events
    /// to the consisting filters and terminal <see cref="IoHandler"/> sequentially.
    /// Every <see cref="IoSession"/> has its own <see cref="IoFilterChain"/> (1-to-1 relationship).
    /// </summary>
    public interface IoFilterChain : IChain<IoFilter, INextFilter>
    {
        /// <summary>
        /// Gets the parent <see cref="IoSession"/> of this chain.
        /// </summary>
        IoSession Session { get; }

        void FireSessionCreated();
        void FireSessionOpened();
        void FireSessionClosed();
        void FireSessionIdle(IdleStatus status);
        void FireMessageReceived(Object message);
        void FireMessageSent(IWriteRequest request);
        void FireExceptionCaught(Exception ex);
        void FireFilterWrite(IWriteRequest writeRequest);
        void FireFilterClose();
    }
}
