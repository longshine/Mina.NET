using System;
using Mina.Core.Session;

namespace Mina.Handler.Chain
{
    /// <summary>
    /// Represents an indirect reference to the next <see cref="IoHandlerCommand"/> of
    /// the <see cref="IoHandlerChain"/>.
    /// </summary>
    public interface INextCommand
    {
        void Execute(IoSession session, Object message);
    }
}
