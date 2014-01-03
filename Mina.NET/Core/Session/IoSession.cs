using System;
using System.Net;
using Mina.Core.Filterchain;
using Mina.Core.Future;
using Mina.Core.Service;
using Mina.Core.Write;

namespace Mina.Core.Session
{
    /// <summary>
    /// A handle which represents connection between two end-points regardless of transport types.
    /// </summary>
    public interface IoSession
    {
        /// <summary>
        /// Gets a unique identifier for this session.
        /// </summary>
        Int64 Id { get; }
        /// <summary>
        /// Gets the configuration of this session.
        /// </summary>
        IoSessionConfig Config { get; }
        /// <summary>
        /// Gets the <see cref="IoService"/> which provides I/O service to this session.
        /// </summary>
        IoService Service { get; }
        /// <summary>
        /// Gets the associated <see cref="IoProcessor"/> for this session.
        /// </summary>
        IoProcessor Processor { get; }
        /// <summary>
        /// Gets the <see cref="IoHandler"/> which handles this session.
        /// </summary>
        IoHandler Handler { get; }
        /// <summary>
        /// Gets the filter chain that only affects this session.
        /// </summary>
        IoFilterChain FilterChain { get; }
        IWriteRequestQueue WriteRequestQueue { get; }
        /// <summary>
        /// Returns <code>true</code> if this session is connected with remote peer.
        /// </summary>
        Boolean Connected { get; }
        EndPoint LocalEndPoint { get; }
        EndPoint RemoteEndPoint { get; }
        /// <summary>
        /// Gets the <see cref="ICloseFuture"/> of this session.
        /// This method returns the same instance whenever user calls it.
        /// </summary>
        ICloseFuture CloseFuture { get; }
        IWriteFuture Write(Object message);
        ICloseFuture Close(Boolean rightNow);
        T GetAttribute<T>(Object key);
        Object GetAttribute(Object key);
        Object SetAttribute(Object key, Object value);
        Object SetAttribute(Object key);
        Object SetAttributeIfAbsent(Object key, Object value);
        Object RemoveAttribute(Object key);
        Boolean ContainsAttribute(Object key);

        Boolean WriteSuspended { get; }
        Boolean ReadSuspended { get; }
        void SuspendRead();
        void SuspendWrite();
        void ResumeRead();
        void ResumeWrite();

        /// <summary>
        /// Gets or sets the <see cref="IWriteRequest"/> which is being processed by <see cref="IoService"/>.
        /// </summary>
        IWriteRequest CurrentWriteRequest { get; set; }

        Int64 ReadBytes { get; }
        Int64 WrittenBytes { get; }
        Int64 ReadMessages { get; }
        Int64 WrittenMessages { get; }
        Double ReadBytesThroughput { get; }
        Double WrittenBytesThroughput { get; }
        Double ReadMessagesThroughput { get; }
        Double WrittenMessagesThroughput { get; }
        DateTime CreationTime { get; }
        DateTime LastIoTime { get; }
        DateTime LastReadTime { get; }
        DateTime LastWriteTime { get; }
        Boolean IsIdle(IdleStatus status);
        Boolean IsReaderIdle { get; }
        Boolean IsWriterIdle { get; }
        Boolean IsBothIdle { get; }
        Int32 GetIdleCount(IdleStatus status);
        DateTime GetLastIdleTime(IdleStatus status);
    }
}
