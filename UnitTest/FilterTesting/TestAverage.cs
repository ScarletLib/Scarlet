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
    public class AverageTest
    {

        [TestMethod]
        public void TestInitialization()
        {
            int testInt = 2;
            Average<double> test0 = new Average<double>();
            Average<double> test1 = new Average<double>(testInt);
            Average<float> test2 = new Average<float>(null);
            Assert.ThrowsException<ArgumentException>(() => new Average<string>());
        }

        [TestMethod]
        public void TestRollMode()
        {
            int LengthTest = 5;
            Average<double> testAverage = new Average<double>(LengthTest);
            double[] firstSet = GetRandDoubleArray(LengthTest);
            double[] secondSet = GetRandDoubleArray(LengthTest);

            double CurSum = 0.0;
            for (int i = 0; i < firstSet.Length; i++)
            {
                testAverage.Feed(firstSet[i]);
                CurSum += firstSet[i];
                Assert.IsTrue(Math.Abs(CurSum / (i + 1) - testAverage.GetOutput()) <= 2 * double.Epsilon);
            }
            for (int i = 0; i < secondSet.Length; i++) { testAverage.Feed(secondSet[i]); }
            Assert.AreEqual(GetAverage(secondSet), testAverage.GetOutput());
        }

        [TestMethod]
        public void TestContinuousMode()
        {
            int LengthTest = 50;
            Average<double> testAverage = new Average<double>(null);
            double[] set = GetRandDoubleArray(LengthTest);

            double CurSum = 0.0;
            for (int i = 0; i < set.Length; i++)
            {
                testAverage.Feed(set[i]);
                CurSum += set[i];
                Assert.IsTrue(Math.Abs(CurSum / (i + 1) - testAverage.GetOutput()) <= 2 * double.Epsilon);
            }
            Assert.AreEqual(GetAverage(set), testAverage.GetOutput());
        }

        private double GetAverage(double[] Vals)
        {
            double Sum = 0.0;
            for (int i = 0; i < Vals.Length; i++) { Sum += Vals[i]; }
            return Sum / Vals.Length;
        }

        private double[] GetRandDoubleArray(int Length, double Max = 1.0)
        {
            Random random = new Random();
            double[] retArr = new double[Length];
            for (int i = 0; i < Length; i++) { retArr[i] = random.NextDouble() * Max; }
            return retArr;
        }

    }
}
