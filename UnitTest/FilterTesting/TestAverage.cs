using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Scarlet.Filters;

namespace UnitTest.FilterTesting
{
    [TestClass]
    public class AverageTest
    {
        /// <summary> Tests basic construction of the Average filter. </summary>
        [TestMethod]
        public void TestInitialization()
        {
            int testInt = 2;
            Average<double> test0 = new Average<double>();
            Average<double> test1 = new Average<double>(testInt);
            Average<float> test2 = new Average<float>(null);
            Assert.ThrowsException<ArgumentException>(() => new Average<string>());
        }

        /// <summary> Tests the roll mode functionality of the average filter. </summary>
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
                Assert.IsTrue(Math.Abs((CurSum / (i + 1)) - testAverage.GetOutput()) <= 2 * double.Epsilon);
            }
            for (int i = 0; i < secondSet.Length; i++) { testAverage.Feed(secondSet[i]); }
            Assert.AreEqual(GetAverage(secondSet), testAverage.GetOutput());
        }

        /// <summary> Tests the continuous mode functionality of the average filter </summary>
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
                Assert.IsTrue(Math.Abs((CurSum / (i + 1)) - testAverage.GetOutput()) <= 2 * double.Epsilon);
            }
            Assert.AreEqual(GetAverage(set), testAverage.GetOutput());
        }

        /// <summary> Gets the average value of a set of doubles </summary>
        /// <param name="Vals"> Set of doubles to average </param>
        /// <returns> Average of <see cref="Vals">Vals</see> </returns>
        private double GetAverage(double[] Vals)
        {
            double Sum = 0.0;
            for (int i = 0; i < Vals.Length; i++) { Sum += Vals[i]; }
            return Sum / Vals.Length;
        }

        /// <summary> Returns a random double array </summary>
        /// <param name="Length"> Desired length of the new array </param>
        /// <param name="Max"> Max element value (in the positive and negative range) </param>
        /// <returns> The new array with the desired length and max element value </returns>
        private double[] GetRandDoubleArray(int Length, double Max = 1.0)
        {
            Random random = new Random();
            double[] retArr = new double[Length];
            for (int i = 0; i < Length; i++) { retArr[i] = (random.NextDouble() - 0.5) * Max * 2.0; }
            return retArr;
        }
    }
}
