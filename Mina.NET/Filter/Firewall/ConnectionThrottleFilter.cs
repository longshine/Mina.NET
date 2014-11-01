using System;
using System.Collections.Concurrent;
using System.Net;
using Common.Logging;
using Mina.Core.Filterchain;
using Mina.Core.Session;

namespace Mina.Filter.Firewall
{
    /// <summary>
    /// A <see cref="IoFilter"/> which blocks connections from connecting
    /// at a rate faster than the specified interval.
    /// </summary>
    public class ConnectionThrottleFilter : IoFilterAdapter
    {
        static readonly Int64 DefaultTime = 1000L;
        static readonly ILog log = LogManager.GetLogger(typeof(ConnectionThrottleFilter));

        private Int64 _allowedInterval;
        private readonly ConcurrentDictionary<String, DateTime> _clients = new ConcurrentDictionary<String, DateTime>();
        // TODO expire overtime clients

        /// <summary>
        /// Default constructor.  Sets the wait time to 1 second
        /// </summary>
        public ConnectionThrottleFilter()
            : this(DefaultTime)
        { }

        /// <summary>
        /// Constructor that takes in a specified wait time.
        /// </summary>
        /// <param name="allowedInterval">The number of milliseconds a client is allowed to wait before making another successful connection</param>
        public ConnectionThrottleFilter(Int64 allowedInterval)
        {
            this._allowedInterval = allowedInterval;
        }

        /// <summary>
        /// Gets or sets the minimal interval (ms) between connections from a client.
        /// </summary>
        public Int64 AllowedInterval
        {
            get { return _allowedInterval; }
            set { _allowedInterval = value; }
        }

        /// <inheritdoc/>
        public override void SessionCreated(INextFilter nextFilter, IoSession session)
        {
            if (!IsConnectionOk(session))
            {
                if (log.IsWarnEnabled)
                    log.Warn("Connections coming in too fast; closing.");
                session.Close(true);
            }
            base.SessionCreated(nextFilter, session);
        }

        /// <summary>
        /// Method responsible for deciding if a connection is OK to continue.
        /// </summary>
        /// <param name="session">the new session that will be verified</param>
        /// <returns>true if the session meets the criteria, otherwise false</returns>
        public Boolean IsConnectionOk(IoSession session)
        {
            IPEndPoint ep = session.RemoteEndPoint as IPEndPoint;
            if (ep != null)
            {
                String addr = ep.Address.ToString();
                DateTime now = DateTime.Now;
                DateTime? lastConnTime = null;

                _clients.AddOrUpdate(addr, now, (k, v) =>
                {
                    if (log.IsDebugEnabled)
                        log.Debug("This is not a new client");
                    lastConnTime = v;
                    return now;
                });

                if (lastConnTime.HasValue)
                {
                    // if the interval between now and the last connection is
                    // less than the allowed interval, return false
                    if ((now - lastConnTime.Value).TotalMilliseconds < _allowedInterval)
                    {
                        if (log.IsWarnEnabled)
                            log.Warn("Session connection interval too short");
                        return false;
                    }
                }

                return true;
            }

            return false;
        }
    }
}
