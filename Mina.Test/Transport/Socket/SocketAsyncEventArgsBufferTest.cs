using System;
using System.Text;
#if !NETFX_CORE
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
using Mina.Transport.Socket;

namespace Mina.Core.Buffer
{
    /// <summary>
    /// IoBufferTest 的摘要说明
    /// </summary>
    [TestClass]
    public class SocketAsyncEventArgsBufferTest
    {
        private static IoBufferAllocator allocator = SocketAsyncEventArgsBufferAllocator.Instance;

        [TestMethod]
        public void TestSliceAutoExpand()
        {
            IoBuffer buffer = allocator.Allocate(8);
            buffer.AutoExpand = true;
            Assert.IsTrue(buffer.AutoExpand, "Should AutoExpand");

            IoBuffer slice = buffer.Slice();
            Assert.IsFalse(buffer.AutoExpand, "Should *NOT* AutoExpand");
            Assert.IsFalse(slice.AutoExpand, "Should *NOT* AutoExpand");
        }

        [TestMethod]
        public void TestAllocate()
        {
            for (int i = 10; i < 1048576 * 2; i = i * 11 / 10) // increase by 10%
            {
                IoBuffer buf = allocator.Allocate(i);
                Assert.AreEqual(0, buf.Position);
                Assert.AreEqual(buf.Capacity, buf.Remaining);
                Assert.IsTrue(buf.Capacity >= i);
                Assert.IsTrue(buf.Capacity < i * 2);
            }
        }

        [TestMethod]
        public void TestAutoExpand()
        {
            IoBuffer buf = allocator.Allocate(1);

            buf.Put((byte)0);
            try
            {
                buf.Put((byte)0);
                Assert.Fail("Buffer can't auto expand, with autoExpand property set at false");
            }
            catch (OverflowException)
            {
                // Expected Exception as auto expand property is false
                Assert.IsTrue(true);
            }

            buf.AutoExpand = true;
            buf.Put((byte)0);
            Assert.AreEqual(2, buf.Position);
            Assert.AreEqual(2, buf.Limit);
            Assert.AreEqual(2, buf.Capacity);

            buf.AutoExpand = false;
            try
            {
                buf.Put(3, (byte)0);
                Assert.Fail("Buffer can't auto expand, with autoExpand property set at false");
            }
            catch (IndexOutOfRangeException)
            {
                // Expected Exception as auto expand property is false
                Assert.IsTrue(true);
            }

            buf.AutoExpand = true;
            buf.Put(3, (byte)0);
            Assert.AreEqual(2, buf.Position);
            Assert.AreEqual(4, buf.Limit);
            Assert.AreEqual(4, buf.Capacity);

            // Make sure the buffer is doubled up.
            buf = allocator.Allocate(1);
            buf.AutoExpand = true;
            int lastCapacity = buf.Capacity;
            for (int i = 0; i < 1048576; i++)
            {
                buf.Put((byte)0);
                if (lastCapacity != buf.Capacity)
                {
                    Assert.AreEqual(lastCapacity * 2, buf.Capacity);
                    lastCapacity = buf.Capacity;
                }
            }
        }

        [TestMethod]
        public void TestAutoExpandMark()
        {
            IoBuffer buf = allocator.Allocate(4);
            buf.AutoExpand = true;

            buf.Put((byte)0);
            buf.Put((byte)0);
            buf.Put((byte)0);

            // Position should be 3 when we reset this buffer.
            buf.Mark();

            // Overflow it
            buf.Put((byte)0);
            buf.Put((byte)0);

            Assert.AreEqual(5, buf.Position);
            buf.Reset();
            Assert.AreEqual(3, buf.Position);
        }

        [TestMethod]
        public void TestAutoShrink()
        {
            IoBuffer buf = allocator.Allocate(8);
            buf.AutoShrink = true;

            // Make sure the buffer doesn't shrink too much (less than the initial
            // capacity.)
            buf.Sweep((byte)1);
            buf.Fill(7);
            buf.Compact();
            Assert.AreEqual(8, buf.Capacity);
            Assert.AreEqual(1, buf.Position);
            Assert.AreEqual(8, buf.Limit);
            buf.Clear();
            Assert.AreEqual(1, buf.Get());

            // Expand the buffer.
            buf.Capacity = 32;
            buf.Clear();
            Assert.AreEqual(32, buf.Capacity);

            // Make sure the buffer shrinks when only 1/4 is being used.
            buf.Sweep((byte)1);
            buf.Fill(24);
            buf.Compact();
            Assert.AreEqual(16, buf.Capacity);
            Assert.AreEqual(8, buf.Position);
            Assert.AreEqual(16, buf.Limit);
            buf.Clear();
            for (int i = 0; i < 8; i++)
            {
                Assert.AreEqual(1, buf.Get());
            }

            // Expand the buffer.
            buf.Capacity = 32;
            buf.Clear();
            Assert.AreEqual(32, buf.Capacity);

            // Make sure the buffer shrinks when only 1/8 is being used.
            buf.Sweep((byte)1);
            buf.Fill(28);
            buf.Compact();
            Assert.AreEqual(8, buf.Capacity);
            Assert.AreEqual(4, buf.Position);
            Assert.AreEqual(8, buf.Limit);
            buf.Clear();
            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(1, buf.Get());
            }

