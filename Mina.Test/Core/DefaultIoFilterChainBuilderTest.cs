using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mina.Core.Filterchain;
using Mina.Filter.Util;

namespace Mina.Core
{
    [TestClass]
    public class DefaultIoFilterChainBuilderTest
    {
        [TestMethod]
        public void TestAdd()
        {
            DefaultIoFilterChainBuilder builder = new DefaultIoFilterChainBuilder();
            builder.AddFirst("A", new NoopFilter());
            builder.AddLast("B", new NoopFilter());
            builder.AddFirst("C", new NoopFilter());
            builder.AddLast("D", new NoopFilter());
            builder.AddBefore("B", "E", new NoopFilter());
            builder.AddBefore("C", "F", new NoopFilter());
            builder.AddAfter("B", "G", new NoopFilter());
            builder.AddAfter("D", "H", new NoopFilter());

            String actual = String.Empty;
            foreach (IEntry entry in builder.GetAll())
            {
                actual += entry.Name;
            }

            Assert.AreEqual("FCAEBGDH", actual);
        }

        [TestMethod]
        public void TestGet()
        {
            DefaultIoFilterChainBuilder builder = new DefaultIoFilterChainBuilder(); // TODO: 初始化为适当的值

            IoFilter filterA = new NoopFilter();
            IoFilter filterB = new NoopFilter();
            IoFilter filterC = new NoopFilter();
            IoFilter filterD = new NoopFilter();

            builder.AddFirst("A", filterA);
            builder.AddLast("B", filterB);
            builder.AddBefore("B", "C", filterC);
            builder.AddAfter("A", "D", filterD);

            Assert.AreSame(filterA, builder.Get("A"));
            Assert.AreSame(filterB, builder.Get("B"));
            Assert.AreSame(filterC, builder.Get("C"));
            Assert.AreSame(filterD, builder.Get("D"));
        }

        [TestMethod]
        public void TestRemove()
        {
            DefaultIoFilterChainBuilder builder = new DefaultIoFilterChainBuilder(); // TODO: 初始化为适当的值

            builder.AddLast("A", new NoopFilter());
            builder.AddLast("B", new NoopFilter());
            builder.AddLast("C", new NoopFilter());
            builder.AddLast("D", new NoopFilter());
            builder.AddLast("E", new NoopFilter());

            builder.Remove("A");
            builder.Remove("E");
            builder.Remove("C");
            builder.Remove("B");
            builder.Remove("D");

            Assert.AreEqual(0, builder.GetAll().Count());
        }

        [TestMethod]
        public void TestClear()
        {
            DefaultIoFilterChainBuilder builder = new DefaultIoFilterChainBuilder(); // TODO: 初始化为适当的值

            builder.AddLast("A", new NoopFilter());
            builder.AddLast("B", new NoopFilter());
            builder.AddLast("C", new NoopFilter());
            builder.AddLast("D", new NoopFilter());
            builder.AddLast("E", new NoopFilter());

            builder.Clear();

            Assert.AreEqual(0, builder.GetAll().Count());
        }
    }
}
