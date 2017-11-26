using Scarlet.Utilities;

using System;

namespace Scarlet.Communications
{
    public class PacketScanner
    {
        private int Cursor;
        public Packet Packet;

        public PacketScanner(Packet Packet)
        {
            this.Packet = Packet;
            this.Cursor = 0;
        }

        private byte[] NextData(int Size)
        {
            if (this.Cursor + Size > this.Packet.Data.Payload.Length) { throw new InvalidOperationException("Reached the end of package data"); }

            byte[] Data = this.Packet.GetDataSlice(this.Cursor, Size);
            this.Cursor += Size;
            return Data;
        }

        public bool   NextBool()   { return UtilData.ToBool(NextData(sizeof(bool))); }
        public char   NextChar()   { return UtilData.ToChar(NextData(sizeof(char))); }
        public double NextDouble() { return UtilData.ToDouble(NextData(sizeof(double))); }
        public float  NextFloat()  { return UtilData.ToFloat(NextData(sizeof(float))); }
        public int    NextInt()    { return UtilData.ToInt(NextData(sizeof(int))); }

        public string NextString()
        {
            if (this.Cursor >= this.Packet.Data.Payload.Length) { throw new InvalidOperationException("Reached the end of package data"); }

            string data = UtilData.ToString(this.Packet.GetDataSlice(Cursor));
            this.Cursor = Packet.Data.Payload.Length;
            return data;
        }

        public string NextString(int Length)
        {
            if (this.Cursor >= this.Packet.Data.Payload.Length) { throw new InvalidOperationException("Reached the end of package data"); }

            string data = UtilData.ToString(this.Packet.GetDataSlice(Cursor, Length));
            this.Cursor += Length;
            return data;
        }

        public byte[] NextBytes()
        {
            if (this.Cursor >= this.Packet.Data.Payload.Length) { throw new InvalidOperationException("Reached the end of package data"); }

            byte[] data = this.Packet.GetDataSlice(Cursor);
            this.Cursor = Packet.Data.Payload.Length;
            return data;
        }

        public byte[] NextBytes(int Length)
        {
            if (this.Cursor >= this.Packet.Data.Payload.Length) { throw new InvalidOperationException("Reached the end of package data"); }

            byte[] data = this.Packet.GetDataSlice(Cursor, Length);
            this.Cursor += Length;
            return data;
        }

        public byte NextByte()
        {
            if (this.Cursor >= this.Packet.Data.Payload.Length) { throw new InvalidOperationException("Reached the end of package data"); }

            byte data = Packet.Data.Payload[Cursor];
            Cursor++;
            return data;
        }

        public byte PeekNextByte() { return Packet.Data.Payload[Cursor]; }
    }
}
