using Scarlet.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTest
{
    public class TestablePWMOutput : IPWMOutput
    {
        public float Frequency { get; private set; }
        public float DutyCycle { get; private set; }
        public float Delay { get; private set; }
        public bool Enabled { get; private set; }

        public void Dispose() { }

        public void SetDelay(float ClockDelay) { this.Delay = ClockDelay; }

        public void SetEnabled(bool Enable) { this.Enabled = Enable; }

        public void SetFrequency(int Frequency) { this.Frequency = Frequency; }

        public void SetFrequency(float Frequency) { this.Frequency = Frequency; }

        public void SetOutput(float DutyCycle) { this.DutyCycle = DutyCycle; }
    }
}
