using Scarlet.IO;
using Scarlet.IO.BeagleBone;
using Scarlet.Utilities;
using System;

namespace Scarlet.Components.Sensors
{
    /// <summary>
    /// Cold-Junction Compensated Thermocouple-to-Digital Converter
    /// Datasheet: https://datasheets.maximintegrated.com/en/ds/MAX31855.pdf
    /// </summary>
    public class MAX31855 : ISensor
    {
        private ISPIBus Bus;
        private IDigitalOut ChipSelect;
        private uint LastReading;
        public string System { get; set; }

        /// <summary>
        /// Lists the possible reported faults.
        /// SHORT_VCC: The thermoucouple is shorted to the Vcc line.
        /// SHORT_GND: The thermoucouple is shorted to the GND line.
        /// NO_THERMOCOUPLE: The thermoucouple is not properly connected or is defective.
        /// </summary>
        [Flags]
        public enum Fault { NONE = 0, NO_THERMOCOUPLE = 1, SHORT_GND = 2, SHORT_VCC = 4 }

        public MAX31855(ISPIBus Bus, IDigitalOut ChipSelect)
        {
            this.Bus = Bus;
            this.ChipSelect = ChipSelect;
        }

        /// <summary> Checks if the sensor reports any faults. </summary>
        /// <returns> true if the sensor is in fault state, or cannot be reached. </returns>
        public bool Test() // TODO: What happens if the sensor is disconnected?
        {
            UpdateState();
            return ConvertFaultFromRaw(this.LastReading) == Fault.NONE;
        }

        /// <summary> Gets a new reading from the sensor and stores it. </summary>
        public void UpdateState()
        {
            byte[] InputData = this.Bus.Write(this.ChipSelect, new byte[] { 0x00, 0x00, 0x00, 0x00 }, 4);
            if (InputData != null && InputData.Length == 4) { this.LastReading = UtilData.ToUInt(InputData); }
        }

        /// <summary> Gets the internal (on-board) temperature at the last UpdateState() call. </summary>
        /// <returns> The temperature, in Celsius. In increments of 1/16 (0.0625) degree increments. </returns>
        public float GetInternalTemp() { return ConvertInternalFromRaw(this.LastReading); }

        /// <summary> Gets the external (thermocouple) temperature at the last UpdateState() call. </summary>
        /// <returns> The temperature, in degrees Celcius. In increments of 1/4 (0.25) degrees. </returns>
        public float GetExternalTemp() { return ConvertExternalFromRaw(this.LastReading); }

        /// <summary> Gets the present faults at the last UpdateState() call. </summary>
        /// <returns> One or more Fault states if applicable, or Fault.NONE if the sensor reports none. </returns>
        public Fault GetFaults() { return ConvertFaultFromRaw(this.LastReading); }

        /// <summary> Gets the sensor's raw data as it was received at the last UpdateState() call. </summary>
        /// <returns> The bytes sent over SPI from the sensor at the last reading. </returns>
        public uint GetRawData() { return this.LastReading; }

        /// <summary> Interprets the internal (on-board) temperature data out of the raw data. </summary>
        /// <param name="RawData"> The raw bytes, as transferred via SPI. </param>
        /// <returns> The temperature, in Celsius. In increments of 1/16 (0.0625) degree increments. </returns>
        public static float ConvertInternalFromRaw(uint RawData)
        {
            ushort InputBits = (ushort)((RawData >> 4) & 0b1111_1111_1111);
            short OutputBits = 0;
            if (InputBits > 0b1000_0000_0000) { OutputBits = (short)((0x1000 - InputBits) * -1); } // Convert 12-bit to 16-bit value.
            else { OutputBits = (short)InputBits; }
            return OutputBits / 16.0000F;
        }

        /// <summary> Interprets the external (thermocouple) temperature data out of the raw data. </summary>
        /// <param name="RawData"> The raw bytes, as transferred via SPI. </param>
        /// <returns> The temperature, in degrees Celcius. In increments of 1/4 (0.25) degrees. </returns>
        public static float ConvertExternalFromRaw(uint RawData)
        {
            ushort InputBits = (ushort)((RawData >> 18) & 0b11_1111_1111_1111);
            short OutputBits = 0;
            if (InputBits > 0b10_0000_0000_0000) { OutputBits = (short)((0x4000 - InputBits) * -1); } // Convert 14-bit to 16-bit value.
            else { OutputBits = (short)InputBits; }
            return OutputBits / 4.00F;
        }

        /// <summary> Interprets the sensor's fault states out of the raw data. </summary>
        /// <param name="RawData">The raw bytes, as transferred via SPI. </param>
        /// <returns> One or more Fault states if applicable, or Fault.NONE if the sensor reports none. </returns>
        public static Fault ConvertFaultFromRaw(uint RawData)
        {
            if (((RawData >> 16) & 1) == 1) // FAULT bit is set.
            {
                return (Fault)((RawData & 0b111));
            }
            else { return Fault.NONE; }
        }

        /// <summary> This sensor does not process events. Will do nothing. </summary>
        public void EventTriggered(object Sender, EventArgs Event) { }

        public DataUnit GetData()
        {
            return new DataUnit("MAX31855")
            {
                { "IntTemp", ConvertInternalFromRaw(this.LastReading) },
                { "ExtTemp", ConvertExternalFromRaw(this.LastReading) },
                { "Fault", ConvertFaultFromRaw(this.LastReading).ToString() }
            }
            .SetSystem(this.System);
        }
    }
}
