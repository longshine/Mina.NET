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
    public interface IoService : IDisposable
    {
        /// <summary>
        /// Gets the <see cref="ITransportMetadata"/> that this service runs on.
        /// </summary>
        ITransportMetadata TransportMetadata { get; }
        /// <summary>
        /// Returns <code>true</code> if and if only all resources of this service
        /// have been disposed.
        /// </summary>
        Boolean Disposed { get; }
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
        /// Gets or sets the <see cref="IoFilterChainBuilder"/> which will build the
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

        /// <summary>
        /// Writes the specified message to all the <see cref="IoSession"/>s
        /// managed by this service.
        /// </summary>
        IEnumerable<IWriteFuture> Broadcast(Object message);

        /// <summary>
        /// Fires when this service is activated.
        /// </summary>
        event EventHandler Activated;
        /// <summary>
        /// Fires when this service is idle.
        /// </summary>
        event EventHandler<IdleEventArgs> Idle;
        /// <summary>
        /// Fires when this service is deactivated.
        /// </summary>
        event EventHandler Deactivated;
        /// <summary>
        /// Fires when a new session is created.
        /// </summary>
        event EventHandler<IoSessionEventArgs> SessionCreated;
        /// <summary>
        /// Fires when a new session is being destroyed.
        /// </summary>
        event EventHandler<IoSessionEventArgs> SessionDestroyed;
        /// <summary>
        /// Fires when a session is opened. Only available when
        /// no <see cref="IoHandler"/> is set to <see cref="Handler"/>.
        /// </summary>
        /// <remarks>
        /// If <see cref="Handler"/> is set, use <see cref="IoHandler.SessionOpened(IoSession)"/> instead.
        /// </remarks>
        event EventHandler<IoSessionEventArgs> SessionOpened;
        /// <summary>
        /// Fires when a session is closed. Only available when
        /// no <see cref="IoHandler"/> is set to <see cref="Handler"/>.
        /// </summary>
        /// <remarks>
        /// If <see cref="Handler"/> is set, use <see cref="IoHandler.SessionClosed(IoSession)"/> instead.
        /// </remarks>
        event EventHandler<IoSessionEventArgs> SessionClosed;
        /// <summary>
        /// Fires when a session is idle. Only available when
        /// no <see cref="IoHandler"/> is set to <see cref="Handler"/>.
        /// </summary>
        /// <remarks>
        /// If <see cref="Handler"/> is set, use <see cref="IoHandler.SessionIdle(IoSession, IdleStatus)"/> instead.
        /// </remarks>
        event EventHandler<IoSessionIdleEventArgs> SessionIdle;
        /// <summary>
        /// Fires when any exception is thrown. Only available when
        /// no <see cref="IoHandler"/> is set to <see cref="Handler"/>.
        /// </summary>
        /// <remarks>
        /// If <see cref="Handler"/> is set, use <see cref="IoHandler.ExceptionCaught(IoSession, Exception)"/> instead.
        /// </remarks>
        event EventHandler<IoSessionExceptionEventArgs> ExceptionCaught;
        /// <summary>
        /// Occurs when the closure of an half-duplex channel.
        /// </summary>
        event EventHandler<IoSessionEventArgs> InputClosed;
        /// <summary>
        /// Fires when a message is received. Only available when
        /// no <see cref="IoHandler"/> is set to <see cref="Handler"/>.
        /// </summary>
        /// <remarks>
        /// If <see cref="Handler"/> is set, use <see cref="IoHandler.MessageReceived(IoSession, Object)"/> instead.
        /// </remarks>
        event EventHandler<IoSessionMessageEventArgs> MessageReceived;
        /// <summary>
        /// Fires when a message is sent. Only available when
        /// no <see cref="IoHandler"/> is set to <see cref="Handler"/>.
        /// </summary>
        /// <remarks>
        /// If <see cref="Handler"/> is set, use <see cref="IoHandler.MessageSent(IoSession, Object)"/> instead.
        /// </remarks>
        event EventHandler<IoSessionMessageEventArgs> MessageSent;

        /// <summary>
        /// Gets the IoServiceStatistics object for this service.
        /// </summary>
        IoServiceStatistics Statistics { get; }
    }
}
