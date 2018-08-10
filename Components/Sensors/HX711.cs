using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Scarlet.Filters;
using Scarlet.IO;
using Scarlet.Utilities;

namespace Scarlet.Components.Sensors
{
    /// <summary>
    /// 24-bit ADC for Weigh Scales
    /// Datasheet here: https://cdn.sparkfun.com/datasheets/Sensors/ForceFlex/hx711_english.pdf
    /// </summary>
    public class HX711 : ISensor
    {
        public string System { get; set; }
        public bool TraceLogging { get; set; }

        /// <summary> The amount to offset readings by, should be determined by calling <see cref="Tare"/>, then used thereafter. </summary>
        public double Offset { get; set; }

        /// <summary> The scale factor to apply to the readings. Used to convert raw values into values in some units. Usually determined once by using a test mass after taring. </summary>
        public double ScaleFactor { get; set; }

        private readonly IDigitalOut Clock;
        private readonly IDigitalIn Data;
        private Gain GainSetting = Gain.GAIN_128x;

        private bool LastReadFailed = false;
        private long LastReading;

        public HX711(IDigitalOut Clock, IDigitalIn Data)
        {
            this.Clock = Clock;
            this.Data = Data;
            this.Clock.SetOutput(false);
            this.Offset = 0;
            this.ScaleFactor = 1;
        }

        /// <summary> Gets a new reading from the device. </summary>
        public void UpdateState() { this.LastReading = Read(); }

        /// <summary> Checks if the ADC is detected and responding. </summary>
        /// <returns> Whether the device returned any valid data. </returns>
        public bool Test()
        {
            Read();
            return !this.LastReadFailed;
        }

        public DataUnit GetData()
        {
            return new DataUnit(this.System)
            {
                { "RawReading", GetRawReading() },
                { "AdjustedReading", GetAdjustedReading() }
            };
        }

        public double GetAdjustedReading() => AdjustReading(this.LastReading, this.Offset, this.ScaleFactor);

        public long GetRawReading() => this.LastReading;

        public static double AdjustReading(long RawReading, double Offset, double ScaleFactor)
        {
            return (RawReading - Offset) * ScaleFactor;
        }

        /// <summary> Sets the ADC's gain factor. Higher gain increases resolution if the output of the load cell is low in amplitude, but will cause input saturation on higher-output load cells. </summary>
        /// <param name="Gain"> The amount to multiply the input by before digital conversion. </param>
        public void SetGain(Gain Gain)
        {
            this.GainSetting = Gain;
            Read();
        }

        /// <summary> Puts the device to sleep to reduce power consumption. Use <see cref="Wake"/> before resuming operation. </summary>
        public void Sleep()
        {
            this.Clock.SetOutput(false);
            this.Clock.SetOutput(true);
            Thread.Sleep(1);
        }

        /// <summary> Wakes the device after using <see cref="Sleep"/>, and re-sets the gain (it is lost during power-down). </summary>
        public void Wake()
        {
            this.Clock.SetOutput(false);
            SetGain(this.GainSetting);
        }

        /// <summary> Sets the scale's offset so that the current state will read as 0 from here on. </summary>
        /// <param name="SampleCount"> The number of samples to take and average to determine the zero value. </param>
        public void Tare(uint SampleCount = 10)
        {
            long Sum = 0;
            for (int i = 0; i < SampleCount; i++) { Sum += Read(); }
            this.Offset = (Sum * 1.0 / SampleCount);
        }

        /// <summary> Gets a new reading, and sets the gain for the next reading. </summary>
        /// <returns> The raw data. </returns>
        private long Read() // TODO: Deal with the system potentially being too fast for the sensor (clock cycles must be at least 0.2us)
        {
            this.Clock.SetOutput(false);
            byte FailCounter = 0;
            while (!this.Data.GetInput())
            {
                Thread.Sleep(1);
                FailCounter++;
                if (FailCounter > 120)
                {
                    Log.Output(Log.Severity.WARNING, Log.Source.SENSORS, "HX711 failed to have data ready for at least 120ms. Check the serial connections.");
                    this.LastReadFailed = true;
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
            if (this.TraceLogging) { Log.Trace(this, "Got raw value: " + RawData.ToString("X16")); }
            this.LastReadFailed = false;
            return unchecked((long)RawData);
        }

        public enum Gain
        {
            GAIN_128x = 1,
            GAIN_64x = 3,
            GAIN_32x = 2
        }
    }
}
