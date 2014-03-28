using System;
using System.Collections.Concurrent;
using Mina.Core.Buffer;
using Mina.Core.Filterchain;
using Mina.Core.Session;
using Mina.Core.Write;

namespace Mina.Filter.Stream
{
    public abstract class AbstractStreamWriteFilter<T> : IoFilterAdapter
        where T : class
    {
        /// <summary>
        /// The default buffer size this filter uses for writing.
        /// </summary>
        public const Int32 DefaultStreamBufferSize = 4096;

        private Int32 _writeBufferSize = DefaultStreamBufferSize;
        protected readonly AttributeKey CURRENT_STREAM;
        protected readonly AttributeKey WRITE_REQUEST_QUEUE;
        protected readonly AttributeKey CURRENT_WRITE_REQUEST;

        protected AbstractStreamWriteFilter()
        { 
            CURRENT_STREAM = new AttributeKey(GetType(), "stream");
            WRITE_REQUEST_QUEUE = new AttributeKey(GetType(), "queue");
            CURRENT_WRITE_REQUEST = new AttributeKey(GetType(), "writeRequest");
        }

        /// <summary>
        /// Gets or sets the size of the write buffer in bytes. Data will be read from the
        /// stream in chunks of this size and then written to the next filter.
        /// </summary>
        public Int32 WriteBufferSize
        {
            get { return _writeBufferSize; }
            set
            {
                if (value < 1)
                    throw new ArgumentException("WriteBufferSize must be at least 1");
                _writeBufferSize = value;
            }
        }

        /// <inheritdoc/>
        public override void OnPreAdd(IoFilterChain parent, String name, INextFilter nextFilter)
        {
            if (parent.Contains(GetType()))
                throw new InvalidOperationException("Only one " + GetType().Name + " is permitted.");
        }

        /// <inheritdoc/>
        public override void FilterWrite(INextFilter nextFilter, IoSession session, IWriteRequest writeRequest)
        {
            // If we're already processing a stream we need to queue the WriteRequest.
            if (session.GetAttribute(CURRENT_STREAM) != null)
            {
                ConcurrentQueue<IWriteRequest> queue = GetWriteRequestQueue(session);
                queue.Enqueue(writeRequest);
                return;
            }

            T stream = writeRequest.Message as T;

            if (stream == null)
            {
                base.FilterWrite(nextFilter, session, writeRequest);
            }
            else
            {
                IoBuffer buffer = GetNextBuffer(stream);
                if (buffer == null)
                {
                    // EOF
                    writeRequest.Future.Written = true;
                    nextFilter.MessageSent(session, writeRequest);
                }
                else
                {
                    session.SetAttribute(CURRENT_STREAM, stream);
                    session.SetAttribute(CURRENT_WRITE_REQUEST, writeRequest);

                    nextFilter.FilterWrite(session, new DefaultWriteRequest(buffer));
                }
            }
        }

        /// <inheritdoc/>
        public override void MessageSent(INextFilter nextFilter, IoSession session, IWriteRequest writeRequest)
        {
            T stream = session.GetAttribute(CURRENT_STREAM) as T;

            if (stream == null)
            {
                base.MessageSent(nextFilter, session, writeRequest);
            }
            else
            {
                IoBuffer buffer = GetNextBuffer(stream);

                if (buffer == null)
                {
                    // EOF
                    session.RemoveAttribute(CURRENT_STREAM);
                    IWriteRequest currentWriteRequest = (IWriteRequest)session.RemoveAttribute(CURRENT_WRITE_REQUEST);

                    // Write queued WriteRequests.
                    ConcurrentQueue<IWriteRequest> queue = RemoveWriteRequestQueue(session);
                    if (queue != null)
                    {
                        IWriteRequest wr;
                        while (queue.TryDequeue(out wr))
                        {
                            FilterWrite(nextFilter, session, wr);
                        }
                    }

                    currentWriteRequest.Future.Written = true;
                    nextFilter.MessageSent(session, currentWriteRequest);
                }
                else
                {
                    nextFilter.FilterWrite(session, new DefaultWriteRequest(buffer));
                }
            }
        }

        protected abstract IoBuffer GetNextBuffer(T message);

        private ConcurrentQueue<IWriteRequest> GetWriteRequestQueue(IoSession session)
        {
            ConcurrentQueue<IWriteRequest> queue = session.GetAttribute<ConcurrentQueue<IWriteRequest>>(WRITE_REQUEST_QUEUE);
            if (queue == null)
            {
                queue = new ConcurrentQueue<IWriteRequest>();
                session.SetAttribute(WRITE_REQUEST_QUEUE, queue);
            }
            return queue;
        }

        private ConcurrentQueue<IWriteRequest> RemoveWriteRequestQueue(IoSession session)
        {
            return (ConcurrentQueue<IWriteRequest>)session.RemoveAttribute(WRITE_REQUEST_QUEUE);
        }
    }
}
