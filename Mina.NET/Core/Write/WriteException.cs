using System;
using System.Collections.Generic;
using System.IO;

namespace Mina.Core.Write
{
    /// <summary>
    /// An exception which is thrown when one or more write operations were failed.
    /// </summary>
    public class WriteException : IOException
    {
        private readonly IList<IWriteRequest> _requests;

        public WriteException(IWriteRequest request)
        {
            _requests = AsRequestList(request);
        }

        public IWriteRequest Request
        {
            get { return _requests[0]; }
        }

        public IEnumerable<IWriteRequest> Requests
        {
            get { return _requests; }
        }

        private static IList<IWriteRequest> AsRequestList(IWriteRequest request)
        {
            if (request == null)
                throw new ArgumentNullException("request");
            List<IWriteRequest> requests = new List<IWriteRequest>(1);
            requests.Add(request);
            return requests.AsReadOnly();
        }
    }
}
