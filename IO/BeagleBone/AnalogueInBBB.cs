using BBBCSIO;
using Scarlet.Utilities;
using System;
using System.IO;
using System.Threading;

namespace Scarlet.IO.BeagleBone
{
    public class AnalogueInBBB : IAnalogueIn
    {
        public BBBPin Pin { get; private set; }
        private A2DPortFS Port;

        /// <summary> Prepares the analogue input for use. This can block for up to 5 seconds while the hardware and kernel are intiializing. </summary>
        /// <param name="Pin"> The pin on the BeagleBone to use for analogue input. Must be one of the 7 AIN pins. </param>
        /// <exception cref="TimeoutException"> If the hardware/kernel take longer than 5 seconds to initialize. </exception>
        public AnalogueInBBB(BBBPin Pin)
        {
            this.Pin = Pin;
            // The ADC input file is not always available immediately, it can take a bit for it to be available after device tree overaly application.
            // Therefore, we will wait for it for up to 5 seconds, then throw an error if it is not ready at that time.
            string Filename = "/sys/bus/iio/devices/iio:device0/in_voltage";
            switch(this.Pin)
            {
                case BBBPin.P9_39: Filename += 0; break;
                case BBBPin.P9_40: Filename += 1; break;
                case BBBPin.P9_37: Filename += 2; break;
                case BBBPin.P9_38: Filename += 3; break;
                case BBBPin.P9_33: Filename += 4; break;
                case BBBPin.P9_36: Filename += 5; break;
                case BBBPin.P9_35: Filename += 6; break;
                default: throw new Exception("Given pin is not a valid ADC pin!");
            }
            Filename += "_raw";
            DateTime Timeout = DateTime.Now.Add(TimeSpan.FromSeconds(5));
            while(!File.Exists(Filename))
            {
                if (DateTime.Now > Timeout)
                {
                    Log.Output(Log.Severity.ERROR, Log.Source.HARDWAREIO, "ADC Failed to initialize.");
                    throw new TimeoutException("ADC failed to initialize within the timeout period.");
                }
                Thread.Sleep(50);
            }
            this.Port = new A2DPortFS(IO.BeagleBone.Pin.PinToA2D(this.Pin));
        }

        /// <summary> The number of possible ADC values. </summary>
        public long GetRawRange() { return 4096; } // This may change depending on device tree settings.

        /// <summary> The range of voltages the ADC can sense. In Volts. </summary>
        public double GetRange() { return 1.8D; }

        /// <summary> Gets the current ADC value, as a voltage. </summary>
        public double GetInput() { return (double)GetRawInput() * GetRange() / (double)(GetRawRange() - 1); }

        /// <summary> Gets the current ADC value, as the raw number. </summary>
        public long GetRawInput() { return this.Port.Read(); }

        public void Dispose() { this.Port.Dispose(); }
    }
}
