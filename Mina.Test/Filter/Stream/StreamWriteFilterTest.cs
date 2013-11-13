using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mina.Filter.Stream
{
    [TestClass]
    public class StreamWriteFilterTest : AbstractStreamWriteFilterTest<System.IO.Stream>
    {
        protected override AbstractStreamWriteFilter<System.IO.Stream> CreateFilter()
        {
            return new StreamWriteFilter();
        }

        protected override System.IO.Stream CreateMessage(Byte[] data)
        {
            return new MemoryStream(data);
        }
    }
}
