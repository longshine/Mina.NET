using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Mina.Core.Buffer;
using Mina.Core.Filterchain;
using Mina.Core.Future;
using Mina.Core.Service;
using Mina.Core.Write;

namespace Mina.Core.Session
{
    /// <summary>
    /// Base implementation of <see cref="IoSession"/>.
    /// </summary>
    public abstract class AbstractIoSession : IoSession
    {
        private static readonly IWriteRequest CLOSE_REQUEST = new DefaultWriteRequest(new Object());
        private static Int64 idGenerator = 0;
        private Int64 _id;
        private Object _syncRoot = new Byte[0];
        protected IoSessionConfig _config;
        private readonly IoService _service;
        private readonly IoHandler _handler;
        private IoSessionAttributeMap _attributes;
        private IWriteRequestQueue _writeRequestQueue;
        private IWriteRequest _currentWriteRequest;
        private readonly DateTime _creationTime;
        private volatile Boolean _closing;
        private readonly ICloseFuture _closeFuture;

        public AbstractIoSession(IoService service)
        {
            _service = service;
            _handler = service.Handler;

            _creationTime = DateTime.Now;
            _lastThroughputCalculationTime = _creationTime;
            _lastReadTime = _lastWriteTime = _creationTime;

            _id = Interlocked.Increment(ref idGenerator);

            _closeFuture = new DefaultCloseFuture(this);
            _closeFuture.Complete += new Action<IoFuture>(ResetCounter);
        }

        public Int64 Id
        {
            get { return _id; }
        }

        public IoSessionConfig Config
        {
            get { return _config; }
        }

        public IoService Service
        {
            get { return _service; }
        }

        public abstract IoProcessor Processor { get; }

        public virtual IoHandler Handler
        {
            get { return _handler; }
        }

        public abstract IoFilterChain FilterChain { get; }

        public Boolean Connected
        {
            get { return !_closeFuture.Closed; }
        }

        public Boolean Closing
        {
            get { return _closing || _closeFuture.Closed; }
        }

        public ICloseFuture CloseFuture
        {
            get { return _closeFuture; }
        }

        public abstract EndPoint LocalEndPoint { get; }

        public abstract EndPoint RemoteEndPoint { get; }

        public IoSessionAttributeMap AttributeMap
        {
            get { return _attributes; }
            set { _attributes = value; }
        }

        public IWriteRequestQueue WriteRequestQueue
        {
            get
            {
                if (_writeRequestQueue == null)
                    throw new InvalidOperationException();
                return _writeRequestQueue;
            }
        }

        public IWriteRequest CurrentWriteRequest
        {
            get { return _currentWriteRequest; }
            set { _currentWriteRequest = value; }
        }

        public void SetWriteRequestQueue(IWriteRequestQueue queue)
        {
            _writeRequestQueue = new CloseAwareWriteQueue(this, queue);
        }

        public IWriteFuture Write(Object message)
        {
            if (message == null)
                return null;

            // If the session has been closed or is closing, we can't either
            // send a message to the remote side. We generate a future
            // containing an exception.
            if (Closing || !Connected)
            {
                IWriteFuture future = new DefaultWriteFuture(this);
                IWriteRequest request = new DefaultWriteRequest(message, future);
                future.Exception = new WriteToClosedSessionException(request);
                return future;
            }

            // Now, we can write the message. First, create a future
            IWriteFuture writeFuture = new DefaultWriteFuture(this);
            IWriteRequest writeRequest = new DefaultWriteRequest(message, writeFuture);

            // Then, get the chain and inject the WriteRequest into it
            IoFilterChain filterChain = this.FilterChain;
            filterChain.FireFilterWrite(writeRequest);

            return writeFuture;
        }

        public ICloseFuture Close(Boolean rightNow)
        {
            if (Closing)
            {
                return _closeFuture;
            }
            else if (rightNow)
            {
                return Close();
            }
            else
            {
                return CloseOnFlush();
            }
        }

        public ICloseFuture Close()
        {
            lock (_syncRoot)
            {
                if (Closing)
                    return _closeFuture;
                _closing = true;
            }
            this.FilterChain.FireFilterClose();
            return _closeFuture;
        }

        private ICloseFuture CloseOnFlush()
        {
            WriteRequestQueue.Offer(this, CLOSE_REQUEST);
            Processor.Flush(this);
            return _closeFuture;
        }

        public Object GetAttribute(Object key)
        {
            return GetAttribute(key, null);
        }

        public Object GetAttribute(Object key, Object defaultValue)
        {
            return _attributes.GetAttribute(this, key, defaultValue);
        }

        public T GetAttribute<T>(Object key)
        {
            return GetAttribute<T>(key, default(T));
        }

