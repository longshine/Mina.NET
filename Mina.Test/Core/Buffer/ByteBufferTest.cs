using System;
using System.Collections.Generic;
using System.Text;
#if !NETFX_CORE
using NUnit.Framework;
using TestClass = NUnit.Framework.TestFixtureAttribute;
using TestMethod = NUnit.Framework.TestAttribute;
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif

namespace Mina.Core.Buffer
{
    /// <summary>
    /// IoBufferTest 的摘要说明
    /// </summary>
    [TestClass]
    public class ByteBufferTest
    {
        [TestMethod]
        public void TestSliceAutoExpand()
        {
            IoBuffer buffer = ByteBufferAllocator.Instance.Allocate(8);
            buffer.AutoExpand = true;
            Assert.IsTrue(buffer.AutoExpand, "Should AutoExpand");

            IoBuffer slice = buffer.Slice();
            Assert.IsFalse(buffer.AutoExpand, "Should *NOT* AutoExpand");
            Assert.IsFalse(slice.AutoExpand, "Should *NOT* AutoExpand");
        }

        [TestMethod]
        public void TestNormalizeCapacity()
        {
            // A few sanity checks
            Assert.AreEqual(Int32.MaxValue, IoBuffer.NormalizeCapacity(-10));
            Assert.AreEqual(0, IoBuffer.NormalizeCapacity(0));
            Assert.AreEqual(Int32.MaxValue, IoBuffer.NormalizeCapacity(Int32.MaxValue));
            Assert.AreEqual(Int32.MaxValue, IoBuffer.NormalizeCapacity(Int32.MinValue));
            Assert.AreEqual(Int32.MaxValue, IoBuffer.NormalizeCapacity(Int32.MaxValue - 10));

            // A sanity check test for all the powers of 2
            for (int i = 0; i < 30; i++)
            {
                int n = 1 << i;

                Assert.AreEqual(n, IoBuffer.NormalizeCapacity(n));

                if (i > 1)
                {
                    // test that n - 1 will be normalized to n (notice that n = 2^i)
                    Assert.AreEqual(n, IoBuffer.NormalizeCapacity(n - 1));
                }

                // test that n + 1 will be normalized to 2^(i + 1)
                Assert.AreEqual(n << 1, IoBuffer.NormalizeCapacity(n + 1));
            }

            // The first performance test measures the time to normalize integers
            // from 0 to 2^27 (it tests 2^27 integers)
            DateTime time = DateTime.Now;

            for (int i = 0; i < 1 << 27; i++)
            {
                IoBuffer.NormalizeCapacity(i);
            }

            DateTime time2 = DateTime.Now;
            //Console.WriteLine("Time for performance test 1: " + (time2 - time).TotalMilliseconds + "ms");

            // The second performance test measures the time to normalize integers
            // from Int32.MaxValue to Int32.MaxValue - 2^27 (it tests 2^27
            // integers)
            time = DateTime.Now;
            for (int i = Int32.MaxValue; i > Int32.MaxValue - (1 << 27); i--)
            {
                IoBuffer.NormalizeCapacity(i);
            }

            time2 = DateTime.Now;
            //Console.WriteLine("Time for performance test 2: " + (time2 - time).TotalMilliseconds + "ms");
        }

        [TestMethod]
        public void TestObjectSerialization()
        {
            IoBuffer buf = ByteBufferAllocator.Instance.Allocate(16);
            buf.AutoExpand = true;
            List<Object> o = new List<Object>();
            o.Add(new DateTime());
            o.Add(typeof(Int64));

            // Test writing an object.
            buf.PutObject(o);

            // Test reading an object.
            buf.Clear();
            Object o2 = buf.GetObject();
#if !NETFX_CORE
            Assert.IsInstanceOf(o.GetType(), o2);
#else
            Assert.IsInstanceOfType(o2, o.GetType());
#endif
            List<Object> l2 = (List<Object>)o2;
            Assert.AreEqual(o.Count, l2.Count);
            for (Int32 i = 0; i < o.Count; i++)
            {
                Assert.AreEqual(o[i], l2[i]);
            }

            // This assertion is just to make sure that deserialization occurred.
            Assert.AreNotSame(o, o2);
        }

