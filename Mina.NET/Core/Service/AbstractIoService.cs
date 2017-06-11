using System;
using System.Threading;
using Mina.Core.Session;
using Mina.Core.Filterchain;
using Mina.Core.Future;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Mina.Core.Buffer;
using Mina.Util;

namespace Mina.Core.Service
{
    /// <summary>
    /// Base implementation of <see cref="IoService"/>s.
    /// </summary>
    public abstract class AbstractIoService : IoService, IoServiceSupport, IDisposable
    {
        private Int32 _active = 0;
        private DateTime _activationTime;
        private IoHandler _handler;
        private Boolean _hasHandler;
        private readonly IoSessionConfig _sessionConfig;
        private readonly IoServiceStatistics _stats;
        private IoFilterChainBuilder _filterChainBuilder = new DefaultIoFilterChainBuilder();
        private IoSessionDataStructureFactory _sessionDataStructureFactory = new DefaultIoSessionDataStructureFactory();
        private Boolean _disposed;

        private ConcurrentDictionary<Int64, IoSession> _managedSessions = new ConcurrentDictionary<Int64, IoSession>();

        /// <inheritdoc/>
        public event EventHandler Activated;
        /// <inheritdoc/>
        public event EventHandler<IdleEventArgs> Idle;
        /// <inheritdoc/>
        public event EventHandler Deactivated;
        /// <inheritdoc/>
        public event EventHandler<IoSessionEventArgs> SessionCreated;
        /// <inheritdoc/>
        public event EventHandler<IoSessionEventArgs> SessionOpened;
        /// <inheritdoc/>
        public event EventHandler<IoSessionEventArgs> SessionClosed;
        /// <inheritdoc/>
        public event EventHandler<IoSessionEventArgs> SessionDestroyed;
        /// <inheritdoc/>
        public event EventHandler<IoSessionIdleEventArgs> SessionIdle;
        /// <inheritdoc/>
        public event EventHandler<IoSessionExceptionEventArgs> ExceptionCaught;
        /// <inheritdoc/>
        public event EventHandler<IoSessionEventArgs> InputClosed;
        /// <inheritdoc/>
        public event EventHandler<IoSessionMessageEventArgs> MessageReceived;
        /// <inheritdoc/>
        public event EventHandler<IoSessionMessageEventArgs> MessageSent;

        /// <summary>
        /// </summary>
        protected AbstractIoService(IoSessionConfig sessionConfig)
        {
            _sessionConfig = sessionConfig;
            _handler = new InnerHandler(this);
            _stats = new IoServiceStatistics(this);
        }

        /// <inheritdoc/>
        public abstract ITransportMetadata TransportMetadata { get; }

        /// <inheritdoc/>
        public Boolean Disposed { get { return _disposed; } }

        /// <inheritdoc/>
        public IoHandler Handler
        {
            get { return _handler; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                _handler = value;
                _hasHandler = true;
            }
        }

        /// <inheritdoc/>
        public IDictionary<Int64, IoSession> ManagedSessions
        {
            get { return _managedSessions; }
        }

        /// <inheritdoc/>
        public IoSessionConfig SessionConfig
        {
            get { return _sessionConfig; }
        }

        /// <inheritdoc/>
        public IoFilterChainBuilder FilterChainBuilder
        {
            get { return _filterChainBuilder; }
            set { _filterChainBuilder = value; }
        }

        /// <inheritdoc/>
        public DefaultIoFilterChainBuilder FilterChain
        {
            get { return _filterChainBuilder as DefaultIoFilterChainBuilder; }
        }

        /// <inheritdoc/>
        public IoSessionDataStructureFactory SessionDataStructureFactory
        {
            get { return _sessionDataStructureFactory; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                else if (Active)
                    throw new InvalidOperationException();
                _sessionDataStructureFactory = value;
            }
        }

        /// <inheritdoc/>
        public Boolean Active
        {
            get { return _active > 0; }
        }

        /// <inheritdoc/>
        public DateTime ActivationTime
        {
            get { return _activationTime; }
        }

        /// <inheritdoc/>
        public IoServiceStatistics Statistics
        {
            get { return _stats; }
        }

        /// <inheritdoc/>
        public IEnumerable<IWriteFuture> Broadcast(Object message)
        {
            List<IWriteFuture> answer = new List<IWriteFuture>(_managedSessions.Count);
            IoBuffer buf = message as IoBuffer;
            if (buf == null)
            {
                foreach (var session in _managedSessions.Values)
                {
                    answer.Add(session.Write(message));
                }
            }
            else
            {
                foreach (var session in _managedSessions.Values)
                {
                    answer.Add(session.Write(buf.Duplicate()));
                }
            }
            return answer;
        }

        /// <summary>
        /// Disposes resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Initializes sessions.
        /// </summary>
        protected void InitSession<TFuture>(IoSession session, TFuture future, Action<IoSession, TFuture> initializeSession)
            where TFuture : IoFuture
        {
            AbstractIoSession s = session as AbstractIoSession;
            if (s != null)
            {
                s.AttributeMap = s.Service.SessionDataStructureFactory.GetAttributeMap(session);
                s.SetWriteRequestQueue(s.Service.SessionDataStructureFactory.GetWriteRequestQueue(session));
            }

            if (future != null && future is IConnectFuture)
                session.SetAttribute(DefaultIoFilterChain.SessionCreatedFuture, future);

            if (initializeSession != null)
                initializeSession(session, future);

            FinishSessionInitialization0(session, future);
        }

