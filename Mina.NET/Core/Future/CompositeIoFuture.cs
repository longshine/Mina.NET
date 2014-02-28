using System;
using System.Collections.Generic;
using System.Threading;

namespace Mina.Core.Future
{
    /// <summary>
    /// An <see cref="IoFuture"/> of <see cref="IoFuture"/>s.
    /// It is useful when you want to get notified when all <see cref="IoFuture"/>s are complete.
    /// </summary>
    public class CompositeIoFuture<TFuture> : DefaultIoFuture
        where TFuture : IoFuture
    {
        private Int32 _unnotified;
        private volatile Boolean _constructionFinished;

        /// <summary>
        /// </summary>
        public CompositeIoFuture(IEnumerable<TFuture> children)
            : base(null)
        {
            foreach (TFuture f in children)
            {
                f.Complete += OnComplete;
                Interlocked.Increment(ref _unnotified);
            }

            _constructionFinished = true;
            if (_unnotified == 0)
                Value = true;
        }

        private void OnComplete(Object sender, IoFutureEventArgs e)
        {
            if (Interlocked.Decrement(ref _unnotified) == 0 && _constructionFinished)
                Value = true;
        }
    }
}
