using Mina.Core.Session;

namespace Mina.Filter.KeepAlive
{
    /// <summary>
    /// Tells <see cref="KeepAliveFilter"/> what to do when a keep-alive response message
    /// was not received within a certain timeout.
    /// </summary>
    public interface IKeepAliveRequestTimeoutHandler
    {
        /// <summary>
        /// Invoked when <see cref="KeepAliveFilter"/> couldn't receive the response for
        /// the sent keep alive message.
        /// </summary>
        void KeepAliveRequestTimedOut(KeepAliveFilter filter, IoSession session);
    }
}
