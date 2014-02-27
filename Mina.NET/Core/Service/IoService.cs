using System;
using System.Collections.Generic;
using Mina.Core.Filterchain;
using Mina.Core.Future;
using Mina.Core.Session;

namespace Mina.Core.Service
{
    /// <summary>
    /// Base interface for all <see cref="IoAcceptor"/>s and <see cref="IoConnector"/>s
    /// that provide I/O service and manage <see cref="IoSession"/>s.
    /// </summary>
    public interface IoService
    {
        /// <summary>
        /// Gets or sets the handler which will handle all connections managed by this service.
        /// </summary>
        IoHandler Handler { get; set; }
        /// <summary>
        /// Gets the map of all sessions which are currently managed by this service.
        /// </summary>
        IDictionary<Int64, IoSession> ManagedSessions { get; }
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

        event EventHandler Activated;
        event EventHandler<IdleStatusEventArgs> Idle;
        event EventHandler Deactivated;
        event EventHandler<IoSessionEventArgs> SessionCreated;
        event EventHandler<IoSessionEventArgs> SessionDestroyed;
        event EventHandler<IoSessionEventArgs> SessionOpened;
        event EventHandler<IoSessionEventArgs> SessionClosed;
        event EventHandler<IoSessionIdleEventArgs> SessionIdle;
        event EventHandler<IoSessionExceptionEventArgs> ExceptionCaught;
        event EventHandler<IoSessionMessageEventArgs> MessageReceived;
        event EventHandler<IoSessionMessageEventArgs> MessageSent;

        /// <summary>
        /// Gets the IoServiceStatistics object for this service.
        /// </summary>
        IoServiceStatistics Statistics { get; }
    }
}
