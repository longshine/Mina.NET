using System;
using System.Text;
using Mina.Core.Filterchain;
using Mina.Core.Session;

namespace Mina.Handler.Chain
{
    /// <summary>
    /// A chain of <see cref="IoHandlerCommand"/>s.
    /// </summary>
    public class IoHandlerChain : Chain<IoHandlerChain, IoHandlerCommand, INextCommand>, IoHandlerCommand
    {
        private static volatile Int32 nextId;

        private readonly Int32 _id = nextId++;
        private readonly String _nextCommandKey;

        /// <summary>
        /// </summary>
        public IoHandlerChain()
            : base(
            e => new NextCommand(e),
            () => new HeadCommand(),
            () => new TailCommand(typeof(IoHandlerChain).Name + "." + Guid.NewGuid() + ".nextCommand")
            )
        {
            _nextCommandKey = ((TailCommand)Tail.Filter).nextCommandKey;
        }

        /// <inheritdoc/>
        public void Execute(INextCommand next, IoSession session, Object message)
        {
            if (next != null)
                session.SetAttribute(_nextCommandKey, next);

            try
            {
                CallNextCommand(Head, session, message);
            }
            finally
            {
                session.RemoveAttribute(_nextCommandKey);
            }
        }

        /// <inheritdoc/>
        public override String ToString()
        {
            StringBuilder buf = new StringBuilder();
            buf.Append("{ ");

            Boolean empty = true;
            Entry e = Head.NextEntry;
            while (e != Tail)
            {
                if (empty)
                    empty = false;
                else
                    buf.Append(", ");

                buf.Append('(');
                buf.Append(e.Name);
                buf.Append(':');
                buf.Append(e.Filter);
                buf.Append(')');

                e = e.NextEntry;
            }

            if (empty)
                buf.Append("empty");

            return buf.Append(" }").ToString();
        }

        private static void CallNextCommand(IEntry<IoHandlerCommand, INextCommand> entry, IoSession session, Object message)
        {
            entry.Filter.Execute(entry.NextFilter, session, message);
        }

        class HeadCommand : IoHandlerCommand
        {
            public void Execute(INextCommand next, IoSession session, Object message)
            {
                next.Execute(session, message);
            }
        }

        class TailCommand : IoHandlerCommand
        {
            public readonly String nextCommandKey;

            public TailCommand(String nextCommandKey)
            {
                this.nextCommandKey = nextCommandKey;
            }

            public void Execute(INextCommand next, IoSession session, Object message)
            {
                next = session.GetAttribute<INextCommand>(nextCommandKey);
                if (next != null)
                    next.Execute(session, message);
            }
        }

        class NextCommand : INextCommand
        {
            readonly Entry _entry;

            public NextCommand(Entry entry)
            {
                _entry = entry;
            }

            public void Execute(IoSession session, Object message)
            {
                CallNextCommand(_entry.NextEntry, session, message);
            }
        }
    }
}
