using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Scarlet.Communications;

namespace UnitTest
{
    [TestClass]
    public class UnitTestPacketBuffer
    {
        private TestContext testContextInstance;

        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        public static Packet NewPacket(int length, int priority)
        {
            return new Packet(new Message((byte)priority, new byte[length]), false);
        }

        public static void AssertPacket(Packet p, int length, int priority)
        {
            Assert.AreEqual(p.Data.Payload.Length, length);
            Assert.AreEqual(p.Data.ID, priority);
        }

        [TestMethod]
        public void TestLeakyBucketEmpty()
        {
            QueueBuffer controller = new QueueBuffer();

            Assert.AreEqual(controller.Peek(), null);
            Assert.AreEqual(controller.Dequeue(), null);

            controller.Enqueue(NewPacket(10, 0));
            AssertPacket(controller.Peek(), 10, 0);
            AssertPacket(controller.Dequeue(), 10, 0);
            Assert.AreEqual(controller.Peek(), null);
            Assert.AreEqual(controller.Dequeue(), null);
        }

        [TestMethod]
        public void TestLeakyBucketCapacity()
        {
            QueueBuffer controller = new QueueBuffer();
            int capacity = 200000;
            for (int i = 0; i < capacity; i++)
                controller.Enqueue(NewPacket(10, 0));
            controller.Peek();
            for (int i = 0; i < capacity; i++)
                controller.Dequeue();
        }

