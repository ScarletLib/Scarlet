﻿using System;
using Scarlet.Utilities;

namespace Scarlet.Filters
{
    /// <summary> The Low Pass filter is intended for use as an average-gathering system, using a low pass filter with time constant <c>LPFk</c>. </summary>
    /// <remarks>
    /// Implementation Details:
    /// 
    /// *Construct Low Pass filter given a time constant
    ///  <c>LPFk</c>
    /// 
    /// *Iteratively add values into the filter
    ///  using <c>Feed(T Input)</c>
    /// 
    /// *Get the filter output by calling
    ///  <c>YourFilterInstance.GetOutput</c>
    /// </remarks>
    /// <typeparam name="T"> A type, which must be a numeric. </typeparam>
    public class LowPass<T> : IFilter<T> where T : IComparable
    {
        private T Output;
        private T LastValue; // Last filter value (internal use)
        private double SteadyStateEpsilon;

        private double P_LPFk;
        public double LPFk
        {
            get { return this.P_LPFk; }
            set
            {
                this.P_LPFk = value;
                if (value > 1) { this.P_LPFk = 1; }
                if (value < 0) { this.P_LPFk = 0; }
            }
        } // Time constant for the Low Pass Filter from 0 to 1


        /// <summary> Constructs a low pass filter with time constant <c>LPFk</c>. </summary>
        /// <param name="LPFk"> Low Pass Filter Time Constant. </param>
        /// <param name="SteadyStateEpsilon"> Allowable difference in output to be considered a steady state system. </param>
        public LowPass(double LPFk = 0.25, double SteadyStateEpsilon = 0)
        {
            if (!UtilData.IsNumericType(typeof(T)))
            {
                Log.Output(Log.Severity.ERROR, Log.Source.OTHER, "Low-pass filter cannot be instantiated with non-numeric type.");
                throw new ArgumentException("Cannot create filter of non-numeric type: " + typeof(T).ToString());
            } // We can now assert that T is a numeric

            this.Output = default(T);
            this.LPFk = LPFk;
            this.SteadyStateEpsilon = SteadyStateEpsilon;
            this.Reset();
        }

        /// <summary> Feeds a value into the filter. </summary>
        /// <param name="Input"> Value to feed into the filter. </param>
        public void Feed(T Input)
        {
            // Store values as dynamic for manipulation
            dynamic _dLastOutput = Output;
            dynamic _dInput = Input;
            // Find output by LPF equation: y(t) = y[t-1] * (1 - c) + x(t) * c, 
            // where c is the time constant and x(t) is the filter input at that time.
            dynamic _dOutput = ((_dLastOutput * (1 - this.LPFk) + _dInput * this.LPFk));
            // Set iterative variables
            this.Output = (T)_dOutput;
            this.LastValue = Input;
        }
        /// <summary> Feeds filter with specified rate. Not used for average filter. </summary>
        /// <param name="Input"> Value to feed into the filer. </param>
        /// <param name="Rate"> Current rate to feed into the filter. </param>
        public void Feed(T Input, T Rate)
        {
            this.Feed(Input); // Low Pass Filter independent of rate.
        }

        /// <summary> Resets the low pass filter to the default value of <c>T</c> </summary>
        public void Reset() { this.LastValue = default(T); }

        /// <summary> Computes whether or not the low pass filter is in steady state</summary>
        /// <returns> Returns whether or not the filter is in steady state</returns>
        public bool IsSteadyState()
        {
            // System is in steady state if the output of the filter and the input
            // are within SteadyStateEpsilon of each other.
            return Math.Abs((dynamic)LastValue - (dynamic)Output) <= SteadyStateEpsilon;
        }

        public T GetOutput() { return this.Output; }

    }
}
