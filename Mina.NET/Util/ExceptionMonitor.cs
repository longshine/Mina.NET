using System;
using Common.Logging;

namespace Mina.Util
{
    /// <summary>
    /// Monitors uncaught exceptions.
    /// </summary>
    public abstract class ExceptionMonitor
    {
        private static ExceptionMonitor _instance = DefaultExceptionMonitor.Monitor;

        /// <summary>
        /// Gets or sets the current exception monitor.
        /// </summary>
        public static ExceptionMonitor Instance
        {
            get { return _instance; }
            set { _instance = value ?? DefaultExceptionMonitor.Monitor; }
        }

        /// <summary>
        /// Invoked when there are any uncaught exceptions.
        /// </summary>
        public abstract void ExceptionCaught(Exception cause);
    }

    class DefaultExceptionMonitor : ExceptionMonitor
    {
        public static readonly DefaultExceptionMonitor Monitor = new DefaultExceptionMonitor();
        private static readonly ILog log = LogManager.GetLogger(typeof(DefaultExceptionMonitor));

        public override void ExceptionCaught(Exception cause)
        {
            if (log.IsWarnEnabled)
                log.Warn("Unexpected exception.", cause);
        }
    }
}
