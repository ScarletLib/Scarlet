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
        public bool TraceLogging { get; set; }

        private readonly II2CBus Bus;
        private readonly byte Address;
        private readonly double Resistor;

        public double CurrentMultiplier { get; private set; }
        private ushort CalibrationVal;
        private ushort[] LastReading = new ushort[4];

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

        // TODO: Implement alert pin configuration.

        /// <summary> Prepares the INA226 device for use. </summary>
        /// <param name="Bus"> The I2C bus the device is connected to. </param>
        /// <param name="DeviceAddress"> The I2C address of the device. Set by hardware pin connections. </param>
        /// <param name="MaxCurrent"> The absolute maximum current that you expect to measure with this. Used to set amplifier scaling. Usually set to the connected device's max current, like motor stall current. </param>
        /// <param name="Resistor"> The resistance of the current shunt path. This should be measured with the best possible precision, as slight error here can cause large measurement error. </param>
        /// <param name="Avg"> How many samples you want the chip to average for voltage/current/power values. Will stabilize readings, but reduce high-frequency response (usually not needed anyways). </param>
        /// <param name="VBusTime"> Determines how long the ADC samples to measure power supply voltage. Longer (slower) times will stabilize readings, but reduce high frequency response (usually not needed anyways). </param>
        /// <param name="VShuntTime"> Determines how long the ADC samples to measure shunt voltage drop. Longer (slower) times will stabilize readings, but reduce high frequency response (usually not needed anyways). </param>
        public INA226(II2CBus Bus, byte DeviceAddress, float MaxCurrent, double Resistor, AveragingMode Avg = AveragingMode.Last1, ConversionTime VBusTime = ConversionTime.Time1100us, ConversionTime VShuntTime = ConversionTime.Time1100us)
        {
            this.Bus = Bus;
            this.Address = DeviceAddress;
            this.Resistor = Resistor;
            SetConfig(MaxCurrent, Avg, VBusTime, VShuntTime);
        }

        /// <summary> Gets the VBus pin voltage at the last UpdateState() call. </summary>
        /// <returns> The voltage, in Volts. In increments of 1.25mV. Always positive. </returns>
        public double GetBusVoltage() => ConvertBusVoltageFromRaw(this.LastReading);

        /// <summary> Gets the voltage across the Vin+ and Vin- pins at the last UpdateState() call. </summary>
        /// <returns> The voltage, in Volts. In increments of 2.5mV. Positive or negative. </returns>
        public double GetShuntVoltage() => ConvertShuntVoltageFromRaw(this.LastReading);

        /// <summary> Gets the calculated current across the shunt resistor at the last UpdateState() call. </summary>
        /// <returns> The current, in Amps. Increments depend on scaling, which is configured by shunt resistance and max current. </returns>
        public double GetCurrent() => ConvertCurrentFromRaw(this.LastReading, this.CurrentMultiplier);

        /// <summary> Gets the calculated power going to the load at the last UpdateState() call. </summary>
        /// <returns> The power, in Watts. Increments depend on scaling, which is configured by shunt resistance and max current. Always positive. </returns>
        public double GetPower() => ConvertPowerFromRaw(this.LastReading, this.CurrentMultiplier);

        /// <summary> Interprets the VBus pin voltage data out of the raw data. </summary>
        /// <param name="RawData"> The raw bytes, as transferred via I2C. </param>
        /// <returns> The voltage, in Volts. In increments of 1.25mV. Always positive. </returns>
        public static double ConvertBusVoltageFromRaw(ushort[] RawData) => RawData[1] * 0.00125D;

        /// <summary> Interprets the shunt voltage drop data out of the raw data. </summary>
        /// <param name="RawData"> The raw bytes, as transferred via I2C. </param>
        /// <returns> The voltage, in Volts. In increments of 2.5mV. Positive or negative. </returns>
        public static double ConvertShuntVoltageFromRaw(ushort[] RawData)
        {
            if (((RawData[0] >> 15) & 0b1) == 1) // Negative number
            {
                ushort Result = (ushort)(RawData[0] - 1);
                Result = (ushort)(~Result);
                return (Result * -0.0000025D);
            }
            else { return RawData[0] * 0.0000025D; } // Positive
        }

        /// <summary> Interprets the calculated current data out of the raw data. </summary>
        /// <param name="RawData"> The raw bytes, as transferred via I2C. </param>
        /// <param name="CurrentMultiplier"> The translation between bits and Amps. Usually <see cref="CurrentMultiplier"/>. </param>
        /// <returns> The current, in Amps. Increments depend on scaling, which is configured by shunt resistance and max current. </returns>
        public static double ConvertCurrentFromRaw(ushort[] RawData, double CurrentMultiplier)
        {
            return RawData[3] * CurrentMultiplier; // TODO: Check this, as it might be negative.
        }

        /// <summary> Interprets the calculated power data out of the raw data. </summary>
        /// <param name="RawData"> The raw bytes, as transferred via I2C. </param>
        /// <param name="CurrentMultiplier"> The translation between bits and Amps. Usually <see cref="CurrentMultiplier"/>. </param>
        /// <returns> The power, in Watts. Increments depend on scaling, which is configured by shunt resistance and max current. Always positive. </returns>
        public static double ConvertPowerFromRaw(ushort[] RawData, double CurrentMultiplier) => RawData[2] * (CurrentMultiplier * 25);

        /// <summary> The last sensor reading, as easily accessible data. </summary>
        /// <returns>
        /// The following data:
        /// | Name       | Type | Remarks                      |
        /// |============|======|==============================|
        /// |BusVoltage  |double|Power supply voltage (V)      |
        /// |ShuntVoltage|double|Current shunt voltage drop (V)|
        /// |Current     |double|Calculated current (A)        |
        /// |Power       |double|Calculated power (W)          |
        /// </returns>
        public DataUnit GetData()
        {
            return new DataUnit("INA226")
            {
                { "BusVoltage", GetBusVoltage() },
                { "ShuntVoltage", GetShuntVoltage() },
                { "Current", GetCurrent() },
                { "Power", GetPower() }
            }
            .SetSystem(this.System);
        }

        /// <summary> Checks if the chip's manufacturer register has the correct value. </summary>
        public bool Test()
        {
            ushort MfgID = UtilData.SwapBytes(this.Bus.ReadRegister16(this.Address, (byte)Register.ManufacturerID));
            if (this.TraceLogging) { Log.Trace(this, "Manufacturer check returned 0x" + MfgID.ToString("X4") + " (expected 0x5449)."); }
            return MfgID == 0x5449;
        }

        /// <summary> Takes a new reading from the sensor and stores it for later retrieval. </summary>
        public void UpdateState()
        {
            this.LastReading[0] = UtilData.SwapBytes(this.Bus.ReadRegister16(this.Address, (byte)Register.ShuntVoltage));
            this.LastReading[1] = UtilData.SwapBytes(this.Bus.ReadRegister16(this.Address, (byte)Register.BusVoltage));
            this.LastReading[2] = UtilData.SwapBytes(this.Bus.ReadRegister16(this.Address, (byte)Register.Power));
            this.LastReading[3] = UtilData.SwapBytes(this.Bus.ReadRegister16(this.Address, (byte)Register.Current));
            if (this.TraceLogging) { Log.Trace(this, "Returned data: " + this.LastReading[0].ToString("X4") + "," + this.LastReading[1].ToString("X4") + "," + this.LastReading[2].ToString("X4") + "," + this.LastReading[3].ToString("X4")); }
        }

        /// <summary> Sets configuration and calibration registers with these settings. </summary>
        /// <param name="MaxCurrent"> The maximum current that the device is expected to measure. </param>
        /// <param name="Avg"> The averaging mode to use. </param>
        /// <param name="VBusTime"> The bus voltage conversion time to use. </param>
        /// <param name="VShuntTime"> The shunt voltage conversion time to use. </param>
        private void SetConfig(float MaxCurrent, AveragingMode Avg, ConversionTime VBusTime, ConversionTime VShuntTime)
        {
            // Sets Configuration Register
            ushort Config = 0b0100_0000_0000_0111;
            Config |= (ushort)(((ushort)Avg << 9) & 0b0000_1110_0000_0000); // Averaging mode
            Config |= (ushort)(((ushort)VBusTime << 6) & 0b0000_0001_1100_0000); // VBus conversion time
            Config |= (ushort)(((ushort)VShuntTime << 3) & 0b0000_0000_0011_1000); // VShunt conversion time
            Config = UtilData.SwapBytes(Config);
            this.Bus.WriteRegister16(this.Address, (byte)Register.Configuration, Config);

            // Sets Calibration Register
            this.CurrentMultiplier = Math.Abs(MaxCurrent) / Math.Pow(2, 15);
            this.CalibrationVal = (ushort)Math.Ceiling(0.00512D / (this.CurrentMultiplier * this.Resistor));
            this.CurrentMultiplier = (0.00512D / this.CalibrationVal) / this.Resistor; // Since rounding the value may have slightly changed the multiplier, make sure we are using what the device will.
            if (this.TraceLogging) { Log.Trace(this, "Using current multiplier " + this.CurrentMultiplier + " A/count (calibration value " + this.CalibrationVal + ")."); }
            this.Bus.WriteRegister16(this.Address, (byte)Register.Calibration, UtilData.SwapBytes(this.CalibrationVal));
        }
    }
}
