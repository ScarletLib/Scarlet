namespace Scarlet.IO.Transforms
{
    /// <summary> Data transformer for <see cref="IAnalogueOut"/> data streams. </summary>
    public class AnalogueOutTransform : IAnalogueOut
    {
        private readonly TransformTypes.DoubleTransform Range, Output;
        private readonly IAnalogueOut Base;

        /// <summary> Sets up the data transformer. </summary>
        /// <param name="Output"> The underlying output whose values should be transformed. </param>
        /// <param name="RangeTransform"> The mathematical expression to use when transforming range information (Output to Parent). </param>
        /// <param name="OutputTransform"> The mathematical expression to use when transforming output value information (Parent to Output). </param>
        public AnalogueOutTransform(IAnalogueOut Output, TransformTypes.DoubleTransform RangeTransform, TransformTypes.DoubleTransform OutputTransform)
        {
            this.Range = RangeTransform;
            this.Output = OutputTransform;
            this.Base = Output;
        }

        /// <summary> Gets the maximum outputtable value. </summary>
        /// <returns> The maximum value this output can send, in Volts, transformed. </returns>
        public double GetRange() { return this.Range(this.Base.GetRange()); }

        /// <summary> Sets the current output level. </summary>
        /// <param name="Output"> The desired output, in Volts, which will be transformed. </param>
        public void SetOutput(double Output) { this.Base.SetOutput(this.Output(Output)); }

        public void Dispose() { this.Base.Dispose(); }
    }
}