        [TestMethod]
        public void TestLeakyBucketOrder()
        {
            QueueBuffer controller = new QueueBuffer();
            controller.Enqueue(NewPacket(1, 0));
            controller.Enqueue(NewPacket(2, 0));
            controller.Enqueue(NewPacket(3, 0));
            AssertPacket(controller.Dequeue(), 1, 0);
            AssertPacket(controller.Dequeue(), 2, 0);
            AssertPacket(controller.Dequeue(), 3, 0);
            Assert.AreEqual(controller.Dequeue(), null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestLeakyBucketPriority()
        {
            QueueBuffer controller = new QueueBuffer();
            controller.Enqueue(NewPacket(1, 0), 1);
        }

        public PriorityBuffer ConstructPriorityController()
        {
            QueueBuffer[] sub = new QueueBuffer[3];
            sub[0] = new QueueBuffer();
            sub[1] = new QueueBuffer();
            sub[2] = new QueueBuffer();
            return new PriorityBuffer(sub);
        }

        [TestMethod]
        public void TestPriorityControllerOrder()
        {
            PriorityBuffer controller = ConstructPriorityController();

            controller.Enqueue(NewPacket(1, 0), 0);
            controller.Enqueue(NewPacket(2, 2), 2);
            controller.Enqueue(NewPacket(3, 1), 1);
            controller.Enqueue(NewPacket(4, 1), 1);

            AssertPacket(controller.Dequeue(), 1, 0);
            AssertPacket(controller.Dequeue(), 3, 1);
            AssertPacket(controller.Dequeue(), 4, 1);
            AssertPacket(controller.Dequeue(), 2, 2);
            Assert.AreEqual(controller.Dequeue(), null);
        }

        [TestMethod]
        public void TestPriorityControllerPeek()
        {
            PriorityBuffer controller = ConstructPriorityController();

            controller.Enqueue(NewPacket(3, 1), 1);
            controller.Enqueue(NewPacket(4, 1), 1);

            AssertPacket(controller.Peek(), 3, 1);
            AssertPacket(controller.Dequeue(), 3, 1);
            AssertPacket(controller.Peek(), 4, 1);

            controller.Enqueue(NewPacket(1, 0), 0);

            AssertPacket(controller.Peek(), 1, 0);

            controller.Enqueue(NewPacket(2, 2), 2);

            AssertPacket(controller.Peek(), 1, 0);

            AssertPacket(controller.Dequeue(), 1, 0);
            AssertPacket(controller.Dequeue(), 4, 1);
            AssertPacket(controller.Dequeue(), 2, 2);

            Assert.AreEqual(controller.Peek(), null);
            Assert.AreEqual(controller.Dequeue(), null);
            Assert.AreEqual(controller.Peek(), null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestPriorityControllerTooLowPriority()
        {
            PriorityBuffer controller = ConstructPriorityController();
            controller.Enqueue(NewPacket(1, -1), -1);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestPriorityControllerTooHighPriority()
        {
            PriorityBuffer controller = ConstructPriorityController();
            controller.Enqueue(NewPacket(1, 3), 3);
        }

        public BandwidthControlBuffer ConstructBandwidthController(int[] badwidhtAllocation = null)
        {
            QueueBuffer[] sub = new QueueBuffer[3];
            sub[0] = new QueueBuffer();
            sub[1] = new QueueBuffer();
            sub[2] = new QueueBuffer();
            return new BandwidthControlBuffer(sub, badwidhtAllocation, 512);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestBandwidthControllerTooLowPriority()
        {
            BandwidthControlBuffer controller = ConstructBandwidthController();
            controller.Enqueue(NewPacket(1, -1), -1);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestBandwidthControllerTooHighPriority()
        {
            BandwidthControlBuffer controller = ConstructBandwidthController();
            controller.Enqueue(NewPacket(1, 3), 3);
        }

        [TestMethod]
        public void TestBandwidthControllerBasic()
        {
            BandwidthControlBuffer controller = ConstructBandwidthController();
            controller.Enqueue(NewPacket(1, 0), 0);
            AssertPacket(controller.Peek(), 1, 0);
            AssertPacket(controller.Dequeue(), 1, 0);
            Assert.AreEqual(controller.Peek(), null);
            Assert.AreEqual(controller.Dequeue(), null);
            Assert.AreEqual(controller.Peek(), null);


            controller.Enqueue(NewPacket(1, 0), 0);
            AssertPacket(controller.Peek(), 1, 0);
            AssertPacket(controller.Dequeue(), 1, 0);
            Assert.AreEqual(controller.Peek(), null);
            Assert.AreEqual(controller.Dequeue(), null);
            Assert.AreEqual(controller.Peek(), null);

            controller.Enqueue(NewPacket(1, 2), 2);
            AssertPacket(controller.Peek(), 1, 2);
            AssertPacket(controller.Dequeue(), 1, 2);
            Assert.AreEqual(controller.Peek(), null);
            Assert.AreEqual(controller.Dequeue(), null);
            Assert.AreEqual(controller.Peek(), null);

            controller.Enqueue(NewPacket(1, 1), 1);
            AssertPacket(controller.Peek(), 1, 1);
            AssertPacket(controller.Dequeue(), 1, 1);
            Assert.AreEqual(controller.Peek(), null);
            Assert.AreEqual(controller.Dequeue(), null);
            Assert.AreEqual(controller.Peek(), null);
        }

        public void BandwidthControllerPressureTest(Random r, int totalPacket)
        {
            int[] bandwidth = { 1, 2, 5 };
            int[] bandwidthCount = { 0, 0, 0 };
            int nPacket = 0;
            BandwidthControlBuffer controller = ConstructBandwidthController(bandwidth);

            for (int i = 0; i < totalPacket; i++)
            {
                controller.Enqueue(NewPacket(r.Next(1, 64), 0), 0);
                controller.Enqueue(NewPacket(r.Next(1, 64), 1), 1);
                controller.Enqueue(NewPacket(r.Next(1, 64), 2), 2);
            }

            for (int i = 0; i < totalPacket * 3 / 4; i++)
            {
                Packet next = controller.Dequeue();
                bandwidthCount[next.Data.ID] += next.GetLength();
                nPacket++;
            }

            if (totalPacket > 10000)
            {
                Assert.AreEqual((double)bandwidthCount[0] / bandwidthCount[1], (double)bandwidth[0] / bandwidth[1], 0.1);
                Assert.AreEqual((double)bandwidthCount[2] / bandwidthCount[1], (double)bandwidth[2] / bandwidth[1], 0.1);
            }

            testContextInstance.WriteLine("Actural bandwidth: " + string.Join(", ", bandwidthCount));

            while (controller.Peek() != null)
            {
                controller.Dequeue();
                nPacket++;
            }

            Assert.AreEqual(nPacket, totalPacket * 3);
        }

        [TestMethod]
        public void TestBandwidthControllerPressureTest()
        {
            Random r = new Random();
            BandwidthControllerPressureTest(r, 20000);

            for (int i = 0; i < 2000; i++)
                BandwidthControllerPressureTest(r, 10);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestGenericControllerTooLowPriority()
        {
            GenericController controller = new GenericController();
            controller.Enqueue(NewPacket(1, -1), -1);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestGenericControllerTooHighPriority()
        {
            GenericController controller = new GenericController();
            controller.Enqueue(NewPacket(1, 3), 5);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestGenericControllerTooLowPriority2()
        {
            GenericController controller = new GenericController();
            controller.Enqueue(NewPacket(1, -1), (GenericController.Priority)(-1));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestGenericControllerTooHighPriority2()
        {
            GenericController controller = new GenericController();
            controller.Enqueue(NewPacket(1, 3), (GenericController.Priority)5);
        }


        [TestMethod]
        public void TestGenericController()
        {
            GenericController controller = new GenericController();
            controller.Enqueue(NewPacket(1, 0), GenericController.Priority.MEDIUM);
            controller.Enqueue(NewPacket(2, 0), GenericController.Priority.LOW);
            controller.Enqueue(NewPacket(3, 0), GenericController.Priority.HIGH);
            controller.Enqueue(NewPacket(4, 0), GenericController.Priority.EMERGENT);
            controller.Enqueue(NewPacket(5, 0), GenericController.Priority.LOWEST);

            AssertPacket(controller.Peek(), 4, 0);
            AssertPacket(controller.Dequeue(), 4, 0);
            AssertPacket(controller.Dequeue(), 3, 0);
            AssertPacket(controller.Dequeue(), 1, 0);
            AssertPacket(controller.Peek(), 2, 0);
            AssertPacket(controller.Dequeue(), 2, 0);
            AssertPacket(controller.Peek(), 5, 0);
            AssertPacket(controller.Dequeue(), 5, 0);

            Assert.AreEqual(controller.Peek(), null);
            Assert.AreEqual(controller.Dequeue(), null);
            Assert.AreEqual(controller.Peek(), null);
        }
    }
}
