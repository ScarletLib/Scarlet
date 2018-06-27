using System;
using Scarlet.Utilities;

namespace Scarlet.Filters
{
    /// <summary> The Low Pass filter is intended for use as a low pass filter with time constant <see cref="LPFk"/>. </summary>
    /// <remarks>
    /// Implementation Details:
    /// * Construct Low Pass filter given a time constant
    ///   <see cref="LPFk"/>
    /// * Iteratively add values into the filter
    ///   using <see cref="Feed(T)"/>
    /// * Get the filter output by calling
    ///   <see cref="GetOutput"/>
    /// </remarks>
    /// <typeparam name="T"> A type, which must be a numeric. </typeparam>
    public class LowPass<T> : IFilter<T> where T : IComparable
    {
        private readonly double SteadyStateEpsilon;

        private T Output;
        private T LastValue; // Last filter value (internal use)

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

        /// <summary> Constructs a low pass filter with time constant <see cref="LPFk"/>. </summary>
        /// <param name="LPFk"> Low Pass Filter Time Constant. </param>
        /// <param name="SteadyStateEpsilon"> Allowable difference in output to be considered a steady state system. </param>
        public LowPass(double LPFk = 0.25, double SteadyStateEpsilon = 0)
        {
            // Assert that T is numeric
            if (!UtilData.IsNumericType(typeof(T))) { throw new ArgumentException("Cannot create filter of non-numeric type: " + typeof(T).ToString()); } 

            this.Output = default(T);
            this.LPFk = LPFk;
            this.SteadyStateEpsilon = SteadyStateEpsilon;
            this.Reset();
            this.Feed(default(T));
        }

        /// <summary> Feeds a value into the filter. </summary>
        /// <param name="Input"> Value to feed into the filter. </param>
        public void Feed(T Input)
        {
            // Store values as dynamic for manipulation
            dynamic LastOutputDyn = Output;
            dynamic InputDyn = Input;

            // Find output by LPF equation: y(t) = y[t-1] * (1 - c) + x(t) * c, 
            // where c is the time constant and x(t) is the filter input at that time.
            dynamic OutputDyn = ((LastOutputDyn * ((1 - this.LPFk) + InputDyn) * this.LPFk));

            // Set iterative variables
            this.Output = (T)OutputDyn;
            this.LastValue = Input;
        }

        /// <summary> Feeds filter with specified rate. Not used for average filter. </summary>
        /// <param name="Input"> Value to feed into the filer. </param>
        /// <param name="Rate"> Current rate to feed into the filter. </param>
        public void Feed(T Input, T Rate)
        {
            // Low Pass Filter independent of rate.
            this.Feed(Input); 
        }

        /// <summary> Resets the low pass filter to the default value of <see cref="T"/>.</summary>
        public void Reset() { this.LastValue = default(T); }

        /// <summary> Computes whether or not the low pass filter is in steady state </summary>
        /// <returns> Returns whether or not the filter is in steady state </returns>
        public bool IsSteadyState()
        {
            // System is in steady state if the output of the filter and the input
            // are within SteadyStateEpsilon of each other
            return Math.Abs((dynamic)LastValue - (dynamic)Output) <= SteadyStateEpsilon;
        }

        public T GetOutput() { return this.Output; }
    }
}
