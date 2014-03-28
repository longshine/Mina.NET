using System;
using System.IO;
#if !NETFX_CORE
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
using TestInitialize = NUnit.Framework.SetUpAttribute;
using TestCleanup = NUnit.Framework.TearDownAttribute;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
using Mina.Core.File;

namespace Mina.Filter.Stream
{
    [TestClass]
    public class FileRegionWriteFilterTest : AbstractStreamWriteFilterTest<IFileRegion, FileRegionWriteFilter>
    {
        private String file;

        [TestInitialize]
        public void SetUp()
        {
            file = Path.GetTempFileName();
        }

        [TestCleanup]
        public void TearDown()
        {
            File.Delete(file);
        }

        protected override AbstractStreamWriteFilter<IFileRegion> CreateFilter()
        {
            return new FileRegionWriteFilter();
        }

        protected override IFileRegion CreateMessage(Byte[] data)
        {
            File.WriteAllBytes(file, data);
            return new FileInfoFileRegion(new FileInfo(file));
        }
    }
}
