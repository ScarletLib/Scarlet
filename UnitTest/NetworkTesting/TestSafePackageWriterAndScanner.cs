using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Scarlet.Communications;

namespace UnitTest
{
    [TestClass]
    public class TestSafePacketWriterAndScanner
    {
        private bool Equality(byte[] a1, byte[] b1)
        {
            int i;
            if (a1.Length == b1.Length)
            {
                i = 0;
                while (i < a1.Length && (a1[i] == b1[i])) //Earlier it was a1[i]!=b1[i]
                {
                    i++;
                }
                if (i == a1.Length)
                {
                    return true;
                }
            }

            return false;
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestDifferentTypeException()
        {
            SafePacketWriter writer = new SafePacketWriter(0x10, true);
            writer.Put(true).Put(false);

            SafePacketScanner scanner = new SafePacketScanner(writer.Packet);
            scanner.NextInt();
        }

        [TestMethod]
        public void TestBool()
        {
            SafePacketWriter writer = new SafePacketWriter(0x10, true);
            writer.Put(true).Put(false);

            SafePacketScanner scanner = new SafePacketScanner(writer.Packet);
            Assert.AreEqual(true, scanner.NextBool());
            Assert.AreEqual(false, scanner.NextBool());
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestBoolException()
        {
            SafePacketWriter writer = new SafePacketWriter(0x10, true);
            writer.Put(true);
            writer.Put(false);

            SafePacketScanner scanner = new SafePacketScanner(writer.Packet);
            scanner.NextBool();
            scanner.NextBool();
            scanner.NextBool();
        }

        [TestMethod]
        public void TestChar()
        {
            SafePacketWriter writer = new SafePacketWriter(0x10, false);
            writer.Put('A').Put('0');

            SafePacketScanner scanner = new SafePacketScanner(writer.Packet);
            Assert.AreEqual('A', scanner.NextChar());
            Assert.AreEqual('0', scanner.NextChar());
        }

        [TestMethod]
        public void TestDouble()
        {
            SafePacketWriter writer = new SafePacketWriter(0x10, false);
            writer.Put(3e-10).Put(45.0);

            SafePacketScanner scanner = new SafePacketScanner(writer.Packet);
            Assert.AreEqual(3e-10, scanner.NextDouble());
            Assert.AreEqual(45.0, scanner.NextDouble());
        }

        [TestMethod]
        public void TestFloat()
        {
            SafePacketWriter writer = new SafePacketWriter(0x10, false);
            writer.Put((float)3e-10);
            writer.Put((float)46.0);

            SafePacketScanner scanner = new SafePacketScanner(writer.Packet);
            Assert.AreEqual((float)3e-10, scanner.NextFloat());
            Assert.AreEqual((float)46.0, scanner.NextFloat());
        }

        [TestMethod]
        public void TestInt()
        {
            SafePacketWriter writer = new SafePacketWriter(0x10, false);
            writer.Put(-230).Put(1234567890);

            SafePacketScanner scanner = new SafePacketScanner(writer.Packet);
            Assert.AreEqual(-230, scanner.NextInt());
            Assert.AreEqual(1234567890, scanner.NextInt());
        }

        [TestMethod]
        public void TestString()
        {
            SafePacketWriter writer = new SafePacketWriter(0x10, false);
            writer.Put("hello world!").Put("TestString");

            SafePacketScanner scanner = new SafePacketScanner(writer.Packet);
            Assert.AreEqual("hello world!", scanner.NextString());
            Assert.AreEqual("TestString", scanner.NextString());
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestStringException()
        {
            SafePacketWriter writer = new SafePacketWriter(0x10, false);
            writer.Put("hello world");

            SafePacketScanner scanner = new SafePacketScanner(writer.Packet);
            Assert.AreEqual("hello world", scanner.NextString());
            scanner.NextString();
        }

        [TestMethod]
        public void TestIntAndString()
        {
            SafePacketWriter writer = new SafePacketWriter(0x10, false);
            writer.Put(123).Put("hello world! TestString");

            SafePacketScanner scanner = new SafePacketScanner(writer.Packet);
            Assert.AreEqual(123, scanner.NextInt());
            Assert.AreEqual("hello world! TestString", scanner.NextString());
        }

        [TestMethod]
        public void TestBytes()
        {
            SafePacketWriter writer = new SafePacketWriter(0x10, false);
            writer.Put(new byte[5] { 0x12, 0x34, 0x56, 0x78, 0xaf });
            writer.Put(new byte[3] { 0xbe, 0xef, 0xcd });

            SafePacketScanner scanner = new SafePacketScanner(writer.Packet);
            Assert.IsTrue(Equality(new byte[5] { 0x12, 0x34, 0x56, 0x78, 0xaf }, scanner.NextBytes()));
            Assert.IsTrue(Equality(new byte[3] { 0xbe, 0xef, 0xcd }, scanner.NextBytes()));
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestBytesException()
        {
            SafePacketWriter writer = new SafePacketWriter(0x10, false);
            writer.Put(new byte[5] { 0x12, 0x34, 0x56, 0x78, 0xaf });

            SafePacketScanner scanner = new SafePacketScanner(writer.Packet);
            Assert.IsTrue(Equality(new byte[5] { 0x12, 0x34, 0x56, 0x78, 0xaf }, scanner.NextBytes()));
            scanner.NextBytes();
        }

        [TestMethod]
        public void TestByte()
        {
            SafePacketWriter writer = new SafePacketWriter(0x10, false);
            writer.Put((byte)0x12).Put((byte)0xcf);

            SafePacketScanner scanner = new SafePacketScanner(writer.Packet);
            Assert.AreEqual(0x12, scanner.NextByte());
            Assert.AreEqual(0xcf, scanner.NextByte());
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestByteException()
        {
            SafePacketWriter writer = new SafePacketWriter(0x10, false);
            writer.Put((byte)0x12).Put((byte)0xcf);

            SafePacketScanner scanner = new SafePacketScanner(writer.Packet);
            Assert.AreEqual(0x12, scanner.NextByte());
            Assert.AreEqual(0xcf, scanner.NextByte());
            scanner.NextByte();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestEmptyScanner()
        {
            SafePacketWriter writer = new SafePacketWriter(0x10, false);

            SafePacketScanner scanner = new SafePacketScanner(writer.Packet);
            scanner.NextBytes();
        }
    }
}
