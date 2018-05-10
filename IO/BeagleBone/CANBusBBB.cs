using System;
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
        [StructLayout(LayoutKind.Sequential)]
        private struct SockAddrCan
        {
            [MarshalAs(UnmanagedType.U2)]
            public ushort CanFamily;

            [MarshalAs(UnmanagedType.I4)]
            public int CanIfIndex;

            [MarshalAs(UnmanagedType.U4)]
            public uint RxId;

            [MarshalAs(UnmanagedType.U4)]
            public uint TxId;
        }

        [StructLayout(LayoutKind.Explicit)]
        private unsafe struct IfRequest
        {
            [FieldOffset(0), MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public fixed byte Name[16];

            [FieldOffset(16), MarshalAs(UnmanagedType.I4)]
            public int IfIndex;

            [FieldOffset(16), MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
            public fixed byte IfrU[24];
        }

        [StructLayout(LayoutKind.Explicit)]
        private unsafe struct CANFrame
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
            public fixed byte Data[8];
        }

        [DllImport("libcan", SetLastError = true)]
        private static extern int InitCan(int CanNum);

        [DllImport("libcan", SetLastError = true)]
        private static extern int Send(int ID, [MarshalAs(UnmanagedType.LPArray)] byte[] Payload, uint Length, int CanNum);

        [DllImport("libcan", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Struct)]
        private static extern CANFrame Read(int CanNum);

        [DllImport("libcan", SetLastError = true)]
        private static extern int Close(int CanNum);

        [DllImport("libc", SetLastError = true)]
        private static extern int socket(int Namespace, int Style, int Protocol);

        [DllImport("libc", SetLastError = true)]
        private static extern unsafe int ioctl(int FileDescriptor, ulong Request, IfRequest* Req);

        [DllImport("libc", SetLastError = true)]
        private static extern unsafe int bind(int FileDescriptor, SockAddrCan* Addr, int AddrLen);

        [DllImport("libc", SetLastError = true)]
        private static extern int read(int FileDescriptor, ref CANFrame Frame, int Size);

        [DllImport("libc", SetLastError = true)]
        private static extern int write(int FileDescriptor, ref CANFrame Frame, int Size);

        [DllImport("libc", SetLastError = true)]
        private static extern int close(int FileDescriptor);

        private const int AF_CAN = 29;
        private const int PF_CAN = AF_CAN;
        private const int SOCK_RAW = 3;
        private const int CAN_RAW = 1;
        private const ulong SIOCGIFINDEX = 0x8933;
        private int Socket;

        internal CANBusBBB(string CanName)
        {
            this.Socket = socket(PF_CAN, SOCK_RAW, CAN_RAW);
            if (this.Socket < 0) { throw new Exception("Error while opening socket. Error code: " + Marshal.GetLastWin32Error()); }

            unsafe
            {
                IfRequest Req = new IfRequest();
                for (int i = 0; i < CanName.Length; i++)
                {
                    Req.Name[i] = Convert.ToByte(CanName[i]);
                }
                Req.Name[CanName.Length] = 0;
                for (int i = 0; i < 16; i++)
                    Console.WriteLine((char)Req.Name[i]);
                if (ioctl(Socket, SIOCGIFINDEX, &Req) < 0) { throw new Exception("Error during IO Control. Error code: " + Marshal.GetLastWin32Error()); }
                SockAddrCan Addr = new SockAddrCan();
                Addr.CanFamily = AF_CAN;
                Addr.CanIfIndex = Req.IfIndex;
                if (bind(Socket, &Addr, Marshal.SizeOf(Addr)) < 0) { throw new Exception("Error while binding socket. Error code: " + Marshal.GetLastWin32Error()); };
            }
        }

        public Tuple<uint, byte[]> Read()
        {
            CANFrame Frame = new CANFrame();
            read(Socket, ref Frame, Marshal.SizeOf(Frame));
            byte[] Payload = new byte[Frame.DataLength];
            unsafe
            {
                for (int i = 0; i < Frame.DataLength; i++)
                {
                    Payload[i] = Frame.Data[i];
                }
            }
            return new Tuple<uint, byte[]>(Frame.CANID, Payload);
        }

        public void Write(byte ID, byte[] Data)
        {
            if (Data.Length > 8) { throw new Exception("CAN Data Length must be less than 8"); }
            unsafe
            {
                CANFrame Frame = new CANFrame();
                Frame.CANID = ID;
                Frame.DataLength = (byte)Data.Length;
                for (int i = 0; i < Data.Length; i++) { Frame.Data[i] = Data[i]; }
                Console.WriteLine("Wrote " + write(Socket, ref Frame, Marshal.SizeOf(Frame)) + " bytes");
                Console.WriteLine("Errno: " + Marshal.GetLastWin32Error());
            }
        }

        public void Dispose()
        {
            close(Socket);
        }
    }
}
