using System;
using Common.Logging;
using Mina.Core.Filterchain;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Filter.Logging
{
    /// <summary>
    /// Logs all MINA protocol events.
    /// </summary>
    public class LoggingFilter : IoFilterAdapter
    {
        private readonly String _name;
        private readonly ILog _log;

        /// <summary>
        /// Default Constructor.
        /// </summary>
        public LoggingFilter()
            : this(typeof(LoggingFilter).Name)
        { }

        /// <summary>
        /// Create a new LoggingFilter using a class name
        /// </summary>
        /// <param name="type">the cass which name will be used to create the logger</param>
        public LoggingFilter(Type type)
            : this(type.Name)
        { }

        /// <summary>
        /// Create a new LoggingFilter using a name
        /// </summary>
        /// <param name="name">the name used to create the logger. If null, will default to "LoggingFilter"</param>
        public LoggingFilter(String name)
        {
            _name = name ?? typeof(LoggingFilter).Name;
            _log = LogManager.GetLogger(_name);

            ExceptionCaughtLevel = LogLevel.Warn;
            MessageReceivedLevel = LogLevel.Info;
            MessageSentLevel = LogLevel.Info;
            SessionCreatedLevel = LogLevel.Info;
            SessionOpenedLevel = LogLevel.Info;
            SessionIdleLevel = LogLevel.Info;
            SessionClosedLevel = LogLevel.Info;
        }

        /// <summary>
        /// Gets the logger's name
        /// </summary>
        public String Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Gets or sets the LogLevel for the ExceptionCaught event.
        /// </summary>
        public LogLevel ExceptionCaughtLevel { get; set; }

        /// <summary>
        /// Gets or sets the LogLevel for the MessageReceived event.
        /// </summary>
        public LogLevel MessageReceivedLevel { get; set; }

        /// <summary>
        /// Get the LogLevel for the MessageSent event.
        /// </summary>
        public LogLevel MessageSentLevel { get; set; }

        /// <summary>
        /// Get the LogLevel for the SessionCreated event.
        /// </summary>
        public LogLevel SessionCreatedLevel { get; set; }

        /// <summary>
        /// Get the LogLevel for the SessionOpened event.
        /// </summary>
        public LogLevel SessionOpenedLevel { get; set; }

        /// <summary>
        /// Get the LogLevel for the SessionIdle event.
        /// </summary>
        public LogLevel SessionIdleLevel { get; set; }

        /// <summary>
        /// Get the LogLevel for the SessionClosed event.
        /// </summary>
        public LogLevel SessionClosedLevel { get; set; }

        /// <inheritdoc/>
        public override void ExceptionCaught(INextFilter nextFilter, IoSession session, Exception cause)
        {
            Log(ExceptionCaughtLevel, "EXCEPTION :", cause);
            base.ExceptionCaught(nextFilter, session, cause);
        }

        /// <inheritdoc/>
        public override void MessageReceived(INextFilter nextFilter, IoSession session, Object message)
        {
            Log(MessageReceivedLevel, "RECEIVED: {0}", message);
            base.MessageReceived(nextFilter, session, message);
        }

        /// <inheritdoc/>
        public override void MessageSent(INextFilter nextFilter, IoSession session, IWriteRequest writeRequest)
        {
            Log(MessageSentLevel, "SENT: {0}", writeRequest.OriginalRequest.Message);
            base.MessageSent(nextFilter, session, writeRequest);
        }

        /// <inheritdoc/>
        public override void SessionCreated(INextFilter nextFilter, IoSession session)
        {
            Log(SessionCreatedLevel, "CREATED");
            base.SessionCreated(nextFilter, session);
        }

        /// <inheritdoc/>
        public override void SessionOpened(INextFilter nextFilter, IoSession session)
        {
            Log(SessionOpenedLevel, "OPENED");
            base.SessionOpened(nextFilter, session);
        }

        /// <inheritdoc/>
        public override void SessionIdle(INextFilter nextFilter, IoSession session, IdleStatus status)
        {
            Log(SessionIdleLevel, "IDLE"); 
            base.SessionIdle(nextFilter, session, status);
        }

        /// <inheritdoc/>
        public override void SessionClosed(INextFilter nextFilter, IoSession session)
        {
            Log(SessionClosedLevel, "CLOSED"); 
            base.SessionClosed(nextFilter, session);
        }

        private void Log(LogLevel level, String message, Exception cause)
        {
            switch (level)
            {
                case LogLevel.Error:
                    _log.Error(message, cause);
                    break;
                case LogLevel.Warn:
                    _log.Warn(message, cause);
                    break;
                case LogLevel.Info:
                    _log.Info(message, cause);
                    break;
                case LogLevel.Debug:
                    _log.Debug(message, cause);
                    break;
                case LogLevel.Trace:
                    _log.Trace(message, cause);
                    break;
                default:
                    break;
            }
        }

        private void Log(LogLevel level, String message, params Object[] args)
        {
            switch (level)
            {
                case LogLevel.Error:
                    _log.ErrorFormat(message, args);
                    break;
                case LogLevel.Warn:
                    _log.WarnFormat(message, args);
                    break;
                case LogLevel.Info:
                    _log.InfoFormat(message, args);
                    break;
                case LogLevel.Debug:
                    _log.DebugFormat(message, args);
                    break;
                case LogLevel.Trace:
                    _log.TraceFormat(message, args);
                    break;
                default:
                    break;
            }
        }
    }
}
