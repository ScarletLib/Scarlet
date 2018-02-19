using System;
using Scarlet.IO;
using Scarlet.Utilities;

namespace Scarlet.Components.Sensors
{
    /// <summary>
    /// High-Side or Low-Side Measurement, Bi-Directional Current and Power Monitor
    /// Datasheet: http://www.ti.com/lit/ds/symlink/ina226.pdf
    /// </summary>
    public class INA226 : ISensor
    {
        public string System { get; set; }

        private int CurrentLSB;

        /// <summary> Prepares the INA226 device for usee. </summary>
        /// <param name="MaxCurrent"> The absolute maximum current that you expect to measure with this. Used to set amplifier scaling. Usually set to the connected device's max current, loike motor stall current. </param>
        /// <param name="Resistor"> The resistance of the current shunt path. This should be measured with the best possible precision, as slight error here can cause large measurement error. </param>
        public INA226(II2CBus Bus, float MaxCurrent, float Resistor)
        {
            this.CurrentLSB = (int)Math.Round(Math.Abs(MaxCurrent) / Math.Pow(2, 15));
        }

        public void EventTriggered(object Sender, EventArgs Event) { }

        public DataUnit GetData()
        {
            return new DataUnit("INA226")
            {

            }
            .SetSystem(this.System);
        }

        public bool Test()
        {
            return true; // TODO: Test sensor.
        }

        public void UpdateState()
        {
            
        }
    }
}
