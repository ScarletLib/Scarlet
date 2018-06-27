using System;
using Scarlet.Utilities;

namespace Scarlet.Filters
{
    /// <summary> 
    /// The Average filter is intended for use as an average-gathering system, using a rolling average 
    /// with "roll-length" <c>FilterCount</c> or continuous average.
    /// </summary>
    /// <remarks>
    /// Intended Usage:
    /// * Construct Average filter given a rolling
    ///   filter length, <see cref="FilterCount">FilterCount</see>. Setting
    ///   FilterCount to null puts the filter into
    ///   continuous mode.
    /// * Iteratively add values into the filter
    ///   using <see cref="Feed(T)">Feed()</see>
    /// * Get the filter output by calling
    ///   <see cref="GetOutput">YourFilterInstance.GetOutput()</see>
    /// </remarks>
    /// <typeparam name="T"> A type, which must be a numeric. </typeparam>
    public class Average<T> : IFilter<T> where T : IComparable
    {
        private readonly int? FilterCount;
        private T Output; // Filter output
        private dynamic[] AverageArray; // Stored average array
        private dynamic CurSum; // Current sum of the average array 
        private int Index, // The current index of the average filter (when in roll mode)
                    Iterations; // The number of iterations through the average filter
        private int NumCyclesAverageSame; // Number of cycles that the average has stayed the same

        /// <summary> Construct an average filter with given roll-length; give null for continuous mode.</summary>
        /// <remarks> To use this as a continuous (non-rolling) average filter, set <c>FilterCount</c> to null </remarks>
        /// <param name="FilterCount"> 
        /// Roll length for the average filter. 
        /// If this value is null, the filter is set to continuous mode (as opposed to a roll mode). 
        /// </param>
        public Average(int? FilterCount = null)
        {
            // Assert that T is a numeric type
            if (!UtilData.IsNumericType(typeof(T)))
            {
                Log.Output(Log.Severity.ERROR, Log.Source.OTHER, "Average filter cannot be instantiated with non-numeric type.");
                throw new ArgumentException("Cannot create filter of non-numeric type: " + typeof(T).ToString());
            } 

            this.Output = default(T);
            this.CurSum = 0;
            this.Index = 0;
            this.Iterations = 0;
            this.FilterCount = FilterCount;

            // Use array if in roll-mode
            if (this.FilterCount != null)
            {
                this.AverageArray = new dynamic[Math.Abs((int)this.FilterCount)];

                // Initialize average array to defaults
                this.InitializeArray();
            }
            this.NumCyclesAverageSame = 0;
            this.Feed(default(T));
        }

        /// <summary> Feeds a value into the filter. </summary>
        /// <param name="Input"> Value to feed into the filter. </param>
        public void Feed(T Input)
        {
            // Increase number of iterations by 1
            this.Iterations++;

            // Value to divide sum by at the end of the computation
            int Divisor = this.Iterations;

            // Store input as a dynamic type since we know T is a numeric
            dynamic dynamicInput = Input;

            // Add current value to sum
            this.CurSum += dynamicInput;

            // Check if in continuous or roll mode
            if (this.FilterCount != null)
            {
                // Subtract current array index value from sum
                this.CurSum -= this.AverageArray[this.Index];

                // Store curent value in old spot
                this.AverageArray[this.Index] = dynamicInput;

                // Increment index. Go back to zero if index + 1 == filterCount
                this.Index = (this.Index + 1) % (int)this.FilterCount;

                // Change the divisor to the filter count if the number of iterations exceeds the width of the array.
                if (this.Iterations > this.FilterCount) { Divisor = (int)this.FilterCount; }
            }

            // Keep temporary track of the last output
            T LastOutput = this.Output;

            // Divide output by either number of iterations or filter length
            this.Output = this.CurSum / Divisor;

            // Add one to the count if the average was the same after this cycle, otherwise reset it
            if ((dynamic)LastOutput == (dynamic)this.Output) { NumCyclesAverageSame++; } else { NumCyclesAverageSame = 0; }
        }

        /// <summary> Rate is irrelevant to average filter, so this is no different than using <see cref="Feed(T)">Feed()</see>. </summary>
        /// <param name="Input"> Value to feed into the filer. </param>
        /// <param name="Rate"> This value is ignored. </param>
        public void Feed(T Input, T Rate) { this.Feed(Input); }

        /// <summary> Computes whether or not the average filter is in steady state. </summary>
        /// <remarks> If in continuous mode, the filter is never considered to be in steady state. </remarks>
        /// <returns> Whether or not filter is in steady state. </returns>
        public bool IsSteadyState() { return FilterCount != null && NumCyclesAverageSame >= FilterCount; }

        public T GetOutput() { return this.Output; }

        /// <summary> Initializes dynamic number array to all zeros. </summary>
        private void InitializeArray()
        {
            for (int i = 0; i < this.AverageArray.Length; i++)
            {
                this.AverageArray[i] = 0;
            }
        }
    }
}
