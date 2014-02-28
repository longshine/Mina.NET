using System;
using Mina.Core.Service;
using Mina.Core.Session;

namespace Mina.Handler.Chain
{
    /// <summary>
    /// An <see cref="IoHandler"/> which executes an <see cref="IoHandlerChain"/>
    /// on a <tt>messageReceived</tt> event.
    /// </summary>
    public class ChainedIoHandler : IoHandlerAdapter
    {
        private readonly IoHandlerChain _chain;

        /// <summary>
        /// </summary>
        public ChainedIoHandler()
            : this(new IoHandlerChain())
        { }

        /// <summary>
        /// </summary>
        public ChainedIoHandler(IoHandlerChain chain)
        {
            if (chain == null)
                throw new ArgumentNullException("chain");
            _chain = chain;
        }

        /// <summary>
        /// Gets the associated <see cref="IoHandlerChain"/>.
        /// </summary>
        public IoHandlerChain Chain
        {
            get { return _chain; }
        }

        /// <inheritdoc/>
        public override void MessageReceived(IoSession session, Object message)
        {
            _chain.Execute(null, session, message);
        }
    }
}
