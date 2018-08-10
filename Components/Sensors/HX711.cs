using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Scarlet.IO;
using Scarlet.Utilities;

namespace Scarlet.Components.Sensors
{
    public class HX711 : ISensor
    {
        public string System { get; set; }
        public bool TraceLogging { get; set; }

        private readonly IDigitalOut Clock;
        private readonly IDigitalIn Data;
        private Gain GainSetting = Gain.GAIN_128x;

        public HX711(IDigitalOut Clock, IDigitalIn Data, double ScaleFactor, double Offset)
        {
            this.Clock = Clock;
            this.Data = Data;
            this.Clock.SetOutput(false);
        }

        public void UpdateState()
        {

        }

        public bool Test()
        {
            return true; // TODO: Implement testing.
        }

        public DataUnit GetData()
        {
            return new DataUnit(this.System)
            {
                // TODO: Add data to DataUnit.
            };
        }

        public void SetGain(Gain Gain)
        {
            this.GainSetting = Gain;
            Read();
        }

        public enum Gain
        {
            GAIN_128x = 1,
            GAIN_64x = 3,
            GAIN_32x = 2
        }

        private long Read() // TODO: Deal with the system potentially being too fast for the sensor (clock cycles must be at least 0.2us)
        {
            byte FailCounter = 0;
            while (!this.Data.GetInput())
            {
                Thread.Sleep(1);
                FailCounter++;
                if (FailCounter > 100)
                {
                    Log.Output(Log.Severity.WARNING, Log.Source.SENSORS, "HX711 failed to have data ready for at least 100ms. Check the serial connections.");
                    return long.MaxValue;
                }
            }
            this.Clock.SetOutput(false);
            ulong RawData = 0;
            for (int i = 0; i < 24; i++)
            {
                this.Clock.SetOutput(true);
                RawData = RawData | ((this.Data.GetInput() ? 1UL : 0UL) << i);
                this.Clock.SetOutput(false);
            }
            for (byte i = 0; i < (int)this.GainSetting; i++)
            {
                this.Clock.SetOutput(true);
                this.Clock.SetOutput(false);
            }

            if (((RawData >> 23) & 0b1) == 0b1) { RawData |= 0xFF000000; } // Fill in top byte if the value is negative.

            if(this.TraceLogging) { Log.Trace(this, "Got raw value: " + RawData.ToString("X16")); }

            return unchecked ((long)RawData);
        }

    }
}
