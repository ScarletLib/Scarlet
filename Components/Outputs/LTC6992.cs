using Scarlet.IO;
using Scarlet.Utilities;

namespace Scarlet.Components.Outputs
{
    /// <summary>
    /// Voltage-Controlled Pulse Width Modulator
    /// Datasheet: https://www.analog.com/media/en/technical-documentation/data-sheets/69921234fc.pdf
    /// </summary>
    public class LTC6992 : IPWMOutput
    {
        private readonly IAnalogueOut Input;
        private bool Enabled;
        private double Value;

        public LTC6992(IAnalogueOut Input)
        {
            this.Input = Input;
            this.Enabled = false;
            this.Value = 0;
        }

        /// <summary> This device does not support setting a clock delay. </summary>
        /// <param name="ClockDelay"> Does nothing. </param>
        public void SetDelay(float ClockDelay) { }

        /// <summary> Sets whether the output is on or off. Duty cycle set to minimum when off. </summary>
        /// <param name="Enable"> Whether the output is enabled or not.  </param>
        public void SetEnabled(bool Enable)
        {
            this.Input.SetOutput(Enable ? this.Value : 0);
            this.Enabled = Enable;
        }

        /// <summary> This is not supported, as frequency is set via resistors. </summary>
        /// <param name="Frequency"> Does nothing, as this function is not supported. </param>
        public void SetFrequency(float Frequency) => Log.Output(Log.Severity.ERROR, Log.Source.HARDWAREIO, "Tried setting frequency of LTC6992, this is not supported. Command ignored.");

        /// <summary> Sets the output duty cycle. If output is disabled, this will not take effect until it is re-enabled. </summary>
        /// <param name="DutyCycle"> The desired duty cycle in %, from 0-1. </param>
        public void SetOutput(float DutyCycle)
        {
            this.Value = DutyCycle;
            if (this.Enabled) { this.Input.SetOutput(DutyCycle); }
        }

        public void Dispose() { this.Input.Dispose(); }
    }
}
