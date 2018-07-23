using Scarlet.IO;
using Scarlet.Utilities;
using System;

namespace Scarlet.Components.Motors
{
    public class Servo : IServo
    {
        public int Position { get; private set; }
        public bool TraceLogging { get; set; }

        private readonly IPWMOutput PWMOut;
        private readonly float MaxTime, MinTime, PWMFreq;
        private readonly int AngleRange;
        private bool Enabled;

        public Servo(IPWMOutput PWMOut, int AngleRange = 180, float MaxTime_ms = 2, float MinTime_ms = 1, float PWMFrequency = 50)
        {
            this.PWMOut = PWMOut;
            this.MaxTime = MaxTime_ms;
            this.MinTime = MinTime_ms;
            this.PWMFreq = PWMFrequency;
            this.AngleRange = AngleRange;
            this.PWMOut.SetFrequency(this.PWMFreq);
        }

        public void SetEnabled(bool Enabled)
        {
            this.Enabled = Enabled;
            this.PWMOut.SetEnabled(Enabled);
        }

        public void SetPosition(int NewPosition)
        {
            this.Position = NewPosition;
            // TODO: Do filtering
            NewPosition = Math.Min(this.AngleRange, Math.Max(0, NewPosition)); // Caps to 0 -> AngleRange
            float OnTime_ms = this.MinTime + ((this.MaxTime - this.MinTime) * (1 - ((this.AngleRange - NewPosition) * 1F / this.AngleRange)));
            float Output = OnTime_ms / (1000F / this.PWMFreq);
            this.PWMOut.SetOutput(Output);
            if (this.TraceLogging) { Log.Trace(this, "Servo moved to " + NewPosition + " degrees, which is " + (Output * 100).ToString("F2") + "% on time (" + OnTime_ms + "ms)"); }
        }

        private void SetPositionDirectly(int NewPosition)
        {
            if (Enabled) { } // TODO: Implement set-position
        }
    }
}
