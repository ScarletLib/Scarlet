﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scarlet.IO.RaspberryPi
{
    public class I2CBusPi : II2CBus
    {

        private int[] DeviceIDs;

        public I2CBusPi()
        {
            this.DeviceIDs = new int[0x7F];
        }

        /// <summary> Reads raw data from the device at the given address. </summary>
        public byte[] Read(byte Address, int DataLength)
        {
            if (this.DeviceIDs[Address] < 1) { this.DeviceIDs[Address] = RaspberryPi.I2CSetup(Address); }
            byte[] Buffer = new byte[DataLength];
            for (int i = 0; i < DataLength; i++)
            {
                Buffer[i] = RaspberryPi.I2CRead(this.DeviceIDs[Address]);
            }
            return Buffer;
        }

        /// <summary> Requests data from the given device at the specified register. </summary>
        public byte[] ReadRegister(byte Address, byte Register, int DataLength)
        {
            Write(Address, new byte[] { Register });
            return Read(Address, DataLength);
        }

        /// <summary> Writes the data as given to the device at the specified address. </summary>
        public void Write(byte Address, byte[] Data)
        {
            if (this.DeviceIDs[Address] < 1) { this.DeviceIDs[Address] = RaspberryPi.I2CSetup(Address); }
            foreach (byte Byte in Data) { RaspberryPi.I2CWrite(this.DeviceIDs[Address], Byte); }
        }

        /// <summary> Selects the register in the given device, then writes the given data. </summary>
        public void WriteRegister(byte Address, byte Register, byte[] Data)
        {
            byte[] NewData = new byte[Data.Length + 1];
            NewData[0] = Register;
            for(int i = 1; i < NewData.Length; i++) { NewData[i] = Data[i - 1]; }
        }

        public void Dispose()
        {
            // TODO: Implement this
            throw new NotImplementedException();
        }
    }
}
