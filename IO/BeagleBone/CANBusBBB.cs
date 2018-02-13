using System;
using System.Runtime.InteropServices;

namespace Scarlet.IO.BeagleBone
{
    public static class CANBBB
    {
        public static CANBusBBB CANBus0 { get; private set; }
        public static CANBusBBB CANBus1 { get; private set; }

        /// <summary> Prepares the given CAN busses for use. Should only be called from BeagleBone.Initialize(). </summary>
        static internal void Initialize(bool[] EnableBuses)
        {
            if (EnableBuses == null || EnableBuses.Length != 2) { throw new Exception("Invalid enable array given to CANBBB.Initialize."); }
            if (EnableBuses[0]) { CANBus0 = new CANBusBBB(new BBBPin[] { BBBPin.P9_20, BBBPin.P9_19 }); }
            if (EnableBuses[1]) { CANBus1 = new CANBusBBB(new BBBPin[] { BBBPin.P9_26, BBBPin.P9_24 }); }
        }

        /// <summary> Converts a pin number to the corresponding CAN bus ID. 255 if invalid. </summary>
        static internal byte PinToCANBus(BBBPin Pin)
        {
            switch (Pin)
            {
                case BBBPin.P9_19:
                case BBBPin.P9_20: return 0;

                case BBBPin.P9_24:
                case BBBPin.P9_26: return 1;
            }
            return 255;
        }
    }

    public class CANBusBBB : ICANBus
    {


        // TODO: Implement CAN functionality.
        internal CANBusBBB(BBBPin[] Pins) // TX, RX
        {
        }

        public byte[] Read(byte Address, int DataLength)
        {
            throw new NotImplementedException();
        }

        public void Write(byte Address, byte[] Data)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