            // Expand the buffer.
            buf.Capacity =32;
            buf.Clear();
            Assert.AreEqual(32, buf.Capacity);

            // Make sure the buffer shrinks when 0 byte is being used.
            buf.Fill(32);
            buf.Compact();
            Assert.AreEqual(8, buf.Capacity);
            Assert.AreEqual(0, buf.Position);
            Assert.AreEqual(8, buf.Limit);

            // Expand the buffer.
            buf.Capacity = 32;
            buf.Clear();
            Assert.AreEqual(32, buf.Capacity);

            // Make sure the buffer doesn't shrink when more than 1/4 is being used.
            buf.Sweep((byte)1);
            buf.Fill(23);
            buf.Compact();
            Assert.AreEqual(32, buf.Capacity);
            Assert.AreEqual(9, buf.Position);
            Assert.AreEqual(32, buf.Limit);
            buf.Clear();
            for (int i = 0; i < 9; i++)
            {
                Assert.AreEqual(1, buf.Get());
            }
        }

        [TestMethod]
        public void TestPutString()
        {
            IoBuffer buf = allocator.Allocate(16);
            Encoding encoding = Encoding.GetEncoding("ISO-8859-1");

            buf.PutString("ABC", encoding);
            Assert.AreEqual(3, buf.Position);
            buf.Clear();
            Assert.AreEqual((byte)'A', buf.Get(0));
            Assert.AreEqual((byte)'B', buf.Get(1));
            Assert.AreEqual((byte)'C', buf.Get(2));

            //buf.PutString("D", 5, encoding);
            //Assert.AreEqual(5, buf.Position);
            //buf.Clear();
            //Assert.AreEqual('D', buf.Get(0));
            //Assert.AreEqual(0, buf.Get(1));

            //buf.PutString("EFG", 2, encoding);
            //Assert.AreEqual(2, buf.Position);
            //buf.Clear();
            //Assert.AreEqual('E', buf.Get(0));
            //Assert.AreEqual('F', buf.Get(1));
            //Assert.AreEqual('C', buf.Get(2)); // C may not be overwritten

            // UTF-16: We specify byte order to omit BOM.
            encoding = Encoding.GetEncoding("UTF-16BE");
            buf.Clear();

            buf.PutString("ABC", encoding);
            Assert.AreEqual(6, buf.Position);
            buf.Clear();

            Assert.AreEqual(0, buf.Get(0));
            Assert.AreEqual((byte)'A', buf.Get(1));
            Assert.AreEqual(0, buf.Get(2));
            Assert.AreEqual((byte)'B', buf.Get(3));
            Assert.AreEqual(0, buf.Get(4));
            Assert.AreEqual((byte)'C', buf.Get(5));

            //buf.PutString("D", 10, encoding);
            //Assert.AreEqual(10, buf.Position);
            //buf.Clear();
            //Assert.AreEqual(0, buf.Get(0));
            //Assert.AreEqual('D', buf.Get(1));
            //Assert.AreEqual(0, buf.Get(2));
            //Assert.AreEqual(0, buf.Get(3));

            //buf.PutString("EFG", 4, encoding);
            //Assert.AreEqual(4, buf.Position);
            //buf.Clear();
            //Assert.AreEqual(0, buf.Get(0));
            //Assert.AreEqual('E', buf.Get(1));
            //Assert.AreEqual(0, buf.Get(2));
            //Assert.AreEqual('F', buf.Get(3));
            //Assert.AreEqual(0, buf.Get(4)); // C may not be overwritten
            //Assert.AreEqual('C', buf.Get(5)); // C may not be overwritten

            // Test putting an emptry string
            buf.PutString("", encoding);
            Assert.AreEqual(0, buf.Position);
            //buf.PutString("", 4, encoding);
            //Assert.AreEqual(4, buf.Position);
            //Assert.AreEqual(0, buf.Get(0));
            //Assert.AreEqual(0, buf.Get(1));
        }

        [TestMethod]
        public void TestSweepWithZeros()
        {
            IoBuffer buf = allocator.Allocate(4);
            Int32 i;
            unchecked
            {
                i = (Int32)0xdeadbeef;
            }
            buf.PutInt32(i);
            buf.Clear();
            Assert.AreEqual(i, buf.GetInt32());
            Assert.AreEqual(4, buf.Position);
            Assert.AreEqual(4, buf.Limit);

            buf.Sweep();
            Assert.AreEqual(0, buf.Position);
            Assert.AreEqual(4, buf.Limit);
            Assert.AreEqual(0x0, buf.GetInt32());
        }

