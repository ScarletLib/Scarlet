using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Scarlet.Communications;

namespace UnitTest
{
    [TestClass]
    public class TestPacketWriterAndScanner
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
        public void TestBool()
        {
            PacketWriter writer = new PacketWriter(0x10, true);
            writer.Put(true);
            writer.Put(false);

            PacketScanner scanner = new PacketScanner(writer.Packet);
            Assert.AreEqual(true, scanner.NextBool());
            Assert.AreEqual(false, scanner.NextBool());
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestBoolException()
        {
            PacketWriter writer = new PacketWriter(0x10, true);
            writer.Put(true);
            writer.Put(false);

            PacketScanner scanner = new PacketScanner(writer.Packet);
            scanner.NextBool();
            scanner.NextBool();
            scanner.NextBool();
        }

        [TestMethod]
        public void TestChar()
        {
            PacketWriter writer = new PacketWriter(0x10, false);
            writer.Put('A');
            writer.Put('0');

            PacketScanner scanner = new PacketScanner(writer.Packet);
            Assert.AreEqual('A', scanner.NextChar());
            Assert.AreEqual('0', scanner.NextChar());
        }

        [TestMethod]
        public void TestDouble()
        {
            PacketWriter writer = new PacketWriter(0x10, false);
            writer.Put(3e-10);
            writer.Put(45.0);

            PacketScanner scanner = new PacketScanner(writer.Packet);
            Assert.AreEqual(3e-10, scanner.NextDouble());
            Assert.AreEqual(45.0, scanner.NextDouble());
        }

        [TestMethod]
        public void TestFloat()
        {
            PacketWriter writer = new PacketWriter(0x10, false);
            writer.Put((float) 3e-10);
            writer.Put((float) 46.0);

            PacketScanner scanner = new PacketScanner(writer.Packet);
            Assert.AreEqual((float) 3e-10, scanner.NextFloat());
            Assert.AreEqual((float) 46.0, scanner.NextFloat());
        }

        [TestMethod]
        public void TestInt()
        {
            PacketWriter writer = new PacketWriter(0x10, false);
            writer.Put(-230);
            writer.Put(1234567890);

            PacketScanner scanner = new PacketScanner(writer.Packet);
            Assert.AreEqual(-230, scanner.NextInt());
            Assert.AreEqual(1234567890, scanner.NextInt());
        }

        [TestMethod]
        public void TestString()
        {
            PacketWriter writer = new PacketWriter(0x10, false);
            writer.Put("hello world! TestString");

            PacketScanner scanner = new PacketScanner(writer.Packet);
            Assert.AreEqual("hello world! TestString", scanner.NextString());
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestStringException()
        {
            PacketWriter writer = new PacketWriter(0x10, false);
            writer.Put("hello world");

            PacketScanner scanner = new PacketScanner(writer.Packet);
            Assert.AreEqual("hello world", scanner.NextString());
            scanner.NextString();
        }

        [TestMethod]
        public void TestIntAndString()
        {
            PacketWriter writer = new PacketWriter(0x10, false);
            writer.Put(123);
            writer.Put("hello world! TestString");

            PacketScanner scanner = new PacketScanner(writer.Packet);
            Assert.AreEqual(123, scanner.NextInt());
            Assert.AreEqual("hello world! TestString", scanner.NextString());
        }

        [TestMethod]
        public void TestByte()
        {
            PacketWriter writer = new PacketWriter(0x10, false);
            writer.Put(new byte[5] { 0x12, 0x34, 0x56, 0x78, 0xaf });

            PacketScanner scanner = new PacketScanner(writer.Packet);
            Assert.IsTrue(Equality(new byte[5] { 0x12, 0x34, 0x56, 0x78, 0xaf }, scanner.NextBytes()));
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestByteException()
        {
            PacketWriter writer = new PacketWriter(0x10, false);
            writer.Put(new byte[5] { 0x12, 0x34, 0x56, 0x78, 0xaf });

            PacketScanner scanner = new PacketScanner(writer.Packet);
            Assert.IsTrue(Equality(new byte[5] { 0x12, 0x34, 0x56, 0x78, 0xaf }, scanner.NextBytes()));
            scanner.NextBytes();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TestEmptyScanner()
        {
            PacketWriter writer = new PacketWriter(0x10, false);

            PacketScanner scanner = new PacketScanner(writer.Packet);
            scanner.NextBytes();
        }
    }
}
