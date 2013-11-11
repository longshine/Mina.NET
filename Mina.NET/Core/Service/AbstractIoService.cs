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
    public abstract class AbstractIoService : IoService, IoServiceSupport, IoHandler
    {
        private Int32 _active = 0;
        private DateTime _activationTime;
        private IoHandler _handler;
        private readonly IoSessionConfig _sessionConfig;
        private readonly IoServiceStatistics _stats;
        private IoFilterChainBuilder _filterChainBuilder = new DefaultIoFilterChainBuilder();
        private IoSessionDataStructureFactory _sessionDataStructureFactory = new DefaultIoSessionDataStructureFactory();

        private ConcurrentDictionary<Int64, IoSession> _managedSessions = new ConcurrentDictionary<Int64, IoSession>();

        public event Action<IoService> Activated;
        public event Action<IoService, IdleStatus> Idle;
        public event Action<IoService> Deactivated;
        public event Action<IoSession> SessionCreated;
        public event Action<IoSession> SessionOpened;
        public event Action<IoSession> SessionClosed;
        public event Action<IoSession> SessionDestroyed;
        public event Action<IoSession, IdleStatus> SessionIdle;
        public event Action<IoSession, Exception> ExceptionCaught;
        public event Action<IoSession, Object> MessageReceived;
        public event Action<IoSession, Object> MessageSent;

        public AbstractIoService(IoSessionConfig sessionConfig)
        {
            _sessionConfig = sessionConfig;
            _handler = this;
            _stats = new IoServiceStatistics(this);
        }

        public IoHandler Handler
        {
            get { return _handler; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("Handler");
                _handler = value;
            }
        }

        public IDictionary<Int64, IoSession> ManagedSessions
        {
            get { return _managedSessions; }
        }

        public IoSessionConfig SessionConfig
        {
            get { return _sessionConfig; }
        }

        public IoFilterChainBuilder FilterChainBuilder
        {
            get { return _filterChainBuilder; }
            set { _filterChainBuilder = value; }
        }

        public DefaultIoFilterChainBuilder FilterChain
        {
            get { return _filterChainBuilder as DefaultIoFilterChainBuilder; }
        }

        public IoSessionDataStructureFactory SessionDataStructureFactory
        {
            get { return _sessionDataStructureFactory; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();
                else if (Active)
                    throw new InvalidOperationException();
                _sessionDataStructureFactory = value;
            }
        }

        public Boolean Active
        {
            get { return _active > 0; }
        }

        public DateTime ActivationTime
        {
            get { return _activationTime; }
        }

        public IoServiceStatistics Statistics
        {
            get { return _stats; }
        }

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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void InitSession(IoSession session, IoFuture future, Action<IoSession, IoFuture> initializeSession)
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

        protected virtual void Dispose(Boolean disposing)
        { 

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
            DelegateUtils.SaveInvoke(Activated, this);
        }

        void IoServiceSupport.FireSessionCreated(IoSession session)
        {
            // If already registered, ignore.
            if (!_managedSessions.TryAdd(session.Id, session))
                return;

            // Fire session events.
            IoFilterChain filterChain = session.FilterChain;
            filterChain.FireSessionCreated();
            filterChain.FireSessionOpened();

            DelegateUtils.SaveInvoke(SessionCreated, session);
        }

        void IoServiceSupport.FireSessionDestroyed(IoSession session)
        {
            IoSession s;
            if (!_managedSessions.TryRemove(session.Id, out s))
                return;

            // Fire session events.
            session.FilterChain.FireSessionClosed();

            DelegateUtils.SaveInvoke(SessionDestroyed, session);
        }

        #endregion

        #region IoHandler

        void IoHandler.SessionCreated(IoSession session)
        {
            Action<IoSession> act = SessionCreated;
            if (act != null)
                act(session);
        }

        void IoHandler.SessionOpened(IoSession session)
        {
            Action<IoSession> act = SessionOpened;
            if (act != null)
                act(session);
        }

        void IoHandler.SessionClosed(IoSession session)
        {
            Action<IoSession> act = SessionClosed;
            if (act != null)
                act(session);
        }

        void IoHandler.SessionIdle(IoSession session, IdleStatus status)
        {
            Action<IoSession, IdleStatus> act = SessionIdle;
            if (act != null)
                act(session, status);
        }

        void IoHandler.ExceptionCaught(IoSession session, Exception cause)
        {
            Action<IoSession, Exception> act = ExceptionCaught;
            if (act != null)
                act(session, cause);
        }

        void IoHandler.MessageReceived(IoSession session, Object message)
        {
            Action<IoSession, Object> act = MessageReceived;
            if (act != null)
                act(session, message);
        }

        void IoHandler.MessageSent(IoSession session, Object message)
        {
            Action<IoSession, Object> act = MessageSent;
            if (act != null)
                act(session, message);
        }

        #endregion
    }
}
