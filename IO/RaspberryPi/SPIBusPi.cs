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

        public void SetBusSpeed(int Speed) { RaspberryPi.SPISetup(BusNum, Speed); }

        /// <summary> Simultaneously writes/reads data to/from the device. </summary>
        public byte[] Write(IDigitalOut DeviceSelect, byte[] Data, int DataLength)
        {
            byte[] DataReturn;
            lock (this.BusLock)
            {
                DeviceSelect.SetOutput(false);
                DataReturn = RaspberryPi.SPIRW(BusNum, Data, DataLength);
                DeviceSelect.SetOutput(true);
            }
            return DataReturn;
        }

        public void Dispose() { }
    }
}
