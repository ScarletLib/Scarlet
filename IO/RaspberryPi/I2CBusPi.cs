using System;
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

        private void CheckDev(byte Address)
        {
            if (this.DeviceIDs[Address] < 1) { this.DeviceIDs[Address] = RaspberryPi.I2CSetup(Address); }
        }

        /// <summary> Reads raw data from the device at the given address. </summary>
        public byte[] Read(byte Address, int DataLength)
        {
            CheckDev(Address);
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
            CheckDev(Address);
            byte[] Data = new byte[DataLength];
            for(int i = 0; i < DataLength; i++)
            {
                Data[i] = RaspberryPi.I2CReadRegister8(this.DeviceIDs[Address], (byte)(Register + i));
            }
            return Data;
        }

        /// <summary> Writes the data as given to the device at the specified address. </summary>
        public void Write(byte Address, byte[] Data)
        {
            CheckDev(Address);
            foreach (byte Byte in Data) { RaspberryPi.I2CWrite(this.DeviceIDs[Address], Byte); }
        }

        /// <summary> Selects the register in the given device, then writes the given data. </summary>
        public void WriteRegister(byte Address, byte Register, byte[] Data)
        {
            CheckDev(Address);
            for (int i = 0; i < Data.Length; i++)
            {
                RaspberryPi.I2CWriteRegister8(this.DeviceIDs[Address], (byte)(Register + i), Data[i]);
            }
        }

        public void WriteRegister16(byte Address, byte Register, ushort Data)
        {
            CheckDev(Address);
            RaspberryPi.I2CWriteRegister16(this.DeviceIDs[Address], Register, Data);
        }

        public ushort ReadRegister16(byte Address, byte Register)
        {
            CheckDev(Address);
            return RaspberryPi.I2CReadRegister16(this.DeviceIDs[Address], Register);
        }

        public void Dispose()
        {
            // TODO: Implement this
            throw new NotImplementedException();
        }
    }
}
