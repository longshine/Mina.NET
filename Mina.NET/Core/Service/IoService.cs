using System;
using Mina.Core.Session;
using Mina.Core.Filterchain;
using Mina.Core.Future;
using System.Collections.Generic;

namespace Mina.Core.Service
{
    /// <summary>
    /// Base interface for all <see cref="IoAcceptor"/>s and <see cref="IoConnector"/>s
    /// that provide I/O service and manage <see cref="IoSession"/>s.
    /// </summary>
    public interface IoService : IDisposable
    {
        /// <summary>
        /// Gets or sets the handler which will handle all connections managed by this service.
        /// </summary>
        IoHandler Handler { get; set; }
        /// <summary>
        /// Returns a value of whether or not this service is active.
        /// </summary>
        Boolean Active { get; }
        /// <summary>
        /// Returns the time when this service was activated.
        /// </summary>
        DateTime ActivationTime { get; }
        /// <summary>
        /// Returns the default configuration of the new <see cref="IoSession"/>s created by this service.
        /// </summary>
        IoSessionConfig SessionConfig { get; }
        /// <summary>
        /// Sets the <see cref="IoFilterChainBuilder"/> which will build the
        /// <see cref="IoFilterChain"/> of all <see cref="IoSession"/>s which is created by this service.
        /// </summary>
        IoFilterChainBuilder FilterChainBuilder { get; set; }
        /// <summary>
        /// A shortcut for <tt>( ( DefaultIoFilterChainBuilder ) </tt><see cref="FilterChainBuilder"/><tt> )</tt>.
        /// </summary>
        DefaultIoFilterChainBuilder FilterChain { get; }
        /// <summary>
        /// Gets or sets the <see cref="IoSessionDataStructureFactory"/> that provides
        /// related data structures for a new session created by this service.
        /// </summary>
        IoSessionDataStructureFactory SessionDataStructureFactory { get; set; }

        IEnumerable<IWriteFuture> Broadcast(Object message);

        event Action<IoSession> SessionCreated;
        event Action<IoSession> SessionOpened;
        event Action<IoSession> SessionClosed;
        event Action<IoSession, IdleStatus> SessionIdle;
        event Action<IoSession, Exception> ExceptionCaught;
        event Action<IoSession, Object> MessageReceived;
        event Action<IoSession, Object> MessageSent;

        /// <summary>
        /// Gets the IoServiceStatistics object for this service.
        /// </summary>
        IoServiceStatistics Statistics { get; }
    }
}
