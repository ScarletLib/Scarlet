using Scarlet.IO;
using System;

namespace Scarlet.Components.Motors
{
    public class Servo : IServo
    {
        private readonly IPWMOutput PWMOut;
        public int Position { get; private set; }

        private bool Enabled;

        public Servo(IPWMOutput PWMOut)
        {
            this.PWMOut = PWMOut;
            this.PWMOut.SetFrequency(50); // TODO: Set this to an actual value, and check if this overrides others.
        }

        public void EventTriggered(object Sender, EventArgs Event) { }

        public void SetEnabled(bool Enabled)
        {
            if (!Enabled) { SetPosition(this.Position); }
            this.Enabled = Enabled;
        }

        public void SetPosition(int NewPosition)
        {
            if (this.Position == NewPosition) { return; }
            // TODO: Do filtering
            this.Position = NewPosition;
        }

        private void SetPositionDirectly(int NewPosition)
        {
            if (Enabled) { } // TODO: Implement set-position
        }
    }
}
