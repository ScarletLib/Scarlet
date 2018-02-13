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
        [StructLayout(LayoutKind.Explicit)]
        private struct CanFrame
        {
            [FieldOffset(0), MarshalAs(UnmanagedType.I4)]
            int CanId;

            [FieldOffset(4), MarshalAs(UnmanagedType.U1)]
            byte CanDLC;

            [FieldOffset(5), MarshalAs(UnmanagedType.U1)]
            byte Padding;

            [FieldOffset(6), MarshalAs(UnmanagedType.U1)]
            byte Reserved0;

            [FieldOffset(7), MarshalAs(UnmanagedType.U1)]
            byte Reserved1;

            //The original source forced this to 8-byte alignment with __attribute__((aligned(8)))
            //If something breaks, maybe fix it?
            [FieldOffset(8), MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            byte[] Data;

        }

        [StructLayout(LayoutKind.Explicit)]
        private struct SockAddrCan
        {
            [FieldOffset(0), MarshalAs(UnmanagedType.U2)]
            short CanIfIndex;

            [FieldOffset(2), MarshalAs(UnmanagedType.U4)]
            uint RxID;

            [FieldOffset(6), MarshalAs(UnmanagedType.U4)]
            uint TxID;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct IFreq
        {
            [FieldOffset(0), MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            byte[] Name;

            [FieldOffset(16), MarshalAs(UnmanagedType.ByValArray, SizeConst = 21)]
            byte[] Useless;
        }

        const int PF_CAN = 29;
        const int SOCK_RAW = 3;
        const int CAN_RAW = 1;

        private BBBPin TX, RX;

        [DllImport("libc.so")]
        private static extern int socket(int Domain, int Type, int Protocol);

        [DllImport("libc.so")]
        private static extern int ioctl(int FD, ulong Request, __arglist);
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
