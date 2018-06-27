using Microsoft.VisualStudio.TestTools.UnitTesting;
using Scarlet.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTest.FilterTesting
{
    [TestClass]
    public class TestLowPass
    {

        [TestMethod]
        public void TestInitialization()
        {
            LowPass<double> test0 = new LowPass<double>();
            LowPass<double> test1 = new LowPass<double>(-3);
            LowPass<double> test2 = new LowPass<double>(3);
            Assert.ThrowsException<ArgumentException>(()=>new LowPass<string>());
        }
    }
}
