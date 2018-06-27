using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Scarlet.Filters;

namespace UnitTest.FilterTesting
{
    [TestClass]
    public class TestLowPass
    {
        /// <summary> Tests basic construction of a low pass filter </summary>
        [TestMethod]
        public void TestInitialization()
        {
            LowPass<double> Test0 = new LowPass<double>();
            LowPass<double> Test1 = new LowPass<double>(-3);
            LowPass<double> Test2 = new LowPass<double>(3);
            Assert.ThrowsException<ArgumentException>(() => new LowPass<string>());
        }
    }
}
