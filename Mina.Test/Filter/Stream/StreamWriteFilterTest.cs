using System;
using System.IO;
#if !NETFX_CORE
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

namespace Mina.Filter.Stream
{
    [TestClass]
    public class StreamWriteFilterTest : AbstractStreamWriteFilterTest<System.IO.Stream, StreamWriteFilter>
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
