using System;
using Mina.Core.Session;
using Mina.Core.Write;
using System.Collections.Generic;

namespace Mina.Core.Filterchain
{
    /// <summary>
    /// A container of <see cref="IoFilter"/>s that forwards <see cref="IoHandler"/> events
    /// to the consisting filters and terminal <see cref="IoHandler"/> sequentially.
    /// Every <see cref="IoSession"/> has its own <see cref="IoFilterChain"/> (1-to-1 relationship).
    /// </summary>
    public interface IoFilterChain
    {
        /// <summary>
        /// Gets the parent <see cref="IoSession"/> of this chain.
        /// </summary>
        IoSession Session { get; }

        void FireSessionCreated();
        void FireSessionOpened();
        void FireSessionClosed();
        void FireSessionIdle(IdleStatus status);
        void FireMessageReceived(Object message);
        void FireMessageSent(IWriteRequest request);
        void FireExceptionCaught(Exception ex);
        void FireFilterWrite(IWriteRequest writeRequest);
        void FireFilterClose();

        IEntry GetEntry(String name);
        IEntry GetEntry(IoFilter filter);
        IoFilter Get(String name);
        INextFilter GetNextFilter(IoFilter filter);
        IEnumerable<IEntry> GetAll();
        Boolean Contains(String name);
        Boolean Contains(IoFilter filter);
        void AddFirst(String name, IoFilter filter);
        void AddLast(String name, IoFilter filter);
        void AddBefore(String baseName, String name, IoFilter filter);
        void AddAfter(String baseName, String name, IoFilter filter);
        IoFilter Replace(String name, IoFilter newFilter);
        void Replace(IoFilter oldFilter, IoFilter newFilter);
        IoFilter Remove(String name);
        void Remove(IoFilter filter);
        void Clear();
    }
}
