using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Scarlet.IO;
using Scarlet.Utilities;

namespace Scarlet.Components.Sensors
{
    /// <summary>
    /// Analogue Soil Moisture Sensor
    /// Data: http://vegetronix.com/Products/VH400/
    /// </summary>
    public class VH400 : ISensor
    {
        public string System { get; set; }
        private IAnalogueIn Input;
        private double LastReading;
        public double VoltageMultiplier { get; set; }

        public VH400(IAnalogueIn Input)
        {
            this.Input = Input;
            this.VoltageMultiplier = 1;
        }

        /// <summary> Gets the sensor's data in a standard format. </summary>
        public DataUnit GetData()
        {
            return new DataUnit("VH400")
            {
                { "RawReading", this.LastReading },
                { "VWC%", ToMoisture(this.LastReading, this.VoltageMultiplier) }
            }
            .SetSystem(this.System);
        }

        /// <summary> Checks to see if the current analogue input is within the output range of the sensor. </summary>
        public bool Test() => (this.LastReading >= 0 && this.LastReading <= 3);

        /// <summary> Gets a new reading from the sensor. </summary>
        public void UpdateState() { this.LastReading = this.Input.GetInput(); }

        /// <summary> Gets the volumetric water content in %, between 0 and 1. </summary>
        /// <returns> VWC %, or -1 if the sensor is not functioning correctly. </returns>
        public float GetReading() { return ToMoisture(this.LastReading, this.VoltageMultiplier); }

        /// <summary> Gets the sensor's output voltage (should be between 0 and 3V). </summary>
        public float GetVoltage() { return (float)this.LastReading; }

        /// <summary> Converts the sensor's voltage output to Volumetric water content (%) between 0 and 1. </summary>
        /// <remarks> Uses the piecewise curve taken from https://vegetronix.com/Products/VH400/VH400-Piecewise-Curve.phtml </remarks>
        /// <param name="RawReading"> The sensor's output voltage (0-3V) </param>
        /// <param name="VoltageMultipler"> The amount to multiply the input voltage by before making calculations. THis is useful if you have a resistor divider on the input, in which case you'd want a multiplier > 1. </param>
        /// <returns> Volumetric water content % (0-1) if valid input, -1 if invalid input. </returns>
        public static float ToMoisture(double RawReading, double VoltageMultipler)
        {
            RawReading *= VoltageMultipler;
            if (RawReading < 0 || RawReading > 3) { return -1; } // Invalid input.
            if (RawReading < 1.1) { return (float)((0.1 * RawReading) - 0.01); }
            if (RawReading < 1.3) { return (float)((0.15 * RawReading) - 0.175); }
            if (RawReading < 1.82) { return (float)((0.4808 * RawReading) - 0.475); }
            if (RawReading < 2.2) { return (float)((0.2632 * RawReading) - 0.0789); }
            return (float)((0.625 * RawReading) - 0.875);
        }

        /// <summary> Does nothing. </summary>
        public void EventTriggered(object Sender, EventArgs Event) { }
    }
}