        [TestMethod]
        public void TestSweepNonZeros()
        {
            IoBuffer buf = allocator.Allocate(4);
            Int32 i;
            unchecked
            {
                i = (Int32)0xdeadbeef;
            }
            buf.PutInt32(i);
            buf.Clear();
            Assert.AreEqual(i, buf.GetInt32());
            Assert.AreEqual(4, buf.Position);
            Assert.AreEqual(4, buf.Limit);

            buf.Sweep((byte)0x45);
            Assert.AreEqual(0, buf.Position);
            Assert.AreEqual(4, buf.Limit);
            Assert.AreEqual(0x45454545, buf.GetInt32());
        }

        [TestMethod]
        public void TestWrapSubArray()
        {
            byte[] array = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            IoBuffer buf = allocator.Wrap(array, 3, 4);
            Assert.AreEqual(3, buf.Position);
            Assert.AreEqual(7, buf.Limit);
            Assert.AreEqual(10, buf.Capacity);

            buf.Clear();
            Assert.AreEqual(0, buf.Position);
            Assert.AreEqual(10, buf.Limit);
            Assert.AreEqual(10, buf.Capacity);
        }

        [TestMethod]
        public void TestDuplicate()
        {
            IoBuffer original;
            IoBuffer duplicate;

            // Test if the buffer is duplicated correctly.
            original = allocator.Allocate(16).Sweep();
            original.Position = 4;
            original.Limit = 10;

            duplicate = original.Duplicate();
            original.Put(4, (byte)127);
            Assert.AreEqual(4, duplicate.Position);
            Assert.AreEqual(10, duplicate.Limit);
            Assert.AreEqual(16, duplicate.Capacity);
            Assert.AreNotSame(original, duplicate);
            //Assert.AreSame(original.buf().array(), duplicate.buf().array());
            Assert.AreEqual(127, duplicate.Get(4));

            // Test a duplicate of a duplicate.
            original = allocator.Allocate(16);
            duplicate = original.Duplicate().Duplicate();
            Assert.AreNotSame(original, duplicate);
            //Assert.AreSame(original.buf().array(), duplicate.buf().array());

            // Try to expand.
            original = allocator.Allocate(16);
            original.AutoExpand = true;
            duplicate = original.Duplicate();
            Assert.IsFalse(original.AutoExpand);

            try
            {
                original.AutoExpand = true;
                Assert.Fail("Derived buffers and their parent can't be expanded");
            }
            catch (InvalidOperationException)
            {
                // Expected an Exception, signifies test success
                Assert.IsTrue(true);
            }

            try
            {
                duplicate.AutoExpand = true;
                Assert.Fail("Derived buffers and their parent can't be expanded");
            }
            catch (InvalidOperationException)
            {
                // Expected an Exception, signifies test success
                Assert.IsTrue(true);
            }
        }

        [TestMethod]
        public void TestSlice()
        {
            IoBuffer original;
            IoBuffer slice;

            // Test if the buffer is sliced correctly.
            original = allocator.Allocate(16).Sweep();
            original.Position = 4;
            original.Limit = 10;
            slice = original.Slice();
            original.Put(4, (byte)127);
            Assert.AreEqual(0, slice.Position);
            Assert.AreEqual(6, slice.Limit);
            Assert.AreEqual(6, slice.Capacity);
            Assert.AreNotSame(original, slice);
            Assert.AreEqual(127, slice.Get(0));
        }

        [TestMethod]
        public void TestReadOnlyBuffer()
        {
            IoBuffer original;
            IoBuffer duplicate;

            // Test if the buffer is duplicated correctly.
            original = allocator.Allocate(16).Sweep();
            original.Position = 4;
            original.Limit = 10;
            duplicate = original.AsReadOnlyBuffer();
            original.Put(4, (byte)127);
            Assert.AreEqual(4, duplicate.Position);
            Assert.AreEqual(10, duplicate.Limit);
            Assert.AreEqual(16, duplicate.Capacity);
            Assert.AreNotSame(original, duplicate);
            Assert.AreEqual(127, duplicate.Get(4));

            // Try to expand.
            try
            {
                original = allocator.Allocate(16);
                duplicate = original.AsReadOnlyBuffer();
                duplicate.PutString("A very very very very looooooong string", Encoding.ASCII);
                Assert.Fail("ReadOnly buffer's can't be expanded");
            }
            catch (OverflowException)
            {
                // Expected an Exception, signifies test success
                Assert.IsTrue(true);
            }
        }
    }
}
