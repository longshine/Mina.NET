using System;
using Mina.Core.Session;

namespace Mina.Handler.Demux
{
    /// <summary>
    /// Default implementation of <see cref="IMessageHandler"/>.
    /// </summary>
    public class MessageHandler<T> : IMessageHandler<T>
    {
        public static readonly IMessageHandler<Object> Noop = new NoopMessageHandler();

        private readonly Action<IoSession, T> _act;

        /// <summary>
        /// </summary>
        public MessageHandler()
        { }

        /// <summary>
        /// </summary>
        public MessageHandler(Action<IoSession, T> act)
        {
            if (act == null)
                throw new ArgumentNullException("act");
            _act = act;
        }

        /// <inheritdoc/>
        public virtual void HandleMessage(IoSession session, T message)
        {
            if (_act != null)
                _act(session, message);
        }

        void IMessageHandler.HandleMessage(IoSession session, Object message)
        {
            HandleMessage(session, (T)message);
        }
    }

    class NoopMessageHandler : IMessageHandler<Object>
    {
        internal NoopMessageHandler() { }

        public void HandleMessage(IoSession session, Object message)
        {
            // Do nothing
        }
    }
}
