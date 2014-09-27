using System;
using Mina.Core.Filterchain;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Filter.KeepAlive
{
    /// <summary>
    /// An <see cref="IoFilter"/> that sends a keep-alive request on <see cref="IoEventType.SessionIdle"/>
    /// and sends back the response for the sent keep-alive request. 
    /// </summary>
    public class KeepAliveFilter : IoFilterAdapter
    {
        private readonly AttributeKey WAITING_FOR_RESPONSE;
        private readonly AttributeKey IGNORE_READER_IDLE_ONCE;

        private readonly IKeepAliveMessageFactory _messageFactory;
        private readonly IdleStatus _interestedIdleStatus;
        private volatile IKeepAliveRequestTimeoutHandler _requestTimeoutHandler;
        private volatile Int32 _requestInterval;
        private volatile Int32 _requestTimeout;
        private volatile Boolean _forwardEvent;

        /// <summary>
        /// Creates a new instance with the default properties.
        /// <ul>
        ///   <li>interestedIdleStatus - <see cref="IdleStatus.ReaderIdle"/></li>
        ///   <li>strategy - <see cref="KeepAliveRequestTimeoutHandler.Close"/></li>
        ///   <li>keepAliveRequestInterval - 60 (seconds)</li>
        ///   <li>keepAliveRequestTimeout - 30 (seconds)</li>
        /// </ul>
        /// </summary>
        /// <param name="messageFactory">the factory to generate keep-alive messages</param>
        public KeepAliveFilter(IKeepAliveMessageFactory messageFactory)
            : this(messageFactory, IdleStatus.ReaderIdle, KeepAliveRequestTimeoutHandler.Close)
        { }

        /// <summary>
        /// Creates a new instance with the default properties.
        /// <ul>
        ///   <li>strategy - <see cref="KeepAliveRequestTimeoutHandler.Close"/></li>
        ///   <li>keepAliveRequestInterval - 60 (seconds)</li>
        ///   <li>keepAliveRequestTimeout - 30 (seconds)</li>
        /// </ul>
        /// </summary>
        /// <param name="messageFactory">the factory to generate keep-alive messages</param>
        /// <param name="interestedIdleStatus"></param>
        public KeepAliveFilter(IKeepAliveMessageFactory messageFactory, IdleStatus interestedIdleStatus)
            : this(messageFactory, interestedIdleStatus, KeepAliveRequestTimeoutHandler.Close)
        { }

        /// <summary>
        /// Creates a new instance with the default properties.
        /// <ul>
        ///   <li>interestedIdleStatus - <see cref="IdleStatus.ReaderIdle"/></li>
        ///   <li>keepAliveRequestInterval - 60 (seconds)</li>
        ///   <li>keepAliveRequestTimeout - 30 (seconds)</li>
        /// </ul>
        /// </summary>
        /// <param name="messageFactory">the factory to generate keep-alive messages</param>
        /// <param name="strategy"></param>
        public KeepAliveFilter(IKeepAliveMessageFactory messageFactory, IKeepAliveRequestTimeoutHandler strategy)
            : this(messageFactory, IdleStatus.ReaderIdle, strategy)
        { }

        /// <summary>
        /// Creates a new instance with the default properties.
        /// <ul>
        ///   <li>keepAliveRequestInterval - 60 (seconds)</li>
        ///   <li>keepAliveRequestTimeout - 30 (seconds)</li>
        /// </ul>
        /// </summary>
        /// <param name="messageFactory">the factory to generate keep-alive messages</param>
        /// <param name="interestedIdleStatus"></param>
        /// <param name="strategy"></param>
        public KeepAliveFilter(IKeepAliveMessageFactory messageFactory, IdleStatus interestedIdleStatus,
            IKeepAliveRequestTimeoutHandler strategy)
            : this(messageFactory, interestedIdleStatus, strategy, 60, 30)
        { }

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="messageFactory">the factory to generate keep-alive messages</param>
        /// <param name="interestedIdleStatus"></param>
        /// <param name="strategy"></param>
        /// <param name="keepAliveRequestInterval">the interval to send a keep-alive request</param>
        /// <param name="keepAliveRequestTimeout">the time to wait for a keep-alive response before timed out</param>
        public KeepAliveFilter(IKeepAliveMessageFactory messageFactory, IdleStatus interestedIdleStatus,
            IKeepAliveRequestTimeoutHandler strategy, Int32 keepAliveRequestInterval, Int32 keepAliveRequestTimeout)
        {
            if (messageFactory == null)
                throw new ArgumentNullException("messageFactory");
            if (strategy == null)
                throw new ArgumentNullException("strategy");

            WAITING_FOR_RESPONSE = new AttributeKey(GetType(), "waitingForResponse");
            IGNORE_READER_IDLE_ONCE = new AttributeKey(GetType(), "ignoreReaderIdleOnce");
            _messageFactory = messageFactory;
            _interestedIdleStatus = interestedIdleStatus;
            _requestTimeoutHandler = strategy;
            RequestInterval = keepAliveRequestInterval;
            RequestTimeout = keepAliveRequestTimeout;
        }

        /// <summary>
        /// Gets or sets the interval to send a keep-alive request.
        /// </summary>
        public Int32 RequestInterval
        {
            get { return _requestInterval; }
            set
            {
                if (value == 0)
                    throw new ArgumentException("RequestInterval must be a positive integer: " + value);
                _requestInterval = value;
            }
        }

        /// <summary>
        /// Gets or sets the time to wait for a keep-alive response before timed out.
        /// </summary>
        public Int32 RequestTimeout
        {
            get { return _requestTimeout; }
            set
            {
                if (value == 0)
                    throw new ArgumentException("RequestTimeout must be a positive integer: " + value);
                _requestTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this filter forwards
        /// an <see cref="IoEventType.SessionIdle"/> event to the next filter.
        /// The default value is <code>false</code>.
        /// </summary>
        public Boolean ForwardEvent
        {
            get { return _forwardEvent; }
            set { _forwardEvent = value; }
        }

        public IKeepAliveRequestTimeoutHandler RequestTimeoutHandler
        {
            get { return _requestTimeoutHandler; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                _requestTimeoutHandler = value;
            }
        }

        /// <inheritdoc/>
        public override void OnPreAdd(IoFilterChain parent, String name, INextFilter nextFilter)
        {
            if (parent.Contains(this))
                throw new ArgumentException("You can't add the same filter instance more than once. "
                    + "Create another instance and add it.");
        }

        /// <inheritdoc/>
        public override void OnPostAdd(IoFilterChain parent, String name, INextFilter nextFilter)
        {
            ResetStatus(parent.Session);
        }

        /// <inheritdoc/>
        public override void OnPostRemove(IoFilterChain parent, String name, INextFilter nextFilter)
        {
            ResetStatus(parent.Session);
        }

        /// <inheritdoc/>
        public override void MessageReceived(INextFilter nextFilter, IoSession session, Object message)
        {
            try
            {
                if (_messageFactory.IsRequest(session, message))
                {
                    Object pongMessage = _messageFactory.GetResponse(session, message);

                    if (pongMessage != null)
                        nextFilter.FilterWrite(session, new DefaultWriteRequest(pongMessage));
                }

                if (_messageFactory.IsResponse(session, message))
                    ResetStatus(session);
            }
            finally
            {
                if (!IsKeepAliveMessage(session, message))
                    nextFilter.MessageReceived(session, message);
            }
        }

        /// <inheritdoc/>
        public override void MessageSent(INextFilter nextFilter, IoSession session, IWriteRequest writeRequest)
        {
            Object message = writeRequest.Message;
            if (!IsKeepAliveMessage(session, message))
                nextFilter.MessageSent(session, writeRequest);
        }

        /// <inheritdoc/>
        public override void SessionIdle(INextFilter nextFilter, IoSession session, IdleStatus status)
        {
            if (status == _interestedIdleStatus)
            {
                if (!session.ContainsAttribute(WAITING_FOR_RESPONSE))
                {
                    Object pingMessage = _messageFactory.GetRequest(session);
                    if (pingMessage != null)
                    {
                        nextFilter.FilterWrite(session, new DefaultWriteRequest(pingMessage));

                        // If policy is OFF, there's no need to wait for
                        // the response.
                        if (_requestTimeoutHandler != KeepAliveRequestTimeoutHandler.DeafSpeaker)
                        {
                            MarkStatus(session);
                            if (_interestedIdleStatus == IdleStatus.BothIdle)
                            {
                                session.SetAttribute(IGNORE_READER_IDLE_ONCE);
                            }
                        }
                        else
                        {
                            ResetStatus(session);
                        }
                    }
                }
                else
                {
                    HandlePingTimeout(session);
                }
            }
            else if (status == IdleStatus.ReaderIdle)
            {
                if (session.RemoveAttribute(IGNORE_READER_IDLE_ONCE) == null)
                {
                    if (session.ContainsAttribute(WAITING_FOR_RESPONSE))
                    {
                        HandlePingTimeout(session);
                    }
                }
            }

            if (_forwardEvent)
                nextFilter.SessionIdle(session, status);
        }

        private void ResetStatus(IoSession session)
        {
            session.Config.ReaderIdleTime = 0;
            session.Config.WriterIdleTime = 0;
            session.Config.SetIdleTime(_interestedIdleStatus, RequestInterval);
            session.RemoveAttribute(WAITING_FOR_RESPONSE);
        }

        private Boolean IsKeepAliveMessage(IoSession session, Object message)
        {
            return _messageFactory.IsRequest(session, message) || _messageFactory.IsResponse(session, message);
        }

        private void HandlePingTimeout(IoSession session)
        {
            ResetStatus(session);
            IKeepAliveRequestTimeoutHandler handler = _requestTimeoutHandler;
            if (handler == KeepAliveRequestTimeoutHandler.DeafSpeaker)
                return;
            handler.KeepAliveRequestTimedOut(this, session);
        }

        private void MarkStatus(IoSession session)
        {
            session.Config.SetIdleTime(_interestedIdleStatus, 0);
            session.Config.ReaderIdleTime = RequestTimeout;
            session.SetAttribute(WAITING_FOR_RESPONSE);
        }
    }
}
