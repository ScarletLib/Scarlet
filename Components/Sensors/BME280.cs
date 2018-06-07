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
    /// Bosch BME280 - Integrated Air Humidity, Pressure and Temperature Sensor
    /// Datasheet: https://download.mikroe.com/documents/datasheets/BST-BME280_DS001-11.pdf
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
            MeasureTemperature = true,

            Mode = Mode.FORCED,
            StandbyTime = StandbyTime.TIME_500us,
            IIRFilterTimeConstant = FilterCoefficient.FILTER_OFF
        };

        private readonly II2CBus I2CBus;
        private readonly byte I2CAddress;
        private readonly ISPIBus SPIBus;
        private readonly IDigitalOut SPICS;
        private readonly bool IsSPI;

        private CompensationParameters CompParams;
        private double Temperature, Humidity, Pressure;

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

        public void Configure(Config Configuration)
        {
            this.CompParams = ReadCompVals();
            ChangeMode(Mode.SLEEP);
            // Do things.
            ChangeMode(Configuration.Mode);
        }

        public void Configure() => Configure(DefaultConfig);

        public void ChangeMode(Mode NewMode)
        {
            // Read CTRL_MEAS
            // Make changes to bits 0, 1
            // Write CTRL_MEAS
        }

        public DataUnit GetData()
        {
            // TODO: Implement DataUnit generation.
            return new DataUnit("BME280")
            {

            }.SetSystem(this.System);
        }

        public bool Test()
        {
            byte[] DeviceData = Read((byte)Register.DEV_ID, 1);
            return (DeviceData != null) && (DeviceData.Length > 0) && (DeviceData[0] == 0x60);
        }

        public void UpdateState()
        {
            // TODO: Read raw values from the device and process them.
            throw new NotImplementedException();
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
        private void WriteSingle(byte Register, byte Data)
        {
            if (this.IsSPI) { this.SPIBus.Write(this.SPICS, new byte[] { (byte)(Register & 0b0111_1111), Data }, 2); }
            else { this.I2CBus.Write(this.I2CAddress, new byte[] { Register, Data }); }
        }

        /// <summary> Writes data into registers at each given location. Useful for write coalescing. Data[i] will be written into Registers[i] for each i. </summary>
        /// <param name="Registers"> The registers to write to. </param>
        /// <param name="Data"> The data to write to each register. </param>
        /// <exception cref="InvalidOperationException"> If Data or Registers are null or 0 length, or if their lengths don't match. </exception>
        private void WriteRegister(byte[] Registers, byte[] Data)
        {
            if (Registers == null || Data == null || Registers.Length == 0 || Data.Length == 0 || Registers.Length != Data.Length) { throw new InvalidOperationException("Register and Data must have contents and matching length."); }
            byte[] DataOut = new byte[Registers.Length * 2];
            for (int i = 0; i < Registers.Length; i++)
            {
                if (this.IsSPI) { DataOut[i * 2] = (byte)(Registers[i] & 0b0111_1111); }
                else { DataOut[i * 2] = Registers[i]; }
                DataOut[(i * 2) + 1] = Data[i];
            }

            if (this.IsSPI) { this.SPIBus.Write(this.SPICS, DataOut, DataOut.Length); }
            else { this.I2CBus.Write(this.I2CAddress, DataOut); }
        }

        /// <summary> Writes data into registers as if the device had auto-increment. </summary>
        /// <param name="StartRegister"> The first register to write data into. </param>
        /// <param name="Data"> The data to write into the registers. </param>
        /// <exception cref="InvalidOperationException"> If Data is null or empty, or the write would go past register 0xFF. </exception>
        private void WriteSequential(byte StartRegister, byte[] Data)
        {
            if (Data == null || Data.Length == 0) { throw new InvalidOperationException("Data must have contents."); }
            if (StartRegister + Data.Length > 0xFF) { throw new InvalidOperationException("Cannot write past register 0xFF."); }
            byte[] Registers = new byte[Data.Length];
            for (byte i = 0; i < Registers.Length; i++) { Registers[i] = (byte)(StartRegister + i); }
            WriteRegister(Registers, Data);
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
            return ((TempInternal * 5 + 128) >> 8) / 100.00D;
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
            Var1 = (((((long)1) << 47) + Var1)) * (this.CompParams.P1) >> 33;
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
            byte[] RegistersLow = Read((byte)Register.CALIBRATION_LOW, 25);
            byte[] RegistersHigh = Read((byte)Register.CALIBRATION_HIGH, 7);
            if (RegistersLow == null || RegistersLow.Length != 25 || RegistersHigh == null || RegistersHigh.Length != 7) { throw new Exception("Failed to get suitable compensation data from device."); }
            CompensationParameters Output = new CompensationParameters()
            {
                T1 = (ushort)(RegistersLow[0] << 8 | RegistersLow[1]),
                T2 = (short)(RegistersLow[2] << 8 | RegistersLow[3]),
                T3 = (short)(RegistersLow[4] << 8 | RegistersLow[5]),
                P1 = (ushort)(RegistersLow[6] << 8 | RegistersLow[7]),
                P2 = (short)(RegistersLow[8] << 8 | RegistersLow[9]),
                P3 = (short)(RegistersLow[10] << 8 | RegistersLow[11]),
                P4 = (short)(RegistersLow[12] << 8 | RegistersLow[13]),
                P5 = (short)(RegistersLow[14] << 8 | RegistersLow[15]),
                P6 = (short)(RegistersLow[16] << 8 | RegistersLow[17]),
                P7 = (short)(RegistersLow[18] << 8 | RegistersLow[19]),
                P8 = (short)(RegistersLow[20] << 8 | RegistersLow[21]),
                P9 = (short)(RegistersLow[22] << 8 | RegistersLow[23]),
                H1 = RegistersLow[24],
                H2 = (short)(RegistersHigh[0] << 8 | RegistersHigh[1]),
                H3 = RegistersHigh[2],
                H4 = (short)(RegistersHigh[3] << 4 | (RegistersHigh[4] & 0b0000_1111)),
                H5 = (short)(((RegistersHigh[4] & 0b1111_0000) >> 4) | RegistersHigh[5] << 4),
                H6 = (sbyte)(RegistersHigh[6])
            };
            return Output;
        }

        public struct Config
        {
            public Oversampling HumidityOversampling;
            public bool MeasureHumidity;
            public Oversampling PressureOversampling;
            public bool MeasurePressure;
            public Oversampling TemperatureOversampling;
            public bool MeasureTemperature;

            public Mode Mode;
            public StandbyTime StandbyTime;
            public FilterCoefficient IIRFilterTimeConstant;
        }

        /// <summary> Stores the factory-set compensation values used during calculation of final readings. </summary>
        private struct CompensationParameters
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
            SLEEP = 0b00,
            FORCED = 0b01,
            NORMAL = 0b11
        }

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

        public enum FilterCoefficient : byte
        {
            FILTER_OFF = 0b000,
            FILTER_2x = 0b001,
            FILTER_4x = 0b010,
            FILTER_8x = 0b011,
            FILTER_16x = 0b100
        }
    }
}
