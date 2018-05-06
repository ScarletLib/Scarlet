using System;
<<<<<<< HEAD
using System.Collections.Generic;
using System.Linq;
=======
>>>>>>> upstream/CAN
using System.Runtime.InteropServices;

namespace Scarlet.IO.BeagleBone
{
    public static class CANBBB
    {
        public static CANBusBBB CANBus0 { get; private set; }
        public static CANBusBBB CANBus1 { get; private set; }

        static CANBBB()
        {
            Initialize(new bool[] { true, false });
        }

        /// <summary> Prepares the given CAN buses for use. Should only be called from BeagleBone.Initialize(). </summary>
        static internal void Initialize(bool[] EnableBuses)
        {
            if (EnableBuses == null || EnableBuses.Length != 2) { throw new Exception("Invalid enable array given to CANBBB.Initialize."); }
            if (EnableBuses[0]) { CANBus0 = new CANBusBBB("can0"); }
            if (EnableBuses[1]) { CANBus1 = new CANBusBBB("can1"); }
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
<<<<<<< HEAD
        [StructLayout(LayoutKind.Explicit)]
        private struct CANFrame
        {
            [FieldOffset(0), MarshalAs(UnmanagedType.U4)]
            public uint CANID;

            [FieldOffset(4), MarshalAs(UnmanagedType.U1)]
            public byte DataLength;

            [FieldOffset(5), MarshalAs(UnmanagedType.U1)]
            public byte Padding;

            [FieldOffset(6), MarshalAs(UnmanagedType.U1)]
            public byte Reserved0;

            [FieldOffset(7), MarshalAs(UnmanagedType.U1)]
            public byte Reserved1;

            //The original source forced this to 8-byte alignment with __attribute__((aligned(8)))
            //If something breaks, maybe fix it?
            [FieldOffset(8), MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] Data;
        }

        [DllImport("libcan", SetLastError = true)]
        private static extern int InitCan([MarshalAs(UnmanagedType.LPStr)] string CanName);

        [DllImport("libcan", SetLastError = true)]
        private static extern int Send(int ID, [MarshalAs(UnmanagedType.LPArray)] byte[] Payload, uint Length);

        internal CANBusBBB(string Name) // TX, RX
        {
            int Result = InitCan(Name);
            if (Result < 0) { throw new Exception("Error while opening socket"); }
=======


        // TODO: Implement CAN functionality.
        internal CANBusBBB(BBBPin[] Pins) // TX, RX
        {
>>>>>>> upstream/CAN
        }

        public byte[] Read(byte Address, int DataLength)
        {
            byte[] Buffer = new byte[DataLength];
            int BytesRead = read(Socket, Buffer, DataLength);
            return Buffer;
        }

        public void Write(byte ID, byte[] Data)
        {
            Send(ID, Data, (uint)Data.Length);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
