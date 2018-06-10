using System;
using Scarlet.IO;
using Scarlet.Utilities;

namespace Scarlet.Components.Sensors
{
    /// <summary>
    /// Bosch BME280 - Integrated Air Humidity, Pressure and Temperature Sensor
    /// Datasheet v1.1: https://download.mikroe.com/documents/datasheets/BST-BME280_DS001-11.pdf
    /// Datasheet v1.19: https://ae-bst.resource.bosch.com/media/_tech/media/datasheets/BST-BMP280-DS001-19.pdf
    ///     NOTE: This is for a similar device, the BMP280 (P for pressure only). This describes register LSB/MSB order, but has no info about humidity.
    /// </summary>
    public class BME280 : ISensor
    {
        public string System { get; set; }

        public static readonly Config DefaultConfig = new Config()
        {
            HumidityOversampling = Oversampling.OS_1x,
            MeasureHumidity = true,
            PressureOversampling = Oversampling.OS_1x,
            MeasurePressure = true,
            TemperatureOversampling = Oversampling.OS_1x,

            Mode = Mode.NORMAL,
            StandbyTime = StandbyTime.TIME_500us,
            IIRFilterTimeConstant = FilterCoefficient.FILTER_OFF,

            Use3WireSPI = false
        };

        public double Temperature { get; private set; }
        public double Humidity { get; private set; }
        public double Pressure { get; private set; }

        private readonly II2CBus I2CBus;
        private readonly byte I2CAddress;
        private readonly ISPIBus SPIBus;
        private readonly IDigitalOut SPICS;
        private readonly bool IsSPI;

        private CompensationParameters CompParams;
        private Config Configuration;

        public BME280(II2CBus I2CBus, byte DeviceAddress = 0x76)
        {
            this.IsSPI = false;
            this.I2CBus = I2CBus;
            this.I2CAddress = DeviceAddress;
        }

        public BME280(ISPIBus SPIBus, IDigitalOut ChipSelect)
        {
            this.IsSPI = true;
            this.SPIBus = SPIBus;
            this.SPICS = ChipSelect;
        }

        /// <summary> Sets the device's registers to match the given <c>Configuration</c>. </summary>
        /// <param name="Configuration"> The configuration to apply. </param>
        public void Configure(Config Configuration)
        {
            this.Configuration = Configuration;
            this.CompParams = ReadCompVals();
            ChangeMode(Mode.SLEEP);

            byte[] RawData = Read((byte)Register.CTRL_HUM, 4);

            byte osrs_h = (byte)(Configuration.MeasureHumidity ? (byte)Configuration.HumidityOversampling : 0);
            byte Ctrl_Hum = (byte)((RawData[0] & 0b1111_1000) | osrs_h & 0b111);

            byte osrs_t = (byte)Configuration.TemperatureOversampling;
            byte osrs_p = (byte)(Configuration.MeasurePressure ? (byte)Configuration.PressureOversampling : 0);
            byte Ctrl_Meas = (byte)(((osrs_t << 5) & 0b1110_0000) | ((osrs_p << 2) & 0b0001_1100) | ((byte)Mode.SLEEP & 0b0000_0011)); // Stays in SLEEP mode, we'll exit once these writes are all done.

            byte Config = (byte)((((byte)Configuration.StandbyTime << 5) & 0b1110_0000) | (((byte)Configuration.IIRFilterTimeConstant << 2) & 0b0001_1100) | (RawData[3] & 0b0000_0010) | (Configuration.Use3WireSPI ? 1 : 0));

            Write((byte)Register.CTRL_HUM, Ctrl_Hum);
            Write((byte)Register.CTRL_MEAS, Ctrl_Meas);
            Write((byte)Register.CONFIG, Config);

            ChangeMode(Configuration.Mode);
        }

        /// <summary> Configures the device with default settings, which should be good enough for basic functionality. </summary>
        public void Configure() => Configure(DefaultConfig);

        /// <summary> Changes the acquisition mode of the device, or brings it in/out of SLEEP. </summary>
        /// <param name="NewMode"> The <c>Mode</c> to put the device into. </param>
        public void ChangeMode(Mode NewMode)
        {
            byte Config = Read((byte)Register.CTRL_MEAS, 1)[0];
            Config = (byte)((Config & 0b1111_1100) | ((byte)NewMode & 0b0000_0011));
            Write((byte)Register.CTRL_MEAS, Config);
        }

