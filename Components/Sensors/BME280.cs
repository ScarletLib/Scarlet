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

        private readonly II2CBus I2CBus;
        private readonly byte I2CAddress;
        private readonly ISPIBus SPIBus;
        private readonly IDigitalOut SPICS;
        private readonly bool IsSPI;

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
        //TODO: Finish this.
        private byte[] DoRead(byte Register, byte Length)
        {
            if (this.IsSPI)
            {
                byte[] DataOut = new byte[Length];
                byte[] DataIn = this.SPIBus.Write(this.SPICS, DataOut, Length);
                return DataIn;
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
            if (this.IsSPI) { this.SPIBus.Write(this.SPICS, new byte[] { Register, Data }, 2); }
            else { this.I2CBus.Write(this.I2CAddress, new byte[] { Register, Data }); }
        }

        /// <summary> Writes data into registers at each given location. Useful for write coalescing. Data[i] will be written into Registers[i] for each i. </summary>
        /// <param name="Registers"> The registers to write to. </param>
        /// <param name="Data"> The data to write to each register. </param>
        /// <exception cref="InvalidOperationException"> If Data or Regsiters are null or 0 length, or if their lengths don't match. </exception>
        private void WriteRegister(byte[] Registers, byte[] Data)
        {
            if (Registers == null || Data == null || Registers.Length == 0 || Data.Length == 0 || Registers.Length != Data.Length) { throw new InvalidOperationException("Register and Data must have contents and matching length."); }
            byte[] DataOut = new byte[Registers.Length * 2];
            for (int i = 0; i < Registers.Length; i++)
            {
                DataOut[i * 2] = Registers[i];
                DataOut[(i * 2) + 1] = Data[i];
            }
            
            if(this.IsSPI) { this.SPIBus.Write(this.SPICS, DataOut, DataOut.Length); }
            else { this.I2CBus.Write(this.I2CAddress, DataOut); }
        }

        /// <summary> Writes data into registers as if the device had auto-increment. </summary>
        /// <param name="StartRegister"> The first register to write data into. </param>
        /// <param name="Data"></param>
        /// <exception cref="InvalidOperationException"> If Data is null or empty, or the write would go past register 0xFF. </exception>
        private void WriteSequential(byte StartRegister, byte[] Data)
        {
            if (Data == null || Data.Length == 0) { throw new InvalidOperationException("Data must have contents."); }
            if (StartRegister + Data.Length > 0xFF) { throw new InvalidOperationException("Cannot write past register 0xFF."); }
            byte[] Registers = new byte[Data.Length];
            for (byte i = 0; i < Registers.Length; i++) { Registers[i] = (byte)(StartRegister + i); }
            WriteRegister(Registers, Data);
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
            byte[] DeviceData = DoRead((byte)Register.DEV_ID, 1);
            return (DeviceData != null) && (DeviceData.Length > 0) && (DeviceData[0] == 0x60);
        }

        public void UpdateState()
        {
            throw new NotImplementedException();
        }

        private enum Register : byte
        {
            DEV_ID = 0xD0,
            RESET = 0xE0,
            CALIBRATION_LOW = 0x88,
            CALIBRATION_HIGH = 0xE1,
            CTRL_HUM = 0xF2, // Sets humidity acquisition options: Must write to CTRL_MEAS before changes are applied.
            STATUS = 0xF3,
            CTRL_MEAS = 0xF4, // Sets pressure and temperature acquision options
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
            NONE = 0b000,
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
            NONE = 0b000,
            FILTER_2x = 0b001,
            FILTER_4x = 0b010,
            FILTER_8x = 0b011,
            FILTER_16x = 0b100
        }
    }
}
