using System;

namespace Scarlet.Communications
{
    public class SafePacketScanner
    {
        private PacketScanner Scanner;

        public SafePacketScanner(PacketScanner Scanner) { this.Scanner = Scanner; }

        public SafePacketScanner(Packet Packet) { this.Scanner = new PacketScanner(Packet); }

        public bool HasType(TypeID Type) { return (byte)Type == Scanner.PeekNextByte(); }

        private void AssertType(TypeID Type)
        {
            if((byte)Type != Scanner.NextByte()) {
                TypeID NextType = (TypeID)Scanner.PeekNextByte();
                String StrExpectedType = Type.ToString().ToLower();
                String StrActualType = Type.ToString().ToLower();
                throw new InvalidOperationException("Expected " + StrExpectedType + ", Actual type " + StrActualType + ".");
            }
        }

        public bool   NextBool()   { AssertType(TypeID.BOOL); return Scanner.NextBool(); }
        public char   NextChar()   { AssertType(TypeID.CHAR); return Scanner.NextChar(); }
        public double NextDouble() { AssertType(TypeID.DOUBLE); return Scanner.NextDouble(); }
        public float  NextFloat()  { AssertType(TypeID.FLOAT); return Scanner.NextFloat(); }
        public int    NextInt()    { AssertType(TypeID.INT); return Scanner.NextInt(); }
        public byte   NextByte()   { AssertType(TypeID.BYTE); return Scanner.NextByte(); }

        public byte[] NextBytes()
        {
            AssertType(TypeID.BYTES);
            int Length = Scanner.NextInt();
            return Scanner.NextBytes(Length);
        }

        public string NextString()
        {
            AssertType(TypeID.STRING);
            int Length = Scanner.NextInt();
            return Scanner.NextString(Length*2);
        }
    }
}
