namespace Scarlet.IO
{
    public interface IPWMOutput
    {
        /// <summary> Sets the PWM Frequency. </summary>
        /// <param name="Frequency"> The output frequency in Hz. </param>
        void SetFrequency(float Frequency);

        /// <summary> Sets the output duty cycle. </summary>
        /// <param name="DutyCycle"> % duty cycle from 0.0 to 1.0. </param>
        void SetOutput(float DutyCycle);

        /// <summary> Sets the delay time of the cycle start. </summary>
        /// <remarks> Only useful for devices that have multiple PWM outputs that share a clock, and you need custom synchronization. Not all devices will support this, if they do not, this setting will simply not apply. </remarks>
        /// <param name="ClockDelay"> % of a clock cycle to delay the leading edge by, from 0.0 to (exclusive) 1.0. </param>
        void SetDelay(float ClockDelay);

        /// <summary> Turns on/off the PWM output device. May have different results on different platforms. </summary>
        void SetEnabled(bool Enable);

        /// <summary> Releases handles to the output, allowing it to be used by another component or application. </summary>
        void Dispose();
    }
}
