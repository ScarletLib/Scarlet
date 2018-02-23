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
        [StructLayout(LayoutKind.Explicit)]
        private struct CANFrame
        {
            [FieldOffset(0), MarshalAs(UnmanagedType.I4)]
            public int CANID;

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

        [StructLayout(LayoutKind.Explicit)]
        private struct SockAddrCAN
        {
            [FieldOffset(0)]
            public ushort CANFamily;

            [FieldOffset(2), MarshalAs(UnmanagedType.U2)]
            public int CANIFIndex;

            [FieldOffset(6), MarshalAs(UnmanagedType.U4)]
            public uint RxID;

            [FieldOffset(10), MarshalAs(UnmanagedType.U4)]
            public uint TxID;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct IFReq
        {
            [FieldOffset(0), MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] Name;

            [FieldOffset(16), MarshalAs(UnmanagedType.ByValArray, SizeConst = 21)]
            public byte[] Useless;
        }

        private const int AF_CAN = 29;
        private const int PF_CAN = 29;
        private const int SOCK_RAW = 3;
        private const int CAN_RAW = 1;
        private const ulong SIOCGIFINDEX = 0x8933;
        private string CanName;
        private byte[] CanNameArr;
        private int Socket;

        [DllImport("libc")]
        private static extern int socket(int Domain, int Type, int Protocol);

        [DllImport("libc")]
        private static extern int ioctl(int FD, ulong Index, ref IFReq Request);

        [DllImport("libc")]
        private static extern int bind(int SockFD, ref SockAddrCAN Addr, int AddrLen);

        [DllImport("libc")]
        private static extern int write(int FileDescriptor, ref CANFrame Frame, int Size);

        [DllImport("libc")]
        private static extern int read(int FileDescriptor, [MarshalAs(UnmanagedType.LPArray)] byte[] Buffer, int Bytes);

        [DllImport("libc")]
        private static extern int fflush(int FileDescriptor);

        // TODO: Implement CAN functionality.
        internal CANBusBBB(string Name) // TX, RX
        {
            this.Socket = socket(PF_CAN, SOCK_RAW, CAN_RAW);
            this.CanName = Name;
            this.CanNameArr = System.Text.Encoding.ASCII.GetBytes(CanName);
            if (Socket < 0) { throw new Exception("Error while opening socket."); }
        }

        public byte[] Read(byte Address, int DataLength)
        {
            byte[] Buffer = new byte[DataLength];
            read(Socket, Buffer, DataLength);
            return Buffer;
        }

        public void Write(byte Address, byte[] Data)
        {
            IFReq Request = new IFReq();
            Request.Name = new byte[16];
            Request.Useless = new byte[21];
            Array.Copy(CanNameArr, Request.Name, CanNameArr.Length);
            Request.Name[CanNameArr.Length] = 0; //Make sure it's null terminated

            ioctl(Socket, SIOCGIFINDEX, ref Request);

            SockAddrCAN Addr = new SockAddrCAN();
            Addr.CANFamily = AF_CAN;
            Addr.CANIFIndex = Request.Useless[3] << 24 | Request.Useless[2] << 16 | Request.Useless[1] << 8 | Request.Useless[0];

            bind(Socket, ref Addr, Marshal.SizeOf(Addr));

            CANFrame Frame = new CANFrame();
            Frame.CANID = 0x123;
            Frame.Data = new byte[8];
            for (int i = 0; i < Data.Length; i += 8)
            {
                Frame.DataLength = (byte)(Data.Length - i < 8 ? Data.Length - i : 8);
                Array.Copy(Data, i, Frame.Data, 0, Frame.DataLength);
                write(Socket, ref Frame, Marshal.SizeOf(Frame));
            }
            fflush(Socket);
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
