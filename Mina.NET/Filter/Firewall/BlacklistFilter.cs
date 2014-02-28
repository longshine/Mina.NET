using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using Common.Logging;
using Mina.Core.Filterchain;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Filter.Firewall
{
    /// <summary>
    /// A {@link IoFilter} which blocks connections from blacklisted remote address.
    /// </summary>
    public class BlacklistFilter : IoFilterAdapter
    {
        static readonly ILog log = LogManager.GetLogger(typeof(BlacklistFilter));

        private readonly List<Subnet> _blacklist = new List<Subnet>();

        /// <inheritdoc/>
        public override void SessionCreated(INextFilter nextFilter, IoSession session)
        {
            if (IsBlocked(session))
                BlockSession(session);
            else
                // forward if not blocked
                base.SessionCreated(nextFilter, session);
        }

        /// <inheritdoc/>
        public override void SessionOpened(INextFilter nextFilter, IoSession session)
        {
            if (IsBlocked(session))
                BlockSession(session);
            else
                // forward if not blocked
                base.SessionOpened(nextFilter, session);
        }

        /// <inheritdoc/>
        public override void SessionClosed(INextFilter nextFilter, IoSession session)
        {
            if (IsBlocked(session))
                BlockSession(session);
            else
                // forward if not blocked
                base.SessionClosed(nextFilter, session);
        }

        /// <inheritdoc/>
        public override void SessionIdle(INextFilter nextFilter, IoSession session, IdleStatus status)
        {
            if (IsBlocked(session))
                BlockSession(session);
            else
                // forward if not blocked
                base.SessionIdle(nextFilter, session, status);
        }

        /// <inheritdoc/>
        public override void MessageReceived(INextFilter nextFilter, IoSession session, Object message)
        {
            if (IsBlocked(session))
                BlockSession(session);
            else
                // forward if not blocked
                base.MessageReceived(nextFilter, session, message);
        }

        /// <inheritdoc/>
        public override void MessageSent(INextFilter nextFilter, IoSession session, IWriteRequest writeRequest)
        {
            if (IsBlocked(session))
                BlockSession(session);
            else
                // forward if not blocked
                base.MessageSent(nextFilter, session, writeRequest);
        }

        /// <summary>
        /// Sets the addresses to be blacklisted.
        /// </summary>
        public void SetBlacklist(IEnumerable<IPAddress> addresses)
        {
            if (addresses == null)
                throw new ArgumentNullException("addresses");
            lock (((IList)_blacklist).SyncRoot)
            {
                _blacklist.Clear();
                foreach (IPAddress addr in addresses)
                {
                    Block(addr);
                }
            }
        }

        /// <summary>
        /// Sets the subnets to be blacklisted.
        /// </summary>
        public void SetSubnetBlacklist(Subnet[] subnets)
        {
            if (subnets == null)
                throw new ArgumentNullException("subnets");
            lock (((IList)_blacklist).SyncRoot)
            {
                _blacklist.Clear();
                foreach (Subnet subnet in subnets)
                {
                    Block(subnet);
                }
            }
        }

        /// <summary>
        /// Blocks the specified endpoint.
        /// </summary>
        public void Block(IPAddress address)
        {
            if (address == null)
                throw new ArgumentNullException("address");
            Block(new Subnet(address, 32));
        }

        /// <summary>
        /// Blocks the specified subnet.
        /// </summary>
        public void Block(Subnet subnet)
        {
            if (subnet == null)
                throw new ArgumentNullException("subnet");
            lock (((IList)_blacklist).SyncRoot)
            {
                _blacklist.Add(subnet);
            }
        }

        /// <summary>
        /// Unblocks the specified endpoint.
        /// </summary>
        public void Unblock(IPAddress address)
        {
            if (address == null)
                throw new ArgumentNullException("address");
            Unblock(new Subnet(address, 32));
        }

        /// <summary>
        /// Unblocks the specified subnet.
        /// </summary>
        private void Unblock(Subnet subnet)
        {
            if (subnet == null)
                throw new ArgumentNullException("subnet");
            lock (((IList)_blacklist).SyncRoot)
            {
                _blacklist.Remove(subnet);
            }
        }

        private void BlockSession(IoSession session)
        {
            if (log.IsWarnEnabled)
                log.Warn("Remote address in the blacklist; closing.");
            session.Close(true);
        }

        private Boolean IsBlocked(IoSession session)
        {
            IPEndPoint ep = session.RemoteEndPoint as IPEndPoint;
            if (ep != null)
            {
                IPAddress address = ep.Address;

                // check all subnets
                lock (((IList)_blacklist).SyncRoot)
                {
                    foreach (Subnet subnet in _blacklist)
                    {
                        if (subnet.InSubnet(address))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
