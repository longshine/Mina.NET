using System;
using System.Collections.Concurrent;
using Mina.Core.Session;

namespace Mina.Core.Write
{
    class DefaultWriteRequestQueue : IWriteRequestQueue
    {
        private ConcurrentQueue<IWriteRequest> q = new ConcurrentQueue<IWriteRequest>();

        public Int32 Size
        {
            get { return q.Count; }
        }

        public IWriteRequest Poll(IoSession session)
        {
            IWriteRequest request;
            q.TryDequeue(out request);
            return request;
        }

        public void Offer(IoSession session, IWriteRequest writeRequest)
        {
            q.Enqueue(writeRequest);
        }

        public Boolean IsEmpty(IoSession session)
        {
            return q.IsEmpty;
        }

        public void Clear(IoSession session)
        {
            q = new ConcurrentQueue<IWriteRequest>();
        }

        public void Dispose(IoSession session)
        {
            // Do nothing
        }
    }
}
