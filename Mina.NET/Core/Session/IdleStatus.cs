namespace Mina.Core.Session
{
    /// <summary>
    /// Represents the type of idleness of <see cref="IoSession"/>.
    /// </summary>
    public enum IdleStatus
    {
        /// <summary>
        /// Represents the session status that no data is coming from the remote peer.
        /// </summary>
        ReaderIdle,
        /// <summary>
        /// Represents the session status that the session is not writing any data.
        /// </summary>
        WriterIdle,
        /// <summary>
        /// Represents both ReaderIdle and WriterIdle.
        /// </summary>
        BothIdle
    }

    /// <summary>
    /// Provides data for idle events.
    /// </summary>
    public class IdleEventArgs : System.EventArgs
    {
        private readonly IdleStatus _idleStatus;

        /// <summary>
        /// </summary>
        public IdleEventArgs(IdleStatus idleStatus)
        {
            _idleStatus = idleStatus;
        }

        /// <summary>
        /// Gets the idle status.
        /// </summary>
        public IdleStatus IdleStatus
        {
            get { return _idleStatus; }
        }
    }
}