        public T GetAttribute<T>(Object key, T defaultValue)
        {
            return (T)_attributes.GetAttribute(this, key, defaultValue);
        }

        public Object SetAttribute(Object key, Object value)
        {
            return _attributes.SetAttribute(this, key, value);
        }

        public Object SetAttribute(Object key)
        {
            return SetAttribute(key, true);
        }

        public Object SetAttributeIfAbsent(Object key, Object value)
        {
            return _attributes.SetAttributeIfAbsent(this, key, value);
        }

        public Object SetAttributeIfAbsent(Object key)
        {
            return SetAttributeIfAbsent(key, true);
        }

        public Object RemoveAttribute(Object key)
        {
            return _attributes.RemoveAttribute(this, key);
        }

        public Boolean ContainsAttribute(Object key)
        {
            return _attributes.ContainsAttribute(this, key);
        }

        #region Traffic control

        private Boolean _readSuspended = false;
        private Boolean _writeSuspended = false;

        public Boolean ReadSuspended
        {
            get { return _readSuspended; }
        }

        public Boolean WriteSuspended
        {
            get { return _writeSuspended; }
        }

        public void SuspendRead()
        {
            _readSuspended = true;
            // TODO getProcessor().updateTrafficControl(this);
        }

        public void SuspendWrite()
        {
            _writeSuspended = true;
        }

        public void ResumeRead()
        {
            _readSuspended = false;
        }

        public void ResumeWrite()
        {
            _writeSuspended = false;
        }

        #endregion

        #region Status variables

        private Int32 _scheduledWriteBytes;
        private Int32 _scheduledWriteMessages;
        private Int64 _readBytes;
        private Int64 _writtenBytes;
        private Int64 _readMessages;
        private Int64 _writtenMessages;
        private DateTime _lastReadTime;
        private DateTime _lastWriteTime;
        private DateTime _lastThroughputCalculationTime;
        private Int64 _lastReadBytes;
        private Int64 _lastWrittenBytes;
        private Int64 _lastReadMessages;
        private Int64 _lastWrittenMessages;
        private Double _readBytesThroughput;
        private Double _writtenBytesThroughput;
        private Double _readMessagesThroughput;
        private Double _writtenMessagesThroughput;
        private Int32 _idleCountForBoth;
        private Int32 _idleCountForRead;
        private Int32 _idleCountForWrite;
        private DateTime _lastIdleTimeForBoth;
        private DateTime _lastIdleTimeForRead;
        private DateTime _lastIdleTimeForWrite;

        public Int64 ReadBytes
        {
            get { return _readBytes; }
        }

        public Int64 WrittenBytes
        {
            get { return _writtenBytes; }
        }

        public Int64 ReadMessages
        {
            get { return _readMessages; }
        }

        public Int64 WrittenMessages
        {
            get { return _writtenMessages; }
        }

        public Double ReadBytesThroughput
        {
            get { return _readBytesThroughput; }
        }

        public Double WrittenBytesThroughput
        {
            get { return _writtenBytesThroughput; }
        }

        public Double ReadMessagesThroughput
        {
            get { return _readMessagesThroughput; }
        }

        public Double WrittenMessagesThroughput
        {
            get { return _writtenMessagesThroughput; }
        }

        public DateTime CreationTime
        {
            get { return _creationTime; }
        }

        public DateTime LastIoTime
        {
            get { return _lastReadTime > _lastWriteTime ? _lastReadTime : _lastWriteTime; }
        }

        public DateTime LastReadTime
        {
            get { return _lastReadTime; }
        }

        public DateTime LastWriteTime
        {
            get { return _lastWriteTime; }
        }

        public Boolean IsIdle(IdleStatus status)
        {
            switch (status)
            {
                case IdleStatus.BothIdle:
                    return _idleCountForBoth > 0;
                case IdleStatus.ReaderIdle:
                    return _idleCountForRead > 0;
                case IdleStatus.WriterIdle:
                    return _idleCountForWrite > 0;
                default:
                    throw new ArgumentException("status");
            }
        }

        public Boolean IsReaderIdle
        {
            get { return IsIdle(IdleStatus.ReaderIdle); }
        }

        public Boolean IsWriterIdle
        {
            get { return IsIdle(IdleStatus.WriterIdle); }
        }

        public Boolean IsBothIdle
        {
            get { return IsIdle(IdleStatus.BothIdle); }
        }

        public Int32 GetIdleCount(IdleStatus status)
        {
            if (Config.GetIdleTime(status) == 0)
            {
                switch (status)
                {
                    case IdleStatus.BothIdle:
                        Interlocked.Exchange(ref _idleCountForBoth, 0);
                        break;
                    case IdleStatus.ReaderIdle:
                        Interlocked.Exchange(ref _idleCountForRead, 0);
                        break;
                    case IdleStatus.WriterIdle:
                        Interlocked.Exchange(ref _idleCountForWrite, 0);
                        break;
                }
            }

            switch (status)
            {
                case IdleStatus.BothIdle:
                    return _idleCountForBoth;
                case IdleStatus.ReaderIdle:
                    return _idleCountForRead;
                case IdleStatus.WriterIdle:
                    return _idleCountForWrite;
                default:
                    throw new ArgumentException("status");
            }
        }

