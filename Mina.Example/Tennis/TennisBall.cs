namespace Mina.Example.Tennis
{
    class TennisBall
    {
        private readonly bool _ping;
        private readonly int _ttl;

        /// <summary>
        /// Creates a new ball with the specified TTL (Time To Live) value.
        /// </summary>
        public TennisBall(int ttl)
            : this(ttl, true)
        { }

        /// <summary>
        /// Creates a new ball with the specified TTL value and PING/PONG state.
        /// </summary>
        private TennisBall(int ttl, bool ping)
        {
            _ttl = ttl;
            _ping = ping;
        }

        /// <summary>
        /// Gets the TTL value of this ball.
        /// </summary>
        public int TTL
        {
            get { return _ttl; }
        }

        /// <summary>
        /// Returns the ball after <see cref="TennisPlayer"/>'s stroke.
        /// The returned ball has decreased TTL value and switched PING/PONG state.
        /// </summary>
        public TennisBall Stroke()
        {
            return new TennisBall(_ttl - 1, !_ping);
        }

        public override string ToString()
        {
            if (_ping)
                return "PING (" + _ttl + ")";
            else
                return "PONG (" + _ttl + ")";
        }
    }
}