        /// <summary> Gets the sensor's current readings in an easy to store format. </summary>
        public DataUnit GetData()
        {
            return new DataUnit("BME280")
            {
                { "Temperature", this.Temperature },
                { "Pressure", this.Pressure },
                { "Humidity", this.Humidity }
            }.SetSystem(this.System);
        }

        /// <summary> Checks if the device is responding by querying a known, constant register value. </summary>
        public bool Test()
        {
            byte[] DeviceData = Read((byte)Register.DEV_ID, 1);
            return (DeviceData != null) && (DeviceData.Length > 0) && (DeviceData[0] == 0x60);
        }

        /// <summary> Restarts the device. </summary>
        public void Reset() => Write((byte)Register.RESET, 0x6B);

        /// <summary> Gets new readings from the device. </summary>
        /// <exception cref="Exception"> If getting readings from the device fails. </exception>
        public void UpdateState()
        {
            byte[] RawData = Read((byte)Register.PRESSURE_MSB, 8);
            if (RawData == null || RawData.Length != 8) { throw new Exception("Failed to get readings from device."); }

            int RawPressure = (RawData[0] << 12) | (RawData[1] << 4) | ((RawData[2] & 0b1111_0000) >> 4);
            int RawTemperature = (RawData[3] << 12) | (RawData[4] << 4) | ((RawData[5] & 0b1111_0000) >> 4);
            int RawHumidity = (RawData[6] << 8) | (RawData[7]);

            int IntTempCal = ProcessTemperatureInternal(RawTemperature);
            this.Temperature = ProcessTemperature(IntTempCal);
            this.Pressure = (this.Configuration.MeasurePressure ? (ProcessPressure(RawPressure, IntTempCal)) : double.NaN);
            this.Humidity = (this.Configuration.MeasureHumidity ? (ProcessHumidity(RawHumidity, IntTempCal)) : double.NaN);
        }

        #region Read/Write
        private byte[] Read(byte Register, byte Length)
        {
            if (this.IsSPI)
            {
                byte[] DataOut = new byte[Length + 1];
                DataOut[0] = (byte)((Register & 0b0111_1111) | 0b1000_0000);
                byte[] DataIn = this.SPIBus.Write(this.SPICS, DataOut, DataOut.Length);
                return UtilMain.SubArray(DataIn, 1, Length);
            }
            else
            {
                return this.I2CBus.ReadRegister(this.I2CAddress, Register, Length);
            }
        }

        /// <summary> Writes a single register. </summary>
        /// <param name="Register"> The register address to write to. </param>
        /// <param name="Data"> The data to write. </param>
        private void Write(byte Register, byte Data)
        {
            if (this.IsSPI) { this.SPIBus.Write(this.SPICS, new byte[] { (byte)(Register & 0b0111_1111), Data }, 2); }
            else { this.I2CBus.WriteRegister(this.I2CAddress, Register, new byte[] { Data }); }
        }
        #endregion

        #region Data Processing
        /// <summary> Gets temperature in format useful for compensation of other values. </summary>
        /// <remarks> Corresponds to <c>t_fine</c> in original code. </remarks>
        /// <source> Datasheet, page 23 </source>
        /// <param name="RawTemp"> The temperature as read directly from the device's registers. </param>
        private int ProcessTemperatureInternal(int RawTemp)
        {
            int Var1, Var2;
            Var1 = ((((RawTemp >> 3) - (this.CompParams.T1 << 1))) * (this.CompParams.T2)) >> 11;
            Var2 = (((((RawTemp >> 4) - (this.CompParams.T1)) * ((RawTemp >> 4) - (this.CompParams.T1))) >> 12) * (this.CompParams.T3)) >> 14;
            return Var1 + Var2;
        }

        /// <summary> Gets the actual external temperature. </summary>
        /// <source> Datasheet, page 23 </source>
        /// <param name="TempInternal"> The temperature produced by <c>ProcessTemperatureInternal()</c>. </param>
        /// <returns> The external temperature reading in increments of 0.01 degrees Celsius. </returns>
        private double ProcessTemperature(int TempInternal)
        {
            return (((TempInternal * 5) + 128) >> 8) / 100.00D;
        }

