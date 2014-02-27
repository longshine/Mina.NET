using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Mina.Util
{
    /// <summary>
    /// Represents a collection of resusable SocketAsyncEventArgs objects.  
    /// </summary>
    class Pool<T>
    {
        ConcurrentStack<T> m_pool;
        
        /// <summary>
        /// Initializes the object pool to the specified size
        /// </summary>
        public Pool()
        {
            m_pool = new ConcurrentStack<T>();
        }

        /// <summary>
        /// Initializes the object pool to the specified size
        /// <param name="collection"></param>
        /// </summary>
        public Pool(IEnumerable<T> collection)
        {
            m_pool = new ConcurrentStack<T>(collection);
        }

        /// <summary>
        /// Add a SocketAsyncEventArg instance to the pool
        /// </summary>
        /// <param name="item">The SocketAsyncEventArgs instance to add to the pool</param>
        public void Push(T item)
        {
            if (item == null) { throw new ArgumentNullException("item", "Items added to a SocketAsyncEventArgsPool cannot be null"); }
            m_pool.Push(item);
        }

        /// <summary>
        /// Removes a SocketAsyncEventArgs instance from the pool
        /// </summary>
        /// <returns>The object removed from the pool</returns>
        public T Pop()
        {
            T e;
            m_pool.TryPop(out e);
            return e;
        }

        /// <summary>
        /// The number of SocketAsyncEventArgs instances in the pool
        /// </summary>
        public int Count
        {
            get { return m_pool.Count; }
        }
    }
}
