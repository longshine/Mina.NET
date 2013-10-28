using System;
using System.Threading;
using Mina.Core.Session;
using Mina.Core.Filterchain;
using Mina.Core.Future;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Mina.Core.Buffer;

namespace Mina.Core.Service
{
    /// <summary>
    /// Base implementation of <see cref="IoService"/>s.
    /// </summary>
    public abstract class AbstractIoService : IoService, IoServiceSupport
    {
        private Int32 _active = 0;
        private DateTime _activationTime;
        private IoHandler _handler;
        private readonly IoSessionConfig _sessionConfig;
        private IoFilterChainBuilder _filterChainBuilder = new DefaultIoFilterChainBuilder();
        private IoSessionDataStructureFactory _sessionDataStructureFactory = new DefaultIoSessionDataStructureFactory();

        private ConcurrentDictionary<Int64, IoSession> _managedSessions = new ConcurrentDictionary<Int64, IoSession>();

        public event Action Activated;

        public AbstractIoService(IoSessionConfig sessionConfig)
        {
            _sessionConfig = sessionConfig;
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

        protected void OnActivated()
        {
            if (Interlocked.CompareExchange(ref _active, 1, 0) > 0)
                // The instance is already active
                return;
            _activationTime = DateTime.Now;
            Action act = Activated;
            if (act != null)
                act();
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
        protected void FinishSessionInitialization0(IoSession session, IoFuture future)
        {
            // Do nothing. Extended class might add some specific code 
        }

        protected virtual void Dispose(Boolean disposing)
        { 

        }

        #region IoServiceSupport
        
        void IoServiceSupport.FireServiceActivated()
        {
            throw new NotImplementedException();
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

            // TODO OnSessionCreated();
        }

        #endregion
    }
}