        /// <summary> Gets the pressure reading. </summary>
        /// <param name="RawPress"> The pressure as read directly from the device's registers. </param>
        /// <param name="TempComp"> The temperature produced by <c>ProcessTemperatureInternal()</c>. </param>
        /// <returns> Pressure reading in 1/256 increments, in Pa. </returns>
        private double ProcessPressure(int RawPress, int TempComp)
        {
            long Var1, Var2, P;
            Var1 = ((long)TempComp) - 128000;
            Var2 = Var1 * Var1 * this.CompParams.P6;
            Var2 = Var2 + ((Var1 * this.CompParams.P5) << 17);
            Var2 = Var2 + (((long)this.CompParams.P4) << 35);
            Var1 = ((Var1 * Var1 * this.CompParams.P3) >> 8) + ((Var1 * this.CompParams.P2) << 12);
            Var1 = (((((long)1) << 47) + Var1)) * ((this.CompParams.P1) >> 33);
            if (Var1 == 0) { return 0; }
            P = 1048576 - RawPress;
            P = (((P << 31) - Var2) * 3125) / Var1;
            Var1 = ((this.CompParams.P9) * (P >> 13) * (P >> 13)) >> 25;
            Var2 = ((this.CompParams.P8) * P) >> 19;
            P = ((P + Var1 + Var2) >> 8) + (((long)this.CompParams.P7) << 4);
            return P / 256.000D;
        }

        /// <summary> Gets the humidity reading. </summary>
        /// <param name="RawHumid"> The humidity as read directly from the device's registers. </param>
        /// <param name="TempComp"> The temperature produced by <c>ProcessTemperatureInternal()</c>. </param>
        /// <returns> Humidity reading in 1/1024 increments, in % from 0-100. </returns>
        private double ProcessHumidity(int RawHumid, int TempComp)
        {
            int Var1;
            Var1 = (TempComp - 76800);
            Var1 = (((((RawHumid << 14) - ((this.CompParams.H4) << 20) - ((this.CompParams.H5) * Var1)) + 16384) >> 15) * (((((((Var1 * (this.CompParams.H6)) >> 10) *
                (((Var1 * (this.CompParams.H3)) >> 11) + 32768)) >> 10) + 2097152) * (this.CompParams.H2) + 8192) >> 14));
            Var1 = (Var1 - (((((Var1 >> 15) * (Var1 >> 15)) >> 7) * this.CompParams.H1) >> 4));
            Var1 = (Var1 < 0 ? 0 : Var1);
            Var1 = (Var1 > 419430400 ? 419430400 : Var1);
            return (Var1 >> 12) / 1024.000D;
        }
        #endregion

        /// <summary> Reads compensation values from the device. </summary>
        /// <remarks> This only needs to be done once, as they are hard-coded on the chip, so will never change. </remarks>
        private CompensationParameters ReadCompVals()
        {
            byte[] RegistersLow = Read((byte)Register.CALIBRATION_LOW, 26);
            byte[] RegistersHigh = Read((byte)Register.CALIBRATION_HIGH, 7);
            if (RegistersLow == null || RegistersLow.Length != 26 || RegistersHigh == null || RegistersHigh.Length != 7) { throw new Exception("Failed to get suitable compensation data from device."); }
            CompensationParameters Output = new CompensationParameters()
            {
                T1 = (ushort)(RegistersLow[0] | RegistersLow[1] << 8),
                T2 = (short)(RegistersLow[2] | RegistersLow[3] << 8),
                T3 = (short)(RegistersLow[4] | RegistersLow[5] << 8),
                P1 = (ushort)(RegistersLow[6] | RegistersLow[7] << 8),
                P2 = (short)(RegistersLow[8] | RegistersLow[9] << 8),
                P3 = (short)(RegistersLow[10] | RegistersLow[11] << 8),
                P4 = (short)(RegistersLow[12] | RegistersLow[13] << 8),
                P5 = (short)(RegistersLow[14] | RegistersLow[15] << 8),
                P6 = (short)(RegistersLow[16] | RegistersLow[17] << 8),
                P7 = (short)(RegistersLow[18] | RegistersLow[19] << 8),
                P8 = (short)(RegistersLow[20] | RegistersLow[21] << 8),
                P9 = (short)(RegistersLow[22] | RegistersLow[23] << 8), // Register A0 appears to be skipped.
                H1 = RegistersLow[25],
                H2 = (short)(RegistersHigh[0] | RegistersHigh[1] << 8),
                H3 = RegistersHigh[2],
                H4 = (short)(RegistersHigh[3] << 4 | (RegistersHigh[4] & 0b0000_1111)),
                H5 = (short)(((RegistersHigh[4] & 0b1111_0000) >> 4) | RegistersHigh[5] << 4),
                H6 = (sbyte)(RegistersHigh[6])
            };
            Log.Output(Log.Severity.DEBUG, Log.Source.SENSORS, "Got BME280 compensation data: " + UtilMain.BytesToNiceString(RegistersLow, true) + " and " + UtilMain.BytesToNiceString(RegistersHigh, true));
            return Output;
        }

