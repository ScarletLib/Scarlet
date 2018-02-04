using Microsoft.VisualStudio.TestTools.UnitTesting;
using Scarlet.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace UnitTest
{
    [TestClass]
    public class StateStoreTest
    {
        private TestContext TestContextInstance;

        public TestContext TestContext
        {
            get { return TestContextInstance; }
            set { TestContextInstance = value; }
        }

        [TestMethod]
        public void BasicStateStoreTest()
        {
            StateStore.Start("StateStoreTests");
            StateStore.Set("TestValue-BASIC", "TestValue-1");
            StateStore.Set("TestValue-BASIC", "TestValue-2");
            StateStore.Set("TestValue-BASIC", "TestValue-0");
            StateStore.Set("TestValue-BASIC", "TestValue-3");
            StateStore.Set("TestValue-BASIC", "TestValue-4");
            StateStore.Set("TestValue-BASIC", "TestValue-5");
            StateStore.Set("TestValue-BASIC", "TestValue-6");
            StateStore.Set("TestValue-BASIC", "TestValue-7");
            StateStore.Set("TestValue-BASIC", "TestValue-8");
            StateStore.Set("TestValue-BASIC", "TestValue-9");
            StateStore.Set("TestValue-BASIC", "TestValue-10");

            Assert.AreEqual("TestValue-10", StateStore.Get("TestValue"));

            Assert.AreEqual(null, StateStore.Get("ThisValueIsNotInTheDictionary"));

            StateStore.Save();

        }

        private void SetValueForThreadTest(object Value)
        {
            Thread.Sleep(1500);
            StateStore.Set("TestValue-THREADING", (string)Value);
        }

        [TestMethod]
        public void StateStoreThreadTesting()
        {
            StateStore.Start("StateStoreTests");
            Thread TestThread1 = new Thread(new ParameterizedThreadStart(SetValueForThreadTest));
            Thread TestThread2 = new Thread(new ParameterizedThreadStart(SetValueForThreadTest));
            TestThread1.Start("Test0");
            TestThread2.Start("Test1");
            StateStore.Save();
            object Value1 = null;
            object Value2 = null;
            Thread TestThread3 = new Thread(() => { Value1 = StateStore.Get("TestValue-THREADING"); });
            Thread TestThread4 = new Thread(() => { Value2 = StateStore.Get("TestValue-THREADING"); });
            TestThread3.Start();
            TestThread4.Start();
            TestThread3.Join();
            TestThread4.Join();
            Assert.AreEqual(Value1, Value2);
            Assert.IsTrue(Value1 != null);
        }

    }
}
