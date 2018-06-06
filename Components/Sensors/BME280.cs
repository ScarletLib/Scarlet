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

        public BME280(II2CBus I2CBus, byte DeviceAddress) // TODO: Set default
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
                byte[] DataOut = new byte[];
                byte[] DataIn = this.SPIBus.Write(SPICS, DataOut, Length);
                return DataIn;
            }
            else
            {
                return this.I2CBus.ReadRegister(this.I2CAddress, Register, Length);
            }
        }

        private void DoWrite(byte Register, byte[] Data)
        {

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