        #region Structs and Enums
        public struct Config
        {
            public Oversampling HumidityOversampling;
            public bool MeasureHumidity;
            public Oversampling PressureOversampling;
            public bool MeasurePressure;
            public Oversampling TemperatureOversampling;

            public Mode Mode;
            public StandbyTime StandbyTime;
            public FilterCoefficient IIRFilterTimeConstant;

            public bool Use3WireSPI;
        }

        /// <summary> Stores the factory-set compensation values used during calculation of final readings. </summary>
        public struct CompensationParameters
        {
            public ushort T1;
            public short T2;
            public short T3;
            public ushort P1;
            public short P2;
            public short P3;
            public short P4;
            public short P5;
            public short P6;
            public short P7;
            public short P8;
            public short P9;
            public byte H1;
            public short H2;
            public byte H3;
            public short H4;
            public short H5;
            public sbyte H6;
        }

        private enum Register : byte
        {
            DEV_ID = 0xD0,
            RESET = 0xE0,
            CALIBRATION_LOW = 0x88,
            CALIBRATION_HIGH = 0xE1,
            CTRL_HUM = 0xF2, // Sets humidity acquisition options: Must write to CTRL_MEAS before changes are applied.
            STATUS = 0xF3,
            CTRL_MEAS = 0xF4, // Sets pressure and temperature acquisition options
            CONFIG = 0xF5, // Must be in sleep mode
            PRESSURE_MSB = 0xF7,
            PRESSURE_LSB = 0xF8,
            PRESSURE_XLSB = 0xF9,
            TEMPERATURE_MSB = 0xFA,
            TEMPERATURE_LSB = 0xFB,
            TEMPERATURE_XLSB = 0xFC,
            HUMIDITY_MSB = 0xFD,
            HUMIDITY_LSB = 0xFE
        }

        public enum Oversampling : byte
        {
            OS_1x = 0b001,
            OS_2x = 0b010,
            OS_4x = 0b011,
            OS_8x = 0b100,
            OS_16x = 0b101
        }

        public enum Mode : byte
        {
            /// <summary> No operation, all registers accessible, lowest power, default after startup. </summary>
            SLEEP = 0b00,

            /// <summary> Performs one measurement, stores results and then returns to sleep mode. </summary>
            FORCED = 0b01,

            /// <summary> Continuously takes readings, waiting <c>StandbyTime</c> between readings. </summary>
            NORMAL = 0b11
        }

        /// <summary> How long to wait between readings in NORMAL operation mode. </summary>
        public enum StandbyTime : byte
        {
            TIME_500us = 0b000,
            TIME_62500us = 0b001,
            TIME_125000us = 0b010,
            TIME_250000us = 0b011,
            TIME_500000us = 0b100,
            TIME_1000000us = 0b101,
            TIME_10000us = 0b110,
            TIME_20000us = 0b111
        }

        /// <summary>
        /// Filter to average out several readings for more consistent data, losing fast changes.
        /// Only applicable to temperature and pressure (not humidity).
        /// </summary>
        public enum FilterCoefficient : byte
        {
            FILTER_OFF = 0b000,
            FILTER_2x = 0b001,
            FILTER_4x = 0b010,
            FILTER_8x = 0b011,
            FILTER_16x = 0b100
        }
        #endregion
    }
}
