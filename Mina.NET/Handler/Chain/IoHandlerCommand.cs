using System;
using Mina.Core.Session;

namespace Mina.Handler.Chain
{
    /// <summary>
    /// A <see cref="IoHandlerCommand"/> encapsulates a unit of processing work to be
    /// performed, whose purpose is to examine and/or modify the state of a
    /// transaction that is represented by custom attributes provided by
    /// <see cref="IoSession"/>.
    /// </summary>
    public interface IoHandlerCommand
    {
        void Execute(INextCommand next, IoSession session, Object message);
    }
}
