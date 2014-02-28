using System;
using System.Collections.Concurrent;
using Common.Logging;
using Mina.Core.Buffer;
using Mina.Core.Filterchain;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Filter.Buffer
{
    /// <summary>
    /// An <see cref="IoFilter"/> implementation used to buffer outgoing <see cref="IWriteRequest"/>.
    /// Using this filter allows to be less dependent from network latency.
    /// It is also useful when a session is generating very small messages
    /// too frequently and consequently generating unnecessary traffic overhead.
    /// <remarks>
    /// Please note that it should always be placed before the <see cref="Filter.Codec.ProtocolCodecFilter"/> 
    /// as it only handles <see cref="IWriteRequest"/>s carrying <see cref="IoBuffer"/> objects.
    /// </remarks>
    /// </summary>
    public class BufferedWriteFilter : IoFilterAdapter
    {
        /// <summary>
        /// Default buffer size value in bytes.
        /// </summary>
        public const Int32 DefaultBufferSize = 8192;

        static readonly ILog log = LogManager.GetLogger(typeof(BufferedWriteFilter));

        private Int32 _bufferSize;
        private ConcurrentDictionary<IoSession, Lazy<IoBuffer>> _buffersMap;

        public BufferedWriteFilter()
            : this(DefaultBufferSize, null)
        { }

        public BufferedWriteFilter(Int32 bufferSize)
            : this(bufferSize, null)
        { }

#if NET20
        internal
#else
        public
#endif
        BufferedWriteFilter(Int32 bufferSize, ConcurrentDictionary<IoSession, Lazy<IoBuffer>> buffersMap)
        {
            _bufferSize = bufferSize;
            _buffersMap = buffersMap == null ?
                new ConcurrentDictionary<IoSession, Lazy<IoBuffer>>() : buffersMap;
        }

        /// <summary>
        /// Gets or sets the buffer size (only for the newly created buffers).
        /// </summary>
        public Int32 BufferSize
        {
            get { return _bufferSize; }
            set { _bufferSize = value; }
        }

        /// <inheritdoc/>
        public override void FilterWrite(INextFilter nextFilter, IoSession session, IWriteRequest writeRequest)
        {
            IoBuffer buf = writeRequest.Message as IoBuffer;
            if (buf == null)
                throw new ArgumentException("This filter should only buffer IoBuffer objects");
            else
                Write(session, buf);
        }

        /// <inheritdoc/>
        public override void SessionClosed(INextFilter nextFilter, IoSession session)
        {
            Free(session);
            base.SessionClosed(nextFilter, session);
        }

        /// <inheritdoc/>
        public override void ExceptionCaught(INextFilter nextFilter, IoSession session, Exception cause)
        {
            Free(session);
            base.ExceptionCaught(nextFilter, session, cause);
        }

        public void Flush(IoSession session)
        {
            Lazy<IoBuffer> lazy;
            _buffersMap.TryGetValue(session, out lazy);
            try
            {
                InternalFlush(session.FilterChain.GetNextFilter(this), session, lazy.Value);
            }
            catch (Exception e)
            {
                session.FilterChain.FireExceptionCaught(e);
            }
        }

        private void Write(IoSession session, IoBuffer data)
        {
            Lazy<IoBuffer> dest = _buffersMap.GetOrAdd(session,
                new Lazy<IoBuffer>(() => IoBuffer.Allocate(_bufferSize)));
            Write(session, data, dest.Value);
        }

        private void Write(IoSession session, IoBuffer data, IoBuffer buf)
        {
            try
            {
                Int32 len = data.Remaining;
                if (len >= buf.Capacity)
                {
                    /*
                     * If the request length exceeds the size of the output buffer,
                     * flush the output buffer and then write the data directly.
                     */
                    INextFilter nextFilter = session.FilterChain.GetNextFilter(this);
                    InternalFlush(nextFilter, session, buf);
                    nextFilter.FilterWrite(session, new DefaultWriteRequest(data));
                    return;
                }
                if (len > (buf.Limit - buf.Position))
                {
                    InternalFlush(session.FilterChain.GetNextFilter(this), session, buf);
                }

                lock (buf)
                {
                    buf.Put(data);
                }
            }
            catch (Exception e)
            {
                session.FilterChain.FireExceptionCaught(e);
            }
        }

        private void InternalFlush(INextFilter nextFilter, IoSession session, IoBuffer buf)
        {
            IoBuffer tmp = null;
            lock (buf)
            {
                buf.Flip();
                tmp = buf.Duplicate();
                buf.Clear();
            }
            if (log.IsDebugEnabled)
                log.Debug("Flushing buffer: " + tmp);
            nextFilter.FilterWrite(session, new DefaultWriteRequest(tmp));
        }

        private void Free(IoSession session)
        {
            Lazy<IoBuffer> lazy;
            if (_buffersMap.TryRemove(session, out lazy))
            {
                lazy.Value.Free();
            }
        }
    }
}