        public Int32 BothIdleCount
        {
            get { return GetIdleCount(IdleStatus.BothIdle); }
        }

        public Int32 ReaderIdleCount
        {
            get { return GetIdleCount(IdleStatus.ReaderIdle); }
        }

        public Int32 WriterIdleCount
        {
            get { return GetIdleCount(IdleStatus.WriterIdle); }
        }

        public DateTime GetLastIdleTime(IdleStatus status)
        {
            switch (status)
            {
                case IdleStatus.BothIdle:
                    return _lastIdleTimeForBoth;
                case IdleStatus.ReaderIdle:
                    return _lastIdleTimeForRead;
                case IdleStatus.WriterIdle:
                    return _lastIdleTimeForWrite;
                default:
                    throw new ArgumentException("status");
            }
        }

        public DateTime LastBothIdleTime
        {
            get { return GetLastIdleTime(IdleStatus.BothIdle); }
        }

        public DateTime LastReaderIdleTime
        {
            get { return GetLastIdleTime(IdleStatus.ReaderIdle); }
        }

        public DateTime LastWriterIdleTime
        {
            get { return GetLastIdleTime(IdleStatus.WriterIdle); }
        }

        public void IncreaseIdleCount(IdleStatus status, DateTime currentTime)
        {
            switch (status)
            {
                case IdleStatus.BothIdle:
                    Interlocked.Increment(ref _idleCountForBoth);
                    _lastIdleTimeForBoth = currentTime;
                    break;
                case IdleStatus.ReaderIdle:
                    Interlocked.Increment(ref _idleCountForRead);
                    _lastIdleTimeForRead = currentTime;
                    break;
                case IdleStatus.WriterIdle:
                    Interlocked.Increment(ref _idleCountForWrite);
                    _lastIdleTimeForWrite = currentTime;
                    break;
                default:
                    throw new ArgumentException("status");
            }
        }

        public void IncreaseReadBytes(Int64 increment, DateTime currentTime)
        {
            if (increment <= 0)
                return;

            _readBytes += increment;
            _lastReadTime = currentTime;
            Interlocked.Exchange(ref _idleCountForBoth, 0);
            Interlocked.Exchange(ref _idleCountForRead, 0);

            this.Service.Statistics.IncreaseReadBytes(increment, currentTime);
        }

        public void IncreaseReadMessages(DateTime currentTime)
        {
            _readMessages++;
            _lastReadTime = currentTime;
            Interlocked.Exchange(ref _idleCountForBoth, 0);
            Interlocked.Exchange(ref _idleCountForRead, 0);

            this.Service.Statistics.IncreaseReadMessages(currentTime);
        }

        public void IncreaseWrittenBytes(Int32 increment, DateTime currentTime)
        {
            if (increment <= 0)
                return;

            _writtenBytes += increment;
            _lastWriteTime = currentTime;
            Interlocked.Exchange(ref _idleCountForBoth, 0);
            Interlocked.Exchange(ref _idleCountForWrite, 0);

            this.Service.Statistics.IncreaseWrittenBytes(increment, currentTime);
            IncreaseScheduledWriteBytes(-increment);
        }

        public void IncreaseWrittenMessages(IWriteRequest request, DateTime currentTime)
        {
            IoBuffer buf = request.Message as IoBuffer;
            if (buf != null && buf.HasRemaining)
                return;

            _writtenMessages++;
            _lastWriteTime = currentTime;

            this.Service.Statistics.IncreaseWrittenMessages(currentTime);
            DecreaseScheduledWriteMessages();
        }

        public void IncreaseScheduledWriteBytes(Int32 increment)
        {
            Interlocked.Add(ref _scheduledWriteBytes, increment);
            this.Service.Statistics.IncreaseScheduledWriteBytes(increment);
        }

        public void IncreaseScheduledWriteMessages()
        {
            Interlocked.Increment(ref _scheduledWriteMessages);
            this.Service.Statistics.IncreaseScheduledWriteMessages();
        }

        public void DecreaseScheduledWriteMessages()
        {
            Interlocked.Decrement(ref _scheduledWriteMessages);
            this.Service.Statistics.DecreaseScheduledWriteMessages();
        }

