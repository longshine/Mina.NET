using System;
using System.Net;
using Mina.Core.Future;

namespace Mina.Core.Write
{
    /// <summary>
    /// A wrapper for an existing <see cref="IWriteRequest"/>.
    /// </summary>
    public class WriteRequestWrapper : IWriteRequest
    {
        private readonly IWriteRequest _inner;

        /// <summary>
        /// </summary>
        public WriteRequestWrapper(IWriteRequest request)
        {
            if (request == null)
                throw new ArgumentNullException("request");
            _inner = request;
        }

        /// <inheritdoc/>
        public IWriteRequest OriginalRequest
        {
            get { return _inner.OriginalRequest; }
        }

        /// <inheritdoc/>
        public virtual Object Message
        {
            get { return _inner.Message; }
        }

        /// <inheritdoc/>
        public EndPoint Destination
        {
            get { return _inner.Destination; }
        }

        /// <inheritdoc/>
        public Boolean Encoded
        {
            get { return _inner.Encoded; }
        }

        /// <inheritdoc/>
        public IWriteFuture Future
        {
            get { return _inner.Future; }
        }

        public IWriteRequest InnerRequest
        {
            get { return _inner; }
        }
    }
}
