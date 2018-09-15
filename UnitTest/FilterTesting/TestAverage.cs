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
            int TestInt = 2;
            Average<double> Test0 = new Average<double>();
            Average<double> Test1 = new Average<double>(TestInt);
            Average<float> Test2 = new Average<float>(null);
            Assert.ThrowsException<ArgumentException>(() => new Average<string>());
        }

        /// <summary> Tests the roll mode functionality of the average filter. </summary>
        [TestMethod]
        public void TestRollMode()
        {
            int LengthTest = 5;
            Average<double> TestAverage = new Average<double>(LengthTest);
            double[] FirstSet = GetRandDoubleArray(LengthTest);
            double[] SecondSet = GetRandDoubleArray(LengthTest);

            double CurSum = 0.0;
            for (int i = 0; i < FirstSet.Length; i++)
            {
                TestAverage.Feed(FirstSet[i]);
                CurSum += FirstSet[i];
                Assert.IsTrue(Math.Abs((CurSum / (i + 1)) - TestAverage.GetOutput()) <= 2 * double.Epsilon);
            }
            for (int i = 0; i < SecondSet.Length; i++) { TestAverage.Feed(SecondSet[i]); }
            Assert.AreEqual(GetAverage(SecondSet), TestAverage.GetOutput());
        }

        /// <summary> Tests the continuous mode functionality of the average filter </summary>
        [TestMethod]
        public void TestContinuousMode()
        {
            int LengthTest = 50;
            Average<double> TestAverage = new Average<double>(null);
            double[] set = GetRandDoubleArray(LengthTest);

            double CurSum = 0.0;
            for (int i = 0; i < set.Length; i++)
            {
                TestAverage.Feed(set[i]);
                CurSum += set[i];
                Assert.IsTrue(Math.Abs((CurSum / (i + 1)) - TestAverage.GetOutput()) <= 2 * double.Epsilon);
            }
            Assert.AreEqual(GetAverage(set), TestAverage.GetOutput());
        }

        /// <summary> Gets the average value of a set of doubles </summary>
        /// <param name="Vals"> Set of doubles to average </param>
        /// <returns> Average of <see cref="Vals"/> </returns>
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
            Random Random = new Random();
            double[] ReturnArray = new double[Length];
            for (int i = 0; i < Length; i++) { ReturnArray[i] = (Random.NextDouble() - 0.5) * Max * 2.0; }
            return ReturnArray;
        }
    }
}
