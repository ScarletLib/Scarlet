using System;

namespace Scarlet.Filters
{
    /// <summary>
    ///  This is an interface meant to wrap all filters in the Filters namespace.
    ///  
    ///  * * * Some Filters require use of just the Feed(T Input) method. Others require a given Feed(T Input, T Rate).
    ///        See the documentation for the given filter to determine necessary information.
    /// </summary>
    public interface IFilter<T> where T : IComparable
    {
        /// <summary>
        /// Feeds the filter with a given input and
        /// rate. Typically if not using Rate the implementation of this
        /// method will call just Feed(Input) and ignore rate.
        /// </summary>
        /// <param name="Input">Filter input</param>
        /// <param name="Rate">Rate input</param>
        void Feed(T Input, T Rate);

        /// <summary>
        /// Feeds the filter with a given input.
        /// </summary>
        /// <param name="Input">Filter input</param>
        void Feed(T Input); // Feeds filter with just an input.
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        T GetOutput(); // Returns the output of the filter

        /// <summary>
        /// Computes whether or not the filter
        /// is in a steady state.
        /// </summary>
        /// <returns>The steady state </returns>
        bool IsSteadyState();
    }
}
