using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mina.Filter.Stream
{
    [TestClass]
    public abstract class AbstractStreamWriteFilterTest<T> where T : class
    {
        [TestMethod]
        public void TestSetWriteBufferSize()
        {
            AbstractStreamWriteFilter<T> filter = CreateFilter();

            try
            {
                filter.WriteBufferSize = 0;
                Assert.Fail("0 writeBuferSize specified. IllegalArgumentException expected.");
            }
            catch (ArgumentException)
            {
                // Pass, exception was thrown
                // Signifies a successful test execution
                Assert.IsTrue(true);
            }

            try
            {
                filter.WriteBufferSize = -100;
                Assert.Fail("Negative writeBuferSize specified. IllegalArgumentException expected.");
            }
            catch (ArgumentException)
            {
                // Pass, exception was thrown
                // Signifies a successful test execution
                Assert.IsTrue(true);
            }

            filter.WriteBufferSize = 1;
            Assert.AreEqual(1, filter.WriteBufferSize);
            filter.WriteBufferSize = 1024;
            Assert.AreEqual(1024, filter.WriteBufferSize);
        }

        protected abstract AbstractStreamWriteFilter<T> CreateFilter();
        protected abstract T CreateMessage(Byte[] data);
    }
}