        /// <summary>
        /// Implement this method to perform additional tasks required for session
        /// initialization. Do not call this method directly.
        /// </summary>
        protected virtual void FinishSessionInitialization0(IoSession session, IoFuture future)
        {
            // Do nothing. Extended class might add some specific code 
        }

        /// <summary>
        /// Disposes resources.
        /// </summary>
        protected virtual void Dispose(Boolean disposing)
        {
            _disposed = true;
        }

        private void DisconnectSessions()
        {
            IoAcceptor acceptor = this as IoAcceptor;
            if (acceptor == null)
                // We don't disconnect sessions for anything but an IoAcceptor
                return;

            if (!acceptor.CloseOnDeactivation)
                return;

            List<ICloseFuture> closeFutures = new List<ICloseFuture>(_managedSessions.Count);
            foreach (IoSession s in _managedSessions.Values)
            {
                closeFutures.Add(s.Close(true));
            }

            new CompositeIoFuture<ICloseFuture>(closeFutures).Await();
        }

        #region IoServiceSupport
        
        void IoServiceSupport.FireServiceActivated()
        {
            if (Interlocked.CompareExchange(ref _active, 1, 0) > 0)
                // The instance is already active
                return;
            _activationTime = DateTime.Now;
            _stats.LastReadTime = _activationTime;
            _stats.LastWriteTime = _activationTime;
            _stats.LastThroughputCalculationTime = _activationTime;
            DelegateUtils.SafeInvoke(Activated, this);
        }

        void IoServiceSupport.FireServiceIdle(IdleStatus idleStatus)
        {
            DelegateUtils.SafeInvoke(Idle, this, new IdleEventArgs(idleStatus));
        }

        void IoServiceSupport.FireSessionCreated(IoSession session)
        {
            if (session.Service is IoConnector)
            {
                // If the first connector session, fire a virtual service activation event.
                Boolean firstSession = _managedSessions.IsEmpty;
                if (firstSession)
                    ((IoServiceSupport)this).FireServiceActivated();
            }

            // If already registered, ignore.
            if (!_managedSessions.TryAdd(session.Id, session))
                return;

            // Fire session events.
            IoFilterChain filterChain = session.FilterChain;
            filterChain.FireSessionCreated();
            filterChain.FireSessionOpened();

            if (_hasHandler)
                DelegateUtils.SafeInvoke(SessionCreated, this, new IoSessionEventArgs(session));
        }

        void IoServiceSupport.FireSessionDestroyed(IoSession session)
        {
            IoSession s;
            if (!_managedSessions.TryRemove(session.Id, out s))
                return;

            // Fire session events.
            session.FilterChain.FireSessionClosed();

            DelegateUtils.SafeInvoke(SessionDestroyed, this, new IoSessionEventArgs(session));

            // Fire a virtual service deactivation event for the last session of the connector.
            if (session.Service is IoConnector)
            {
                Boolean lastSession = _managedSessions.IsEmpty;
                if (lastSession)
                    ((IoServiceSupport)this).FireServiceDeactivated();
            }
        }

        void IoServiceSupport.FireServiceDeactivated()
        {
            if (Interlocked.CompareExchange(ref _active, 0, 1) == 0)
                // The instance is already desactivated
                return;
            DelegateUtils.SafeInvoke(Deactivated, this);
            DisconnectSessions();
        }

        #endregion

        class InnerHandler : IoHandler
        {
            private readonly AbstractIoService _service;

            public InnerHandler(AbstractIoService service)
            {
                _service = service;
            }

            public void SessionCreated(IoSession session)
            {
                EventHandler<IoSessionEventArgs> act = _service.SessionCreated;
                if (act != null)
                    act(_service, new IoSessionEventArgs(session));
            }

            void IoHandler.SessionOpened(IoSession session)
            {
                EventHandler<IoSessionEventArgs> act = _service.SessionOpened;
                if (act != null)
                    act(_service, new IoSessionEventArgs(session));
            }

            void IoHandler.SessionClosed(IoSession session)
            {
                EventHandler<IoSessionEventArgs> act = _service.SessionClosed;
                if (act != null)
                    act(_service, new IoSessionEventArgs(session));
            }

            void IoHandler.SessionIdle(IoSession session, IdleStatus status)
            {
                EventHandler<IoSessionIdleEventArgs> act = _service.SessionIdle;
                if (act != null)
                    act(_service, new IoSessionIdleEventArgs(session, status));
            }

            void IoHandler.ExceptionCaught(IoSession session, Exception cause)
            {
                EventHandler<IoSessionExceptionEventArgs> act = _service.ExceptionCaught;
                if (act != null)
                    act(_service, new IoSessionExceptionEventArgs(session, cause));
            }

            void IoHandler.MessageReceived(IoSession session, Object message)
            {
                EventHandler<IoSessionMessageEventArgs> act = _service.MessageReceived;
                if (act != null)
                    act(_service, new IoSessionMessageEventArgs(session, message));
            }

            void IoHandler.MessageSent(IoSession session, Object message)
            {
                EventHandler<IoSessionMessageEventArgs> act = _service.MessageSent;
                if (act != null)
                    act(_service, new IoSessionMessageEventArgs(session, message));
            }

            void IoHandler.InputClosed(IoSession session)
            {
                EventHandler<IoSessionEventArgs> act = _service.InputClosed;
                if (act != null)
                    act(_service, new IoSessionEventArgs(session));
                else
                    session.Close(true);
            }
        }
    }
}
