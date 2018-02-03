using BBBCSIO;
using System;

namespace Scarlet.IO.BeagleBone
{
    public static class I2CBBB
    {
        public static I2CBusBBB I2CBus1 { get; private set; }
        public static I2CBusBBB I2CBus2 { get; private set; }

        /// <summary> Prepares the given I2C ports for use. Should only be called from BeagleBOne.Initialize(). </summary>
        static internal void Initialize(bool[] EnableBuses)
        {
            if (EnableBuses == null || EnableBuses.Length != 2) { throw new Exception("Invalid enable array given to I2CBBB.Initialize."); }
            if (EnableBuses[0]) { I2CBus1 = new I2CBusBBB(1); }
            if (EnableBuses[1]) { I2CBus2 = new I2CBusBBB(2); }
        }
    }

    public class I2CBusBBB : II2CBus
    {
        private ScarletI2CPortFS Port;

        /// <summary> This should only be initialized from I2CBBB. </summary>
        internal I2CBusBBB(byte ID)
        {
            switch (ID)
            {
                case 1: this.Port = new ScarletI2CPortFS(I2CPortEnum.I2CPORT_1); break;
                case 2: this.Port = new ScarletI2CPortFS(I2CPortEnum.I2CPORT_2); break;
                default: throw new ArgumentOutOfRangeException("Only I2C ports 1 and 2 are supported.");
            }
        }

        /// <summary> Writes the data as given to the device at the specified address. </summary>
        public void Write(byte Address, byte[] Data)
        {
            this.Port.Write(Address, Data, Data.Length);
        }

        /// <summary> Selects the register in the given device, then writes the given data. </summary>
        public void WriteRegister(byte Address, byte Register, byte[] Data)
        {
            byte[] NewData = new byte[Data.Length + 1];
            NewData[0] = Register;
            for(int Index = 1; Index < NewData.Length; Index++) { NewData[Index] = Data[Index - 1]; }
            Write(Address, NewData);
        }

        /// <summary> Reads raw data from the device at the given address. </summary>
        public byte[] Read(byte Address, int DataLength)
        {
            byte[] Buffer = new byte[DataLength];
            this.Port.Read(Address, Buffer, DataLength);
            return Buffer;
        }

        /// <summary> Requests data from the given device at the specified register. </summary>
        public byte[] ReadRegister(byte Address, byte Register, int DataLength)
        {
            Write(Address, new byte[] { Register });
            return Read(Address, DataLength);
        }

        public void Dispose() { } // TODO: Implement.
    }
}
