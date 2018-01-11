using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scarlet.IO.BeagleBone
{
    public static class CANBBB
    {
        public static CANBusBBB CANBus0 { get; private set; }
        public static CANBusBBB CANBus1 { get; private set; }

        /// <summary> Prepares the given PWM ports for use. Should only be called from BeagleBone.Initialize(). </summary>
        static internal void Initialize(bool En0, bool En1)
        {
            if (En0) { CANBus0 = new CANBusBBB(new BBBPin[] { BBBPin.P9_20, BBBPin.P9_19 }); }
            if (En1) { CANBus1 = new CANBusBBB(new BBBPin[] { BBBPin.P9_26, BBBPin.P9_24 }); }
        }

        /// <summary> Converts a pin number to the corresponding CAN bus ID. </summary>
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

    public class CANBusBBB // : ICANBus // TODO: Create
    {
        internal CANBusBBB(BBBPin[] Pins) // TX, RX
        {

        }
    }
}
