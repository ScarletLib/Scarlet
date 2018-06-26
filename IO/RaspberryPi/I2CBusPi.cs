using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scarlet.IO.RaspberryPi
{
    public class I2CBusPi : II2CBus
    {
        private readonly object BusLock = new object();
        private int[] DeviceIDs;

        public I2CBusPi()
        {
            this.DeviceIDs = new int[0x7F];
        }

        /// <summary> Reads 8-bit data from the device at the given address. </summary>
        /// <param name="Address"> The address of the device to read from. </param>
        /// <param name="DataLength"> How many bytes to read. </param>
        public byte[] Read(byte Address, int DataLength)
        {
            CheckDev(Address);
            byte[] Buffer = new byte[DataLength];
            lock (this.BusLock)
            {
                for (int i = 0; i < DataLength; i++)
                {
                    Buffer[i] = RaspberryPi.I2CRead(this.DeviceIDs[Address]);
                }
            }
            return Buffer;
        }

        /// <summary> Requests 8-bit data from the given device at the specified register. </summary>
        /// <param name="Address"> The address of the device to read from. </param>
        /// <param name="Register"> The register to start reading data from. </param>
        /// <param name="DataLength"> How many bytes to read. If more than 1, subsequent registers are polled. </param>
        public byte[] ReadRegister(byte Address, byte Register, int DataLength)
        {
            CheckDev(Address);
            byte[] Data = new byte[DataLength];
            lock (this.BusLock)
            {
                for (int i = 0; i < DataLength; i++)
                {
                    Data[i] = RaspberryPi.I2CReadRegister8(this.DeviceIDs[Address], (byte)(Register + i));
                }
            }
            return Data;
        }

        /// <summary> Writes the data as given to the device at the specified address. </summary>
        /// <param name="Address"> The address of the device to write data to. </param>
        /// <param name="Data"> The 8-bit data to write to the device. </param>
        public void Write(byte Address, byte[] Data)
        {
            CheckDev(Address);
            lock (this.BusLock)
            {
                foreach (byte Byte in Data) { RaspberryPi.I2CWrite(this.DeviceIDs[Address], Byte); }
            }
        }

        /// <summary> Selects the register in the given device, then writes the given data. </summary>
        /// <param name="Address"> The address of the device to write data to. </param>
        /// <param name="Register"> The register to start writing data at. </param>
        /// <param name="Data"> The 8-bit data to write. If more than 1 byte, subsequent registers are written. </param>
        public void WriteRegister(byte Address, byte Register, byte[] Data)
        {
            CheckDev(Address);
            lock (this.BusLock)
            {
                for (int i = 0; i < Data.Length; i++)
                {
                    RaspberryPi.I2CWriteRegister8(this.DeviceIDs[Address], (byte)(Register + i), Data[i]);
                }
            }
        }

        /// <summary> Writes data to a 16-bit register. </summary>
        /// <param name="Address"> The address of the device to write to. </param>
        /// <param name="Register"> The register to write data to. </param>
        /// <param name="Data"> The 16-bit data to write to the register. </param>
        public void WriteRegister16(byte Address, byte Register, ushort Data)
        {
            CheckDev(Address);
            lock (this.BusLock) { RaspberryPi.I2CWriteRegister16(this.DeviceIDs[Address], Register, Data); }
        }

        /// <summary> Reads data from a 16-bit register. </summary>
        /// <param name="Address"> The address of the device to read from. </param>
        /// <param name="Register"> The register to read from. </param>
        /// <returns> The 16-bit data in the register. </returns>
        public ushort ReadRegister16(byte Address, byte Register)
        {
            CheckDev(Address);
            lock (this.BusLock) { return RaspberryPi.I2CReadRegister16(this.DeviceIDs[Address], Register); }
        }

        /// <summary> Does nothing. </summary>
        public void Dispose() { }

        private void CheckDev(byte Address)
        {
            if (this.DeviceIDs[Address] < 1) { this.DeviceIDs[Address] = RaspberryPi.I2CSetup(Address); }
        }
    }
}
