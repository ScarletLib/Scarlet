using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Scarlet.Utilities
{
    public static class UtilData
    {
        public static byte[] ToBytes(bool Input) { return EnsureBigEndian(BitConverter.GetBytes(Input), 0, sizeof(bool)); }
        public static byte[] ToBytes(char Input) { return EnsureBigEndian(BitConverter.GetBytes(Input), 0, sizeof(char)); }
        public static byte[] ToBytes(double Input) { return EnsureBigEndian(BitConverter.GetBytes(Input), 0, sizeof(double)); }
        public static byte[] ToBytes(float Input) { return EnsureBigEndian(BitConverter.GetBytes(Input), 0, sizeof(float)); }
        public static byte[] ToBytes(int Input) { return EnsureBigEndian(BitConverter.GetBytes(Input), 0, sizeof(int)); }
        public static byte[] ToBytes(long Input) { return EnsureBigEndian(BitConverter.GetBytes(Input), 0, sizeof(long)); }
        public static byte[] ToBytes(short Input) { return EnsureBigEndian(BitConverter.GetBytes(Input), 0, sizeof(short)); }
        public static byte[] ToBytes(uint Input) { return EnsureBigEndian(BitConverter.GetBytes(Input), 0, sizeof(uint)); }
        public static byte[] ToBytes(ulong Input) { return EnsureBigEndian(BitConverter.GetBytes(Input), 0, sizeof(ulong)); }
        public static byte[] ToBytes(ushort Input) { return EnsureBigEndian(BitConverter.GetBytes(Input), 0, sizeof(ushort)); }
        public static byte[] ToBytes(string Input)
        {
            if (Input == null || Input.Length == 0) { return new byte[0]; }
            char[] Characters = Input.ToCharArray();
            byte[] Output = new byte[Characters.Length * 2];
            for (int i = 0; i < Characters.Length; i++)
            {
                Output[i * 2] = (byte)(Characters[i] >> 8);
                Output[(i * 2) + 1] = (byte)(Characters[i]);
            }
            return Output;
        }

        public static List<object> ToTypes(byte[] Input, params Type[] Types)
        {
            List<byte> Bytes = new List<byte>(Input);
            List<object> Result = new List<object>();
            int i = 0;
            Dictionary<Type, Action> Switch = new Dictionary<Type, Action>
            {
                { typeof(bool), () => { Result.Add(ToBool(Bytes.GetRange(i, sizeof(bool)).ToArray())); i += sizeof(bool); } },
                { typeof(char), () => { Result.Add(ToChar(Bytes.GetRange(i, sizeof(char)).ToArray())); i += sizeof(char); } },
                { typeof(double), () => { Result.Add(ToDouble(Bytes.GetRange(i, sizeof(double)).ToArray())); i += sizeof(double); } },
                { typeof(float), () => { Result.Add(ToFloat(Bytes.GetRange(i, sizeof(float)).ToArray())); i += sizeof(float); } },
                { typeof(int), () => { Result.Add(ToInt(Bytes.GetRange(i, sizeof(int)).ToArray())); i += sizeof(int); } },
                { typeof(long), () => { Result.Add(ToLong(Bytes.GetRange(i, sizeof(long)).ToArray())); i += sizeof(long); } },
                { typeof(short), () => { Result.Add(ToShort(Bytes.GetRange(i, sizeof(short)).ToArray())); i += sizeof(short); } },
                { typeof(uint), () => { Result.Add(ToUInt(Bytes.GetRange(i, sizeof(uint)).ToArray())); i += sizeof(uint); } },
                { typeof(ulong), () => { Result.Add(ToULong(Bytes.GetRange(i, sizeof(ulong)).ToArray())); i += sizeof(ulong); } },
                { typeof(ushort), () => { Result.Add(ToUShort(Bytes.GetRange(i, sizeof(ushort)).ToArray())); i += sizeof(ushort); } },
                { typeof(byte), () => { Result.Add(Bytes[i]); i += sizeof(byte); } },
            };
            foreach (Type t in Types)
            {
                if (!Switch.ContainsKey(t)) { throw new Exception("Unsupported type " + t); }
                if (i + Marshal.SizeOf(t) > Input.Length)
                {
                    int TotalSize = 0;
                    foreach (Type T in Types) { TotalSize += Marshal.SizeOf(T); }
                    throw new Exception("Not enough bytes to parse these types. Given: " + Input.Length + ", Expected: " + TotalSize);
                }
                Switch[t].Invoke();
            }
            return Result;
        }

        public static bool ToBool(byte[] Input, int Start = 0)
        {
            if (Input.Length != sizeof(bool) + Start) { throw new FormatException("Not enough data to complete conversion."); }
            return BitConverter.ToBoolean(EnsureBigEndian(Input, Start, sizeof(bool)), Start);
        }

        public static char ToChar(byte[] Input, int Start = 0)
        {
            if (Input.Length != sizeof(char) + Start) { throw new FormatException("Not enough data to complete conversion."); }
            return BitConverter.ToChar(EnsureBigEndian(Input, Start, sizeof(char)), Start);
        }

        public static double ToDouble(byte[] Input, int Start = 0)
        {
            if (Input.Length != sizeof(double) + Start) { throw new FormatException("Not enough data to complete conversion."); }
            return BitConverter.ToDouble(EnsureBigEndian(Input, Start, sizeof(double)), Start);
        }

        public static float ToFloat(byte[] Input, int Start = 0)
        {
            if (Input.Length != sizeof(float) + Start) { throw new FormatException("Not enough data to complete conversion."); }
            return BitConverter.ToSingle(EnsureBigEndian(Input, Start, sizeof(float)), Start);
        }

        public static int ToInt(byte[] Input, int Start = 0)
        {
            if (Input.Length != sizeof(int) + Start) { throw new FormatException("Not enough data to complete conversion."); }
            return BitConverter.ToInt32(EnsureBigEndian(Input, Start, sizeof(int)), Start);
        }

        public static long ToLong(byte[] Input, int Start = 0)
        {
            if (Input.Length != sizeof(long) + Start) { throw new FormatException("Not enough data to complete conversion."); }
            return BitConverter.ToInt64(EnsureBigEndian(Input, Start, sizeof(long)), Start);
        }

        public static short ToShort(byte[] Input, int Start = 0)
        {
            if (Input.Length != sizeof(short) + Start) { throw new FormatException("Not enough data to complete conversion."); }
            return BitConverter.ToInt16(EnsureBigEndian(Input, Start, sizeof(short)), Start);
        }

        public static uint ToUInt(byte[] Input, int Start = 0)
        {
            if (Input.Length != sizeof(uint) + Start) { throw new FormatException("Not enough data to complete conversion."); }
            return BitConverter.ToUInt32(EnsureBigEndian(Input, Start, sizeof(uint)), Start);
        }

        public static ulong ToULong(byte[] Input, int Start = 0)
        {
            if (Input.Length != sizeof(ulong) + Start) { throw new FormatException("Not enough data to complete conversion."); }
            return BitConverter.ToUInt64(EnsureBigEndian(Input, Start, sizeof(ulong)), Start);
        }

        public static ushort ToUShort(byte[] Input, int Start = 0)
        {
            if (Input.Length != sizeof(ushort) + Start) { throw new FormatException("Not enough data to complete conversion."); }
            return BitConverter.ToUInt16(EnsureBigEndian(Input, Start, sizeof(ushort)), Start);
        }

        /// <summary> Returns a string from the byte representation of the string in unicode </summary>
        /// <param name="Input"> Byte representation of the unicode string </param>
        /// <returns> String representation of the bytes </returns>
        public static string ToString(byte[] Input)
        {
            if (Input == null || Input.Length == 0 || Input.Length % 2 == 1) { throw new FormatException("Given byte[] does not convert to string."); }
            StringBuilder Output = new StringBuilder(Input.Length / 2);
            for (int i = 0; i < Input.Length; i += 2)
            {
                Output.Append((char)(Input[i] << 8 | Input[i + 1]));
            }
            return Output.ToString();
        }

        /// <summary> Tries to convert a byte input (unicode) to a string </summary>
        /// <param name="Input"> Bytes to convert to string </param>
        /// <param name="Output"> Output of the conversion. (Null if failed) </param>
        /// <returns> True if string conversion succeeds </returns>
        public static bool TryToString(byte[] Input, out string Output)
        {
            Output = null;
            try { Output = ToString(Input); }
            catch { return false; }
            return true;
        }

        /// <summary> Determines if the given type is numeric. </summary>
        /// <param name="Type"> Type to determine whether or not it is a numeric </param>
        /// <returns> Returns <c>true</c> if param is a numeric; otherwise returns <c>false</c>. </returns>
        public static bool IsNumericType(Type Type)
        {
            switch (Type.GetTypeCode(Type))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary> Takes a 16bit number, and swaps the locations of the first and last 8b. E.g. 0x54EC would become 0xEC54. </summary>
        /// Intended for use with 16b I2C devices that expect the byte order reversed.
        /// <param name="Input"> The 16b value to swap the bytes of. </param>
        public static ushort SwapBytes(ushort Input)
        {
            return (ushort)(((Input & 0b1111_1111) << 8) | ((Input >> 8) & 0b1111_1111));
        }

        /// <summary> Computes a CRC-16 checksum from given payload. </summary>
        /// <param name="Payload"> Payload of which to compute the checksum. </param>
        public static ushort CRC16(byte[] Payload)
        {
            int i;
            int cksum = 0;

            for (i = 0; i < Payload.Length; i++)
            {
                cksum = crc16_table[(((cksum >> 8) ^ Payload[i]) & 0xFF)] ^ (cksum << 8);
            }

            return (ushort)cksum;
        }

        private static ushort[] crc16_table =
        {
            0x0000, 0x1021, 0x2042, 0x3063, 0x4084, 0x50a5, 0x60c6, 0x70e7,
            0x8108, 0x9129, 0xa14a, 0xb16b, 0xc18c, 0xd1ad, 0xe1ce, 0xf1ef,
            0x1231, 0x0210, 0x3273, 0x2252, 0x52b5, 0x4294, 0x72f7, 0x62d6,
            0x9339, 0x8318, 0xb37b, 0xa35a, 0xd3bd, 0xc39c, 0xf3ff, 0xe3de,
            0x2462, 0x3443, 0x0420, 0x1401, 0x64e6, 0x74c7, 0x44a4, 0x5485,
            0xa56a, 0xb54b, 0x8528, 0x9509, 0xe5ee, 0xf5cf, 0xc5ac, 0xd58d,
            0x3653, 0x2672, 0x1611, 0x0630, 0x76d7, 0x66f6, 0x5695, 0x46b4,
            0xb75b, 0xa77a, 0x9719, 0x8738, 0xf7df, 0xe7fe, 0xd79d, 0xc7bc,
            0x48c4, 0x58e5, 0x6886, 0x78a7, 0x0840, 0x1861, 0x2802, 0x3823,
            0xc9cc, 0xd9ed, 0xe98e, 0xf9af, 0x8948, 0x9969, 0xa90a, 0xb92b,
            0x5af5, 0x4ad4, 0x7ab7, 0x6a96, 0x1a71, 0x0a50, 0x3a33, 0x2a12,
            0xdbfd, 0xcbdc, 0xfbbf, 0xeb9e, 0x9b79, 0x8b58, 0xbb3b, 0xab1a,
            0x6ca6, 0x7c87, 0x4ce4, 0x5cc5, 0x2c22, 0x3c03, 0x0c60, 0x1c41,
            0xedae, 0xfd8f, 0xcdec, 0xddcd, 0xad2a, 0xbd0b, 0x8d68, 0x9d49,
            0x7e97, 0x6eb6, 0x5ed5, 0x4ef4, 0x3e13, 0x2e32, 0x1e51, 0x0e70,
            0xff9f, 0xefbe, 0xdfdd, 0xcffc, 0xbf1b, 0xaf3a, 0x9f59, 0x8f78,
            0x9188, 0x81a9, 0xb1ca, 0xa1eb, 0xd10c, 0xc12d, 0xf14e, 0xe16f,
            0x1080, 0x00a1, 0x30c2, 0x20e3, 0x5004, 0x4025, 0x7046, 0x6067,
            0x83b9, 0x9398, 0xa3fb, 0xb3da, 0xc33d, 0xd31c, 0xe37f, 0xf35e,
            0x02b1, 0x1290, 0x22f3, 0x32d2, 0x4235, 0x5214, 0x6277, 0x7256,
            0xb5ea, 0xa5cb, 0x95a8, 0x8589, 0xf56e, 0xe54f, 0xd52c, 0xc50d,
            0x34e2, 0x24c3, 0x14a0, 0x0481, 0x7466, 0x6447, 0x5424, 0x4405,
            0xa7db, 0xb7fa, 0x8799, 0x97b8, 0xe75f, 0xf77e, 0xc71d, 0xd73c,
            0x26d3, 0x36f2, 0x0691, 0x16b0, 0x6657, 0x7676, 0x4615, 0x5634,
            0xd94c, 0xc96d, 0xf90e, 0xe92f, 0x99c8, 0x89e9, 0xb98a, 0xa9ab,
            0x5844, 0x4865, 0x7806, 0x6827, 0x18c0, 0x08e1, 0x3882, 0x28a3,
            0xcb7d, 0xdb5c, 0xeb3f, 0xfb1e, 0x8bf9, 0x9bd8, 0xabbb, 0xbb9a,
            0x4a75, 0x5a54, 0x6a37, 0x7a16, 0x0af1, 0x1ad0, 0x2ab3, 0x3a92,
            0xfd2e, 0xed0f, 0xdd6c, 0xcd4d, 0xbdaa, 0xad8b, 0x9de8, 0x8dc9,
            0x7c26, 0x6c07, 0x5c64, 0x4c45, 0x3ca2, 0x2c83, 0x1ce0, 0x0cc1,
            0xef1f, 0xff3e, 0xcf5d, 0xdf7c, 0xaf9b, 0xbfba, 0x8fd9, 0x9ff8,
            0x6e17, 0x7e36, 0x4e55, 0x5e74, 0x2e93, 0x3eb2, 0x0ed1, 0x1ef0
        };

        internal static byte[] EnsureBigEndian(byte[] Input, int Start, int Length)
        {
            if (BitConverter.IsLittleEndian) { Array.Reverse(Input, Start, Length); }
            return Input;
        }
    }
}
