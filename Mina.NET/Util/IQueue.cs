using System;

namespace Mina.Util
{
    public interface IQueue<T>
    {
        Boolean Empty { get; }
        void Enqueue(T item);
        T Dequeue();
        Int32 Count { get; }
    }

    public class ConcurrentQueue<T> : System.Collections.Concurrent.ConcurrentQueue<T>, IQueue<T>
    {
        public Boolean Empty
        {
            get { return Count == 0; }
        }

        public T Dequeue()
        {
            T e = default(T);
            this.TryDequeue(out e);
            return e;
        }
    }
}
