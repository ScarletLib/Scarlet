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

        private II2CBus Bus;
        private byte Address;
        private double Resistor;

        private double CurrentMultiplier;
        private ushort CalibrationVal;
        private ushort[] LastReading = new ushort[4];

        private const float BUS_VOLTAGE_MULTIPLIER = 0.00125F;

        private enum Register
        {
            Configuration = 0x00,
            ShuntVoltage = 0x01,
            BusVoltage = 0x02,
            Power = 0x03,
            Current = 0x04,
            Calibration = 0x05,
            AlertMask = 0x06,
            AlertLimit = 0x07,
            ManufacturerID = 0xFE,
            DieID = 0xFF
        }

        /// <summary> Determines the number of samples that are collected and averaged. </summary>
        public enum AveragingMode
        {
            Last1 = 0b000,
            Last4 = 0b001,
            Last16 = 0b010,
            Last64 = 0b011,
            Last128 = 0b100,
            Last256 = 0b101,
            Last512 = 0b110,
            Last1024 = 0b111
        }

        /// <summary> Sets the conversion time for the bus voltage measurement. </summary>
        public enum ConversionTime
        {
            Time140us = 0b000,
            Time204us = 0b001,
            Time332us = 0b010,
            Time588us = 0b011,
            Time1100us = 0b100,
            Time2116us = 0b101,
            Time4156us = 0b110,
            Time8244us = 0b111
        }

        /// <summary> Prepares the INA226 device for usee. </summary>
        /// <param name="MaxCurrent"> The absolute maximum current that you expect to measure with this. Used to set amplifier scaling. Usually set to the connected device's max current, loike motor stall current. </param>
        /// <param name="Resistor"> The resistance of the current shunt path. This should be measured with the best possible precision, as slight error here can cause large measurement error. </param>
        public INA226(II2CBus Bus, byte DeviceAddress, float MaxCurrent, double Resistor, AveragingMode Avg = AveragingMode.Last1, ConversionTime VBusTime = ConversionTime.Time1100us, ConversionTime VShuntTime = ConversionTime.Time1100us)
        {
            // TODO add comments for params
            this.Bus = Bus;
            this.Address = DeviceAddress;
            this.Resistor = Resistor;
            SetConfig(MaxCurrent, Avg, VBusTime, VShuntTime);
        }

        /// <summary> Sets configuration and calibration registers with these settings. </summary>
        private void SetConfig(float MaxCurrent, AveragingMode Avg, ConversionTime VBusTime, ConversionTime VShuntTime)
        {
            // Sets Configuration Register
            ushort Config = 0b0100_0000_0000_0111;
            Config |= (ushort)(((ushort)Avg << 9) & 0b0000_1110_0000_0000); // Averaging mode
            Config |= (ushort)(((ushort)VBusTime << 6) & 0b0000_0001_1100_0000); // VBus conversion time
            Config |= (ushort)(((ushort)VShuntTime << 3) & 0b0000_0000_0011_1000); // VShunt conversion time
            this.Bus.WriteRegister(this.Address, (byte)Register.Configuration, new byte[] { (byte)((Config >> 8) & 0b1111_1111), (byte)(Config & 0b1111_1111) });

            // Sets Calibration Register
            this.CurrentMultiplier = Math.Abs(MaxCurrent) / Math.Pow(2, 15);
            this.CalibrationVal = (ushort)Math.Ceiling(0.00512D / (this.CurrentMultiplier * this.Resistor));
            this.CurrentMultiplier = (0.00512D / this.CalibrationVal) / this.Resistor; // Since rounding the value may have slightly changed the multiplier, make sure we are using what the device will.
            Log.Output(Log.Severity.DEBUG, Log.Source.SENSORS, "INA226 is using current multiplier " + this.CurrentMultiplier + " A/count.");
        }

        public double GetBusVoltage() => ConvertBusVoltageFromRaw(this.LastReading);

        public double GetCurrent() => ConvertCurrentFromRaw(this.LastReading);

        public double GetPower() => ConvertPowerFromRaw(this.LastReading);

        public static double ConvertBusVoltageFromRaw(ushort[] RawData)
        {
            return 0;
        }

        public static double ConvertCurrentFromRaw(ushort[] RawData)
        {
            return 0;
        }

        public static double ConvertPowerFromRaw(ushort[] RawData)
        {
            return 0;
        }

        public DataUnit GetData()
        {
            return new DataUnit("INA226")
            {
                { "BusVoltage", GetBusVoltage() },
                { "Current", GetCurrent() },
                { "Power", GetPower() }
            }
            .SetSystem(this.System);
        }

        public bool Test()
        { // TODO: Check if this works as expected.
            byte[] DieID = this.Bus.ReadRegister(this.Address, (byte)Register.DieID, 2);
            return (DieID[0] == 0x22) && (DieID[1] == 0x60);
        }

        public void UpdateState()
        {
            byte[] RawData = this.Bus.ReadRegister(this.Address, (byte)Register.ShuntVoltage, 8);
            this.LastReading[0] = (ushort)((RawData[0] << 8) | RawData[1]);
            this.LastReading[1] = (ushort)((RawData[2] << 8) | RawData[3]);
            this.LastReading[2] = (ushort)((RawData[4] << 8) | RawData[5]);
            this.LastReading[3] = (ushort)((RawData[6] << 8) | RawData[7]);
        }

        public void EventTriggered(object Sender, EventArgs Event) { }
    }
}