        public void UpdateThroughput(DateTime currentTime, Boolean force)
        {
            UInt64 interval = (UInt64)(currentTime - _lastThroughputCalculationTime).TotalMilliseconds;

            UInt64 minInterval = Config.ThroughputCalculationIntervalInMillis;
            if ((minInterval == 0) || (interval < minInterval))
            {
                if (!force)
                {
                    return;
                }
            }

            _readBytesThroughput = (_readBytes - _lastReadBytes) * 1000.0 / interval;
            _writtenBytesThroughput = (_writtenBytes - _lastWrittenBytes) * 1000.0 / interval;
            _readMessagesThroughput = (_readMessages - _lastReadMessages) * 1000.0 / interval;
            _writtenMessagesThroughput = (_writtenMessages - _lastWrittenMessages) * 1000.0 / interval;

            _lastReadBytes = _readBytes;
            _lastWrittenBytes = _writtenBytes;
            _lastReadMessages = _readMessages;
            _lastWrittenMessages = _writtenMessages;

            _lastThroughputCalculationTime = currentTime;
        }

        private static void ResetCounter(IoFuture future)
        {
            AbstractIoSession session = (AbstractIoSession)future.Session;
            Interlocked.Exchange(ref session._scheduledWriteBytes, 0);
            Interlocked.Exchange(ref session._scheduledWriteMessages, 0);
            session._readBytesThroughput = 0;
            session._readMessagesThroughput = 0;
            session._writtenBytesThroughput = 0;
            session._writtenMessagesThroughput = 0;
        }

        #endregion

        /// <summary>
        /// Fires a {@link IoEventType#SESSION_IDLE} event to any applicable sessions in the specified collection.
        /// </summary>
        /// <param name="sessions"></param>
        /// <param name="currentTime"></param>
        public static void NotifyIdleness(IEnumerable<IoSession> sessions, DateTime currentTime)
        {
            foreach (IoSession s in sessions)
            {
                NotifyIdleSession(s, currentTime);
            }
        }

        public static void NotifyIdleSession(IoSession session, DateTime currentTime)
        {
            NotifyIdleSession(session, currentTime, IdleStatus.BothIdle, session.LastIoTime);
            NotifyIdleSession(session, currentTime, IdleStatus.ReaderIdle, session.LastReadTime);
            NotifyIdleSession(session, currentTime, IdleStatus.WriterIdle, session.LastWriteTime);
            NotifyWriteTimeout(session, currentTime);
        }

        private static void NotifyIdleSession(IoSession session, DateTime currentTime, IdleStatus status, DateTime lastIoTime)
        {
            UInt64 idleTime = session.Config.GetIdleTimeInMillis(status);
            DateTime lastIdleTime = session.GetLastIdleTime(status);
            if (lastIoTime < lastIdleTime)
                lastIoTime = lastIdleTime;

            if ((idleTime > 0) && ((currentTime - lastIoTime).TotalMilliseconds >= idleTime))
                session.FilterChain.FireSessionIdle(status);
        }

        private static void NotifyWriteTimeout(IoSession session, DateTime currentTime)
        {
            UInt64 writeTimeout = session.Config.WriteTimeoutInMillis;
            if ((writeTimeout > 0) && ((currentTime - session.LastWriteTime).TotalMilliseconds >= writeTimeout)
                    && !session.WriteRequestQueue.IsEmpty(session))
            {
                IWriteRequest request = session.CurrentWriteRequest;
                if (request != null)
                {
                    session.CurrentWriteRequest = null;
                    WriteTimeoutException cause = new WriteTimeoutException(request);
                    request.Future.Exception = cause;
                    session.FilterChain.FireExceptionCaught(cause);
                    // WriteException is an IOException, so we close the session.
                    session.Close(true);
                }
            }
        }

        /// <summary>
        /// A queue which handles the CLOSE request.
        /// </summary>
        class CloseAwareWriteQueue : IWriteRequestQueue
        {
            private readonly AbstractIoSession _session;
            private readonly IWriteRequestQueue _queue;

            public CloseAwareWriteQueue(AbstractIoSession session, IWriteRequestQueue queue)
            {
                _session = session;
                _queue = queue;
            }

            public Int32 Size
            {
                get { return _queue.Size; }
            }

            public IWriteRequest Poll(IoSession session)
            {
                IWriteRequest answer = _queue.Poll(session);
                if (Object.ReferenceEquals(answer, CLOSE_REQUEST))
                {
                    _session.Close();
                    Dispose(_session);
                    answer = null;
                }
                return answer;
            }

            public void Offer(IoSession session, IWriteRequest writeRequest)
            {
                _queue.Offer(session, writeRequest);
            }

            public Boolean IsEmpty(IoSession session)
            {
                return _queue.IsEmpty(session);
            }

            public void Clear(IoSession session)
            {
                _queue.Clear(session);
            }

            public void Dispose(IoSession session)
            {
                _queue.Dispose(session);
            }
        }
    }
}
