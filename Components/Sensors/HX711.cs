using System.Threading;
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
        private int LastReading;

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

        /// <summary> Gets the most recent reading, and adjusts it (offset and scaling). </summary>
        /// <returns> A reading in the unit system you defined with <see cref="Offset"/> and <see cref="ScaleFactor"/>. </returns>
        public double GetAdjustedReading() => AdjustReading(this.LastReading, this.Offset, this.ScaleFactor);

        /// <summary> Gets the raw reading. </summary>
        /// <returns> THe data as the sensor returned it. Not scaled or offset. </returns>
        public int GetRawReading() => this.LastReading;

        /// <summary> Used to convert a raw reading into a adjusted one in the desired unit system. </summary>
        /// <param name="RawReading"> The raw reading, as it came from the sensor. </param>
        /// <param name="Offset"> The amount to offset the reading (done first). Used to set the correct zero point. Obtained from using <see cref="Tare"/>. </param>
        /// <param name="ScaleFactor"> The amount to scale the reading (done second). Used to convert the reading to a familiar unit system, like g or kg. Obtained by getting an adjusted reading with a test mass after taring. </param>
        /// <returns> The most recent reading in the desired unit system. </returns>
        public static double AdjustReading(int RawReading, double Offset, double ScaleFactor)
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
        private int Read() // TODO: Deal with the system potentially being too fast for the sensor (clock cycles must be at least 0.2us)
        {
            this.Clock.SetOutput(false);
            byte FailCounter = 0;
            while (this.Data.GetInput())
            {
                Thread.Sleep(1);
                FailCounter++;
                if (FailCounter > 120)
                {
                    Log.Output(Log.Severity.WARNING, Log.Source.SENSORS, "HX711 failed to have data ready for at least 120ms. Check the serial connections.");
                    this.LastReadFailed = true;
                    return int.MaxValue;
                }
            }
            this.Clock.SetOutput(false);

            uint RawData = 0;
            for (int i = 0; i < 24; i++)
            {
                this.Clock.SetOutput(true);
                RawData = RawData | ((this.Data.GetInput() ? 1U : 0U) << (23 - i));
                this.Clock.SetOutput(false);
            }
            for (byte i = 0; i < (int)this.GainSetting; i++)
            {
                this.Clock.SetOutput(true);
                this.Clock.SetOutput(false);
            }

            if (((RawData >> 23) & 0b1) == 0b1) { RawData |= 0xFF000000; } // Fill in top byte if the value is negative.
            if (this.TraceLogging) { Log.Trace(this, "Got raw value: " + RawData.ToString("X4")); }
            this.LastReadFailed = false;
            return unchecked((int)RawData);
        }

        public enum Gain
        {
            /// <summary> Input channel A, 128x gain. </summary>
            GAIN_128x = 1,

            /// <summary> Input channel A, 64x gain. </summary>
            GAIN_64x = 3,

            /// <summary> Input channel B, 32x gain. </summary>
            GAIN_32x = 2
        }
    }
}
