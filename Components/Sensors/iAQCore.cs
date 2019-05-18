using Scarlet.IO;
using Scarlet.Utilities;

namespace Scarlet.Components.Sensors
{
    /// <summary>
    /// Indoor Air Quality Sensor Module with CO2 and Total VOC measurement capabilities.
    /// Datasheet: https://ams.com/documents/20143/36005/iAQ-core_DS000334_1-00.pdf
    /// </summary>
    public class iAQCore : ISensor
    {
        private readonly II2CBus I2C;
        private readonly byte Address;
        private byte[] LastReading;
        private bool ReadingValid = true;

        public string System { get; set; }
        public bool TraceLogging { get; set; }

        /// <summary> Prepares the iAQ-Core device for use. </summary>
        /// <param name="I2C"> The I2C bus to communicate via. </param>
        /// <param name="Address"> The sensor's I2C address, this is unlikely to change. </param>
        public iAQCore(II2CBus I2C, byte Address = 0x5A)
        {
            this.I2C = I2C;
            this.Address = Address;
        }

        /// <summary> Gets a DataUnit containing all of the most recent sensor readings, as well as an indicator whether the data is likely to be valid. </summary>
        /// <returns> The DataUnit containing most recent sensor data. </returns>
        public DataUnit GetData()
        {
            return new DataUnit("iAQ-Core")
            {
                { "CO2-ppm", GetCO2Value() },
                { "TVOC-ppb", GetTVOCValue() },
                { "SesnsorResistance", GetSensorResistance() },
                { "DataValid", Test() }
            }.SetSystem(this.System);
        }

        /// <summary> Checks whether the sensor appears to be responding correctly. </summary>
        /// <returns> If the previous attempt to get data from the sensor returned a valid status code. </returns>
        public bool Test() { return this.ReadingValid; } // TODO: Do sanity checking on sensor resistance to filter out invalid data.

        /// <summary> Gets the most recent data from the sensor. This may fail, check <see cref="Test"/> to see the most recent status after this. </summary>
        public void UpdateState()
        {
            byte Attempts = 0;
            do
            {
                this.LastReading = this.I2C.Read(this.Address, 9);
                Attempts++;
                if (this.TraceLogging) { Log.Trace(this, "Data read attempt " + Attempts + " returned status 0x" + this.LastReading[2].ToString("X2")); }
            }
            while (this.LastReading[2] != 0x00 && Attempts < 10);
            if (Attempts >= 10)
            {
                this.ReadingValid = false;
                Log.Output(Log.Severity.WARNING, Log.Source.SENSORS, "Failed to get valid sensor data from iAQ-Core after 10 attempts. Last status code: 0x" + this.LastReading[2].ToString("X2"));
            }
        }

        /// <summary> Gets the most recent CO2 content reading. Update the reading with <see cref="UpdateState"/>. </summary>
        /// <returns> The most recent CO2 reading, in ppm. </returns>
        public ushort GetCO2Value() { return (ushort)(this.LastReading[0] << 8 | this.LastReading[1]); }

        /// <summary> Gets the most recent sensor resistance reading. Update the reading with <see cref="UpdateState"/>. </summary>
        /// <returns> The most recent sensor resistance reading, in Ohms. </returns>
        public int GetSensorResistance() { return (this.LastReading[3] << 24 | this.LastReading[4] << 16 | this.LastReading[5] << 8 | this.LastReading[6]); }

        /// <summary> Gets the most recent TVOC content reading. Update the reading with <see cref="UpdateState"/>. </summary>
        /// <returns> The most recent TVOC (total volatile organic compond) concentration reading, in ppb. </returns>
        public ushort GetTVOCValue() { return (ushort)(this.LastReading[7] << 8 | this.LastReading[8]); }
    }
}
