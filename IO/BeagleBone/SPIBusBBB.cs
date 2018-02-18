﻿using BBBCSIO;
using System;

namespace Scarlet.IO.BeagleBone
{
    public static class SPIBBB
    {
        public static SPIBusBBB SPIBus0 { get; private set; }
        public static SPIBusBBB SPIBus1 { get; private set; }

        /// <summary> Prepares the given SPI ports for use. Should only be called from BeagleBOne.Initialize(). </summary>
        static internal void Initialize(bool[] EnableBuses)
        {
            if (EnableBuses == null || EnableBuses.Length != 2) { throw new Exception("Invalid enable array given to SPIBBB.Initialize."); }
            if (EnableBuses[0]) { SPIBus0 = new SPIBusBBB(0); }
            if (EnableBuses[1]) { SPIBus1 = new SPIBusBBB(1); }
        }
    }

    public class SPIBusBBB : ISPIBus
    {
        private ScarletSPIPortFS Port;

        /// <summary> This should only be initialized from SPIBBB. </summary>
        internal SPIBusBBB(byte ID)
        {
            switch (ID)
            {
                case 0: this.Port = new ScarletSPIPortFS(SPIPortEnum.SPIPORT_0); break;
                case 1: this.Port = new ScarletSPIPortFS(SPIPortEnum.SPIPORT_1); break;
                default: throw new ArgumentOutOfRangeException("Only SPI ports 0 and 1 are supported.");
            }
            this.Port.SetMode(SPIModeEnum.SPI_MODE_0);
            this.Port.SetDefaultSpeedInHz(100000);
        }

        /// <summary> Simultaneously writes/reads data to/from the device. </summary>
        public byte[] Write(IDigitalOut DeviceSelect, byte[] Data, int DataLength)
        {
            byte[] ReceivedData = new byte[DataLength];
            this.Port.SPITransfer(DeviceSelect, Data, ReceivedData, DataLength);
            return ReceivedData;
        }

        public void Dispose()
        {
            this.Port.ClosePort();
            this.Port.Dispose();
        }
    }
}