        [TestMethod]
        public void TestAllocate()
        {
            for (int i = 10; i < 1048576 * 2; i = i * 11 / 10) // increase by 10%
            {
                IoBuffer buf = ByteBufferAllocator.Instance.Allocate(i);
                Assert.AreEqual(0, buf.Position);
                Assert.AreEqual(buf.Capacity, buf.Remaining);
                Assert.IsTrue(buf.Capacity >= i);
                Assert.IsTrue(buf.Capacity < i * 2);
            }
        }

        [TestMethod]
        public void TestAutoExpand()
        {
            IoBuffer buf = ByteBufferAllocator.Instance.Allocate(1);

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
            buf = ByteBufferAllocator.Instance.Allocate(1);
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
            IoBuffer buf = ByteBufferAllocator.Instance.Allocate(4);
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
            IoBuffer buf = ByteBufferAllocator.Instance.Allocate(8);
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
            buf.Capacity = 32;
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
        public void TestGetString()
        {
            IoBuffer buf = ByteBufferAllocator.Instance.Allocate(16);
            Encoding encoding = Encoding.UTF8;

            buf.Clear();
            buf.PutString("hello", encoding);
            buf.Put((Byte)0);
            buf.Flip();
            Assert.AreEqual("hello", buf.GetString(encoding));

            buf.Clear();
            buf.PutString("hello", encoding);
            buf.Flip();
            Assert.AreEqual("hello", buf.GetString(encoding));

            encoding = Encoding.GetEncoding("ISO-8859-1");
            buf.Clear();
            buf.Put((Byte)'A');
            buf.Put((Byte)'B');
            buf.Put((Byte)'C');
            buf.Put((Byte)0);

            buf.Position = 0;
            Assert.AreEqual("ABC", buf.GetString(encoding));
            Assert.AreEqual(4, buf.Position);

            buf.Position = 0;
            buf.Limit = 1;
            Assert.AreEqual("A", buf.GetString(encoding));
            Assert.AreEqual(1, buf.Position);

            buf.Clear();
            Assert.AreEqual("ABC", buf.GetString(10, encoding));
            Assert.AreEqual(10, buf.Position);

            buf.Clear();
            Assert.AreEqual("A", buf.GetString(1, encoding));
            Assert.AreEqual(1, buf.Position);

            // Test a trailing garbage
            buf.Clear();
            buf.Put((Byte)'A');
            buf.Put((Byte)'B');
            buf.Put((Byte)0);
            buf.Put((Byte)'C');
            buf.Position = 0;
            Assert.AreEqual("AB", buf.GetString(4, encoding));
            Assert.AreEqual(4, buf.Position);

            buf.Clear();
            buf.FillAndReset(buf.Limit);
            encoding = Encoding.GetEncoding("UTF-16BE");
            buf.Put((Byte)0);
            buf.Put((Byte)'A');
            buf.Put((Byte)0);
            buf.Put((Byte)'B');
            buf.Put((Byte)0);
            buf.Put((Byte)'C');
            buf.Put((Byte)0);
            buf.Put((Byte)0);

            buf.Position = 0;
            Assert.AreEqual("ABC", buf.GetString(encoding));
            Assert.AreEqual(8, buf.Position);

            buf.Position = 0;
            buf.Limit = 2;
            Assert.AreEqual("A", buf.GetString(encoding));
            Assert.AreEqual(2, buf.Position);

            buf.Position = 0;
            buf.Limit = 3;
            Assert.AreEqual("A", buf.GetString(encoding));
            Assert.AreEqual(2, buf.Position);

            buf.Clear();
            Assert.AreEqual("ABC", buf.GetString(10, encoding));
            Assert.AreEqual(10, buf.Position);

            buf.Clear();
            Assert.AreEqual("A", buf.GetString(2, encoding));
            Assert.AreEqual(2, buf.Position);

            buf.Clear();
            try
            {
                buf.GetString(1, encoding);
                Assert.Fail();
            }
            catch (Exception)
            {
                // Expected an Exception, signifies test success
                Assert.IsTrue(true);
            }

            // Test getting strings from an empty buffer.
            buf.Clear();
            buf.Limit = 0;
            Assert.AreEqual("", buf.GetString(encoding));
            Assert.AreEqual("", buf.GetString(2, encoding));

            // Test getting strings from non-empty buffer which is filled with 0x00
            buf.Clear();
            buf.PutInt32(0);
            buf.Clear();
            buf.Limit = 4;
            Assert.AreEqual("", buf.GetString(encoding));
            Assert.AreEqual(2, buf.Position);
            Assert.AreEqual(4, buf.Limit);

            buf.Position = 0;
            Assert.AreEqual("", buf.GetString(2, encoding));
            Assert.AreEqual(2, buf.Position);
            Assert.AreEqual(4, buf.Limit);
        }

        [TestMethod]
        public void TestPutString()
        {
            IoBuffer buf = ByteBufferAllocator.Instance.Allocate(16);
            Encoding encoding = Encoding.GetEncoding("ISO-8859-1");

            buf.PutString("ABC", encoding);
            Assert.AreEqual(3, buf.Position);
            buf.Clear();
            Assert.AreEqual((Byte)'A', buf.Get(0));
            Assert.AreEqual((Byte)'B', buf.Get(1));
            Assert.AreEqual((Byte)'C', buf.Get(2));

            buf.PutString("D", 5, encoding);
            Assert.AreEqual(5, buf.Position);
            buf.Clear();
            Assert.AreEqual((Byte)'D', buf.Get(0));
            Assert.AreEqual(0, buf.Get(1));

            buf.PutString("EFG", 2, encoding);
            Assert.AreEqual(2, buf.Position);
            buf.Clear();
            Assert.AreEqual((Byte)'E', buf.Get(0));
            Assert.AreEqual((Byte)'F', buf.Get(1));
            Assert.AreEqual((Byte)'C', buf.Get(2)); // C may not be overwritten

            // UTF-16: We specify byte order to omit BOM.
            encoding = Encoding.GetEncoding("UTF-16BE");
            buf.Clear();

            buf.PutString("ABC", encoding);
            Assert.AreEqual(6, buf.Position);
            buf.Clear();

            Assert.AreEqual(0, buf.Get(0));
            Assert.AreEqual((Byte)'A', buf.Get(1));
            Assert.AreEqual(0, buf.Get(2));
            Assert.AreEqual((Byte)'B', buf.Get(3));
            Assert.AreEqual(0, buf.Get(4));
            Assert.AreEqual((Byte)'C', buf.Get(5));

            buf.PutString("D", 10, encoding);
            Assert.AreEqual(10, buf.Position);
            buf.Clear();
            Assert.AreEqual(0, buf.Get(0));
            Assert.AreEqual((Byte)'D', buf.Get(1));
            Assert.AreEqual(0, buf.Get(2));
            Assert.AreEqual(0, buf.Get(3));

            buf.PutString("EFG", 4, encoding);
            Assert.AreEqual(4, buf.Position);
            buf.Clear();
            Assert.AreEqual(0, buf.Get(0));
            Assert.AreEqual((Byte)'E', buf.Get(1));
            Assert.AreEqual(0, buf.Get(2));
            Assert.AreEqual((Byte)'F', buf.Get(3));
            Assert.AreEqual(0, buf.Get(4)); // C may not be overwritten
            Assert.AreEqual((Byte)'C', buf.Get(5)); // C may not be overwritten

            // Test putting an emptry string
            buf.PutString("", encoding);
            Assert.AreEqual(0, buf.Position);
            buf.PutString("", 4, encoding);
            Assert.AreEqual(4, buf.Position);
            Assert.AreEqual(0, buf.Get(0));
            Assert.AreEqual(0, buf.Get(1));
        }

        [TestMethod]
        public void TestGetPrefixedString()
        {
            IoBuffer buf = IoBuffer.Allocate(16);
            Encoding encoding = Encoding.GetEncoding("ISO-8859-1");

            buf.PutInt16((short)3);
            buf.PutString("ABCD", encoding);
            buf.Clear();
            Assert.AreEqual("ABC", buf.GetPrefixedString(encoding));
        }

        [TestMethod]
        public void TestPutPrefixedString()
        {
            Encoding encoding = Encoding.GetEncoding("ISO-8859-1");
            IoBuffer buf = IoBuffer.Allocate(16);
            buf.FillAndReset(buf.Remaining);

            // Without autoExpand
            buf.PutPrefixedString("ABC", encoding);
            Assert.AreEqual(5, buf.Position);
            Assert.AreEqual(0, buf.Get(0));
            Assert.AreEqual(3, buf.Get(1));
            Assert.AreEqual((byte)'A', buf.Get(2));
            Assert.AreEqual((byte)'B', buf.Get(3));
            Assert.AreEqual((byte)'C', buf.Get(4));

            buf.Clear();
            try
            {
                buf.PutPrefixedString("123456789012345", encoding);
                Assert.Fail();
            }
            catch (OverflowException)
            {
                // Expected an Exception, signifies test success
                Assert.IsTrue(true);
            }

            // With autoExpand
            buf.Clear();
            buf.AutoExpand = true;
            buf.PutPrefixedString("123456789012345", encoding);
            Assert.AreEqual(17, buf.Position);
            Assert.AreEqual(0, buf.Get(0));
            Assert.AreEqual(15, buf.Get(1));
            Assert.AreEqual((byte)'1', buf.Get(2));
            Assert.AreEqual((byte)'2', buf.Get(3));
            Assert.AreEqual((byte)'3', buf.Get(4));
            Assert.AreEqual((byte)'4', buf.Get(5));
            Assert.AreEqual((byte)'5', buf.Get(6));
            Assert.AreEqual((byte)'6', buf.Get(7));
            Assert.AreEqual((byte)'7', buf.Get(8));
            Assert.AreEqual((byte)'8', buf.Get(9));
            Assert.AreEqual((byte)'9', buf.Get(10));
            Assert.AreEqual((byte)'0', buf.Get(11));
            Assert.AreEqual((byte)'1', buf.Get(12));
            Assert.AreEqual((byte)'2', buf.Get(13));
            Assert.AreEqual((byte)'3', buf.Get(14));
            Assert.AreEqual((byte)'4', buf.Get(15));
            Assert.AreEqual((byte)'5', buf.Get(16));
        }

        [TestMethod]
        public void TestPutPrefixedStringWithPrefixLength()
        {
            Encoding encoding = Encoding.GetEncoding("ISO-8859-1");
            IoBuffer buf = IoBuffer.Allocate(16).Sweep();
            buf.AutoExpand = true;

            buf.PutPrefixedString("A", 1, encoding);
            Assert.AreEqual(2, buf.Position);
            Assert.AreEqual(1, buf.Get(0));
            Assert.AreEqual((byte)'A', buf.Get(1));

            buf.Sweep();
            buf.PutPrefixedString("A", 2, encoding);
            Assert.AreEqual(3, buf.Position);
            Assert.AreEqual(0, buf.Get(0));
            Assert.AreEqual(1, buf.Get(1));
            Assert.AreEqual((byte)'A', buf.Get(2));

            buf.Sweep();
            buf.PutPrefixedString("A", 4, encoding);
            Assert.AreEqual(5, buf.Position);
            Assert.AreEqual(0, buf.Get(0));
            Assert.AreEqual(0, buf.Get(1));
            Assert.AreEqual(0, buf.Get(2));
            Assert.AreEqual(1, buf.Get(3));
            Assert.AreEqual((byte)'A', buf.Get(4));
        }

        [TestMethod]
        public void TestSweepWithZeros()
        {
            IoBuffer buf = ByteBufferAllocator.Instance.Allocate(4);
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
            IoBuffer buf = ByteBufferAllocator.Instance.Allocate(4);
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

            IoBuffer buf = ByteBufferAllocator.Instance.Wrap(array, 3, 4);
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
            original = ByteBufferAllocator.Instance.Allocate(16).Sweep();
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
            original = ByteBufferAllocator.Instance.Allocate(16);
            duplicate = original.Duplicate().Duplicate();
            Assert.AreNotSame(original, duplicate);
            //Assert.AreSame(original.buf().array(), duplicate.buf().array());

            // Try to expand.
            original = ByteBufferAllocator.Instance.Allocate(16);
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
            original = ByteBufferAllocator.Instance.Allocate(16).Sweep();
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
            original = ByteBufferAllocator.Instance.Allocate(16).Sweep();
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
                original = ByteBufferAllocator.Instance.Allocate(16);
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

        [TestMethod]
        public void TestGetUnsigned()
        {
            IoBuffer buf = IoBuffer.Allocate(16);
            buf.Put((byte)0xA4);
            buf.Put((byte)0xD0);
            buf.Put((byte)0xB3);
            buf.Put((byte)0xCD);
            buf.Flip();

            buf.Order = ByteOrder.LittleEndian;

            buf.Mark();
            Assert.AreEqual(0xA4, buf.Get());
            buf.Reset();
            Assert.AreEqual(0xD0A4, (UInt16)buf.GetInt16());
            buf.Reset();
            Assert.AreEqual(0xCDB3D0A4L, (UInt32)buf.GetInt32());
        }

        [TestMethod]
        public void TestIndexOf()
        {
            for (int i = 0; i < 2; i++)
            {
                IoBuffer buf = IoBuffer.Allocate(16);
                buf.Put((byte)0x1);
                buf.Put((byte)0x2);
                buf.Put((byte)0x3);
                buf.Put((byte)0x4);
                buf.Put((byte)0x1);
                buf.Put((byte)0x2);
                buf.Put((byte)0x3);
                buf.Put((byte)0x4);
                buf.Position = 2;
                buf.Limit = 5;

                Assert.AreEqual(4, buf.IndexOf((byte)0x1));
                Assert.AreEqual(-1, buf.IndexOf((byte)0x2));
                Assert.AreEqual(2, buf.IndexOf((byte)0x3));
                Assert.AreEqual(3, buf.IndexOf((byte)0x4));
            }
        }

        [TestMethod]
        public void TestGetSlice()
        {
            IoBuffer buf = IoBuffer.Allocate(36);

            for (Byte i = 0; i < 36; i++)
            {
                buf.Put(i);
            }

            IoBuffer res = buf.GetSlice(1, 3);

            // The limit should be 3, the pos should be 0 and the bytes read
            // should be 0x01, 0x02 and 0x03
            Assert.AreEqual(0, res.Position);
            Assert.AreEqual(3, res.Limit);
            Assert.AreEqual(0x01, res.Get());
            Assert.AreEqual(0x02, res.Get());
            Assert.AreEqual(0x03, res.Get());

            // Now test after a flip
            buf.Flip();

            res = buf.GetSlice(1, 3);
            // The limit should be 3, the pos should be 0 and the bytes read
            // should be 0x01, 0x02 and 0x03
            Assert.AreEqual(0, res.Position);
            Assert.AreEqual(3, res.Limit);
            Assert.AreEqual(0x01, res.Get());
            Assert.AreEqual(0x02, res.Get());
            Assert.AreEqual(0x03, res.Get());
        }

        [TestMethod]
        public void TestShrink()
        {
            IoBuffer buf = IoBuffer.Allocate(36);
            buf.MinimumCapacity = 0;

            buf.Limit = 18;
            buf.Shrink();
            Assert.AreEqual(18, buf.Capacity);

            buf.Limit = 9;
            buf.Shrink();
            Assert.AreEqual(9, buf.Capacity);

            buf.Limit = 4;
            buf.Shrink();
            Assert.AreEqual(4, buf.Capacity);

            buf.Limit = 2;
            buf.Shrink();
            Assert.AreEqual(2, buf.Capacity);

            buf.Limit = 1;
            buf.Shrink();
            Assert.AreEqual(1, buf.Capacity);

            buf.Limit = 0;
            buf.Shrink();
            Assert.AreEqual(0, buf.Capacity);
        }

        [TestMethod]
        public void TestShrink2()
        {
            IoBuffer buf = IoBuffer.Allocate(36);
            buf.Put(Encoding.Default.GetBytes("012345"));
            buf.Flip();
            buf.Position = 4;
            buf.MinimumCapacity = 8;

            IoBuffer newBuf = buf.Shrink();
            Assert.AreEqual(4, newBuf.Position);
            Assert.AreEqual(6, newBuf.Limit);
            Assert.AreEqual(9, newBuf.Capacity);
            Assert.AreEqual(8, newBuf.MinimumCapacity);

            buf = IoBuffer.Allocate(6);
            buf.Put(Encoding.Default.GetBytes("012345"));
            buf.Flip();
            buf.Position = 4;

            newBuf = buf.Shrink();
            Assert.AreEqual(4, newBuf.Position);
            Assert.AreEqual(6, newBuf.Limit);
            Assert.AreEqual(6, newBuf.Capacity);
            Assert.AreEqual(6, newBuf.MinimumCapacity);
        }

        [TestMethod]
        public void TestCapacity()
        {
            IoBuffer buffer = IoBuffer.Allocate(10);

            buffer.Put(Encoding.Default.GetBytes("012345"));
            buffer.Flip();

            // See if we can decrease the capacity (we shouldn't be able to go under the minimul capacity)
            buffer.Capacity = 7;
            Assert.AreEqual(10, buffer.Capacity);

            // See if we can increase the capacity
            buffer = IoBuffer.Allocate(10);

            buffer.Put(Encoding.Default.GetBytes("012345"));
            buffer.Flip();
            buffer.Capacity = 14;
            Assert.AreEqual(14, buffer.Capacity);
            buffer.Put(0, (byte)'9');
            Assert.AreEqual((byte)'9', buffer.Get(0));
            Assert.AreEqual((byte)'9', buffer.Get(0));

            // See if we can go down when the minimum capacity is below the current capacity
            // We should not.
            buffer = IoBuffer.Allocate(10);
            buffer.Capacity = 5;
            Assert.AreEqual(10, buffer.MinimumCapacity);
            Assert.AreEqual(10, buffer.Capacity);
        }

        [TestMethod]
        public void TestExpand()
        {
            IoBuffer buffer = IoBuffer.Allocate(10);

            buffer.Put(Encoding.Default.GetBytes("012345"));
            buffer.Flip();

            Assert.AreEqual(6, buffer.Remaining);

            // See if we can expand with a lower number of remaining bytes. We should not.
            IoBuffer newBuffer = buffer.Expand(2);
            Assert.AreEqual(6, newBuffer.Limit);
            Assert.AreEqual(10, newBuffer.Capacity);
            Assert.AreEqual(0, newBuffer.Position);

            // Now, let's expand the buffer above the number of current bytes but below the limit
            buffer = IoBuffer.Allocate(10);

            buffer.Put(Encoding.Default.GetBytes("012345"));
            buffer.Flip();
            newBuffer = buffer.Expand(8);
            Assert.AreEqual(8, newBuffer.Limit);
            Assert.AreEqual(10, newBuffer.Capacity);
            Assert.AreEqual(0, newBuffer.Position);

            // Last, expand the buffer above the limit
            buffer = IoBuffer.Allocate(10);

            buffer.Put(Encoding.Default.GetBytes("012345"));
            buffer.Flip();
            newBuffer = buffer.Expand(12);
            Assert.AreEqual(12, newBuffer.Limit);
            Assert.AreEqual(12, newBuffer.Capacity);
            Assert.AreEqual(0, newBuffer.Position);

            // Now, move forward in the buffer
            buffer = IoBuffer.Allocate(10);

            buffer.Put(Encoding.Default.GetBytes("012345"));
            buffer.Flip();
            buffer.Position = 4;

            // See if we can expand with a lower number of remaining bytes. We should not.
            newBuffer = buffer.Expand(2);
            Assert.AreEqual(6, newBuffer.Limit);
            Assert.AreEqual(10, newBuffer.Capacity);
            Assert.AreEqual(4, newBuffer.Position);

            // Expand above the current limit
            buffer = IoBuffer.Allocate(10);

            buffer.Put(Encoding.Default.GetBytes("012345"));
            buffer.Flip();
            buffer.Position = 4;
            newBuffer = buffer.Expand(3);
            Assert.AreEqual(7, newBuffer.Limit);
            Assert.AreEqual(10, newBuffer.Capacity);
            Assert.AreEqual(4, newBuffer.Position);

            // Expand above the current capacity
            buffer = IoBuffer.Allocate(10);

            buffer.Put(Encoding.Default.GetBytes("012345"));
            buffer.Flip();
            buffer.Position = 4;
            newBuffer = buffer.Expand(7);
            Assert.AreEqual(11, newBuffer.Limit);
            Assert.AreEqual(11, newBuffer.Capacity);
            Assert.AreEqual(4, newBuffer.Position);
        }

        [TestMethod]
        public void TestExpandPos()
        {
            IoBuffer buffer = IoBuffer.Allocate(10);

            buffer.Put(Encoding.Default.GetBytes("012345"));
            buffer.Flip();

            Assert.AreEqual(6, buffer.Remaining);

            // See if we can expand with a lower number of remaining bytes. We should not.
            IoBuffer newBuffer = buffer.Expand(3, 2);
            Assert.AreEqual(6, newBuffer.Limit);
            Assert.AreEqual(10, newBuffer.Capacity);
            Assert.AreEqual(0, newBuffer.Position);

            // Now, let's expand the buffer above the number of current bytes but below the limit
            buffer = IoBuffer.Allocate(10);
            buffer.Put(Encoding.Default.GetBytes("012345"));
            buffer.Flip();

            newBuffer = buffer.Expand(3, 5);
            Assert.AreEqual(8, newBuffer.Limit);
            Assert.AreEqual(10, newBuffer.Capacity);
            Assert.AreEqual(0, newBuffer.Position);

            // Last, expand the buffer above the limit
            buffer = IoBuffer.Allocate(10);

            buffer.Put(Encoding.Default.GetBytes("012345"));
            buffer.Flip();
            newBuffer = buffer.Expand(3, 9);
            Assert.AreEqual(12, newBuffer.Limit);
            Assert.AreEqual(12, newBuffer.Capacity);
            Assert.AreEqual(0, newBuffer.Position);

            // Now, move forward in the buffer
            buffer = IoBuffer.Allocate(10);

            buffer.Put(Encoding.Default.GetBytes("012345"));
            buffer.Flip();
            buffer.Position = 4;

            // See if we can expand with a lower number of remaining bytes. We should not be.
            newBuffer = buffer.Expand(5, 1);
            Assert.AreEqual(6, newBuffer.Limit);
            Assert.AreEqual(10, newBuffer.Capacity);
            Assert.AreEqual(4, newBuffer.Position);

            // Expand above the current limit
            buffer = IoBuffer.Allocate(10);

            buffer.Put(Encoding.Default.GetBytes("012345"));
            buffer.Flip();
            buffer.Position = 4;
            newBuffer = buffer.Expand(5, 2);
            Assert.AreEqual(7, newBuffer.Limit);
            Assert.AreEqual(10, newBuffer.Capacity);
            Assert.AreEqual(4, newBuffer.Position);

            // Expand above the current capacity
            buffer = IoBuffer.Allocate(10);

            buffer.Put(Encoding.Default.GetBytes("012345"));
            buffer.Flip();
            buffer.Position = 4;
            newBuffer = buffer.Expand(5, 6);
            Assert.AreEqual(11, newBuffer.Limit);
            Assert.AreEqual(11, newBuffer.Capacity);
            Assert.AreEqual(4, newBuffer.Position);
        }

        [TestMethod]
        public void TestAllocateNegative()
        {
            Exception expected = null;
            try
            {
                IoBuffer.Allocate(-1);
            }
            catch (Exception e)
            {
                expected = e;
            }
            Assert.IsTrue(expected is ArgumentException);
        }

        [TestMethod]
        public void TestFillByteSize()
        {
            int length = 1024 * 1020;
            IoBuffer buffer = IoBuffer.Allocate(length);
            buffer.Fill((byte)0x80, length);

            buffer.Flip();
            for (int i = 0; i<length; i++) {
                Assert.AreEqual((byte)0x80, buffer.Get());
            }
        }
    }
}
