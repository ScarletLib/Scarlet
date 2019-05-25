namespace Scarlet.IO.Transforms
{
    /// <summary> Data transformer for <see cref="IAnalogueIn"/> data streams. </summary>
    public class AnalogueInTransform : IAnalogueIn
    {
        private readonly TransformTypes.DoubleTransform Range, Input;
        private readonly TransformTypes.LongTransform RawRange, RawInput;
        private readonly IAnalogueIn Base;

        /// <summary> Sets up the data transformer. </summary>
        /// <param name="Input"> The underlying input whose values should be transformed. </param>
        /// <param name="RangeTransform"> The mathematical expression to use when transforming range information (Input to Parent). </param>
        /// <param name="InputTransform"> The mathematical expression to use when transforming input voltage value information (Input to Parent). </param>
        /// <param name="RangeRawTransform"> Same as RangeTransform, but for the raw values. If left null, the same transform will be used instead. </param>
        /// <param name="InputRawTransform"> Same as InputTransform, but for the raw values. If left null, the same transform will be used instead. </param>
        public AnalogueInTransform(IAnalogueIn Input, TransformTypes.DoubleTransform RangeTransform, TransformTypes.DoubleTransform InputTransform, TransformTypes.LongTransform RangeRawTransform = null, TransformTypes.LongTransform InputRawTransform = null)
        {
            this.Range = RangeTransform;
            this.Input = InputTransform;
            this.RawRange = RangeRawTransform;
            this.RawInput = InputRawTransform;
            this.Base = Input;
        }

        /// <summary> Gets the current input value. </summary>
        /// <returns> The current input value, in Volts. </returns>
        public double GetInput() { return this.Input(this.Base.GetInput()); }

        /// <summary> Gets the maximum possible input value. </summary>
        /// <returns> The maximum possible input, in Volts. </returns>
        public double GetRange() { return this.Range(this.Base.GetRange()); }

        /// <summary> Gets the current input value. </summary>
        /// <returns> The current input value, using the underlying numerical representation of the input system. </returns>
        public long GetRawInput()
        {
            if (this.RawInput == null) { return (long)this.Input(this.Base.GetRawInput()); }
            else { return this.RawInput(this.Base.GetRawInput()); }
        }

        /// <summary> Gets the maximum possible input value. </summary>
        /// <returns> The maximum possible input, using the underlying numerical representation of the input system. </returns>
        public long GetRawRange()
        {
            if (this.RawRange == null) { return (long)this.Range(this.Base.GetRawRange()); }
            else { return this.RawRange(this.Base.GetRawRange()); }
        }

        public void Dispose() { this.Base.Dispose(); }
    }
}
