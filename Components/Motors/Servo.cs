using Scarlet.IO;
using Scarlet.Utilities;
using System;

namespace Scarlet.Components.Motors
{
    public class Servo : IServo // TODO: Implement filtering for servo position.
    {
        public int Position { get; private set; }
        public bool TraceLogging { get; set; }

        private readonly IPWMOutput PWMOut;
        private readonly float MaxTime, MinTime, PWMFreq;
        private readonly int AngleRange;
        private bool Enabled;

        /// <summary> Drives a basic PWM-controlled servo motor. </summary>
        /// <remarks> Servos usually don't care about the PWM frequency, but about the on-time of the pulses. Therefore, the range is configured with on-times instead of PWM percentage. </remarks>
        /// <param name="PWMOut"> The PWM output to use to control the servo. </param>
        /// <param name="AngleRange"> The range of the servo (check the datasheet). Usually between 180 and 300. </param>
        /// <param name="MaxTime_ms"> The on-time (in milliseconds) that corresponds with the maximum servo displacement. Usually between 1 and 3. </param>
        /// <param name="MinTime_ms"> The on-time (in milliseconds) that corresponds with the minimum servo displacement. Less than <see cref="MaxTime_ms"/>, usually between 0.5 and 2. </param>
        /// <param name="PWMFrequency"> The frequency to set the PWM output to. Usually between 10 and 200. </param>
        public Servo(IPWMOutput PWMOut, int AngleRange = 180, float MaxTime_ms = 2, float MinTime_ms = 1, float PWMFrequency = 50)
        {
            this.PWMOut = PWMOut;
            this.MaxTime = MaxTime_ms;
            this.MinTime = MinTime_ms;
            this.PWMFreq = PWMFrequency;
            this.AngleRange = AngleRange;
            this.PWMOut.SetFrequency(this.PWMFreq);
        }

        /// <summary> Enables/disables output to the servo. Disabling usually causes it to stop resisting external movement. </summary>
        /// <param name="Enabled"> Whether to output a PWM signal to the servo. </param>
        public void SetEnabled(bool Enabled)
        {
            this.Enabled = Enabled;
            this.PWMOut.SetEnabled(Enabled);
        }

        /// <summary> Changes the servo's position to the given angle. </summary>
        /// <param name="NewPosition"> The desired angle, in degrees. Input outside the range 0->AngleRange is capped at those limits. </param>
        public void SetPosition(int NewPosition)
        {
            this.Position = NewPosition;
            NewPosition = Math.Min(this.AngleRange, Math.Max(0, NewPosition)); // Caps to 0 -> AngleRange
            float OnTime_ms = this.MinTime + ((this.MaxTime - this.MinTime) * (1 - ((this.AngleRange - NewPosition) * 1F / this.AngleRange)));
            float Output = OnTime_ms / (1000F / this.PWMFreq);
            this.PWMOut.SetOutput(Output);
            if (this.TraceLogging) { Log.Trace(this, "Servo moved to " + NewPosition + " degrees, which is " + (Output * 100).ToString("F2") + "% on time (" + OnTime_ms + "ms)"); }
        }
    }
}
