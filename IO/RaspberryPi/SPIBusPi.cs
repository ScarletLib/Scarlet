using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scarlet.IO.RaspberryPi
{
    public class SPIBusPi : ISPIBus
    {
        private const int DEFAULT_SPEED = 50000;

        private readonly int BusNum;
        private readonly object BusLock = new object();

        public SPIBusPi(int Bus)
        {
            this.BusNum = Bus;
            RaspberryPi.SPISetup(Bus, DEFAULT_SPEED);
        }

        public void SetBusSpeed(int Speed) { RaspberryPi.SPISetup(this.BusNum, Speed); }

        /// <summary> Simultaneously writes/reads data to/from the device. </summary>
        /// <param name="DeviceSelect"> The chip select output to use to choose the desired device. </param>
        /// <param name="Data"> The data to write. </param>
        /// <param name="DataLength"> The amount of data to read/write. </param>
        /// <returns> The data provided back by the device. </returns>
        public byte[] Write(IDigitalOut DeviceSelect, byte[] Data, int DataLength)
        {
            byte[] DataReturn;
            lock (this.BusLock)
            {
                DeviceSelect.SetOutput(false);
                DataReturn = RaspberryPi.SPIRW(this.BusNum, Data, DataLength);
                DeviceSelect.SetOutput(true);
            }
            return DataReturn;
        }

        /// <summary> Does nothing. </summary>
        public void Dispose() { }
    }
}
