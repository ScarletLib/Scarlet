namespace Scarlet.Communications
{
    public class SafePacketWriter
    {
        private PacketWriter Writer;
        public Packet Packet { get { return Writer.Packet; } }

        public SafePacketWriter(PacketWriter Writer) { this.Writer = Writer; }

        public SafePacketWriter(Packet Packet) { this.Writer = new PacketWriter(Packet); }

        public SafePacketWriter(byte ID, bool IsUDP) { this.Writer = new PacketWriter(ID, IsUDP); }

        public SafePacketWriter Put(bool data)   { this.Writer.Put((byte)TypeID.BOOL).Put(data); return this; }
        public SafePacketWriter Put(char data)   { this.Writer.Put((byte)TypeID.CHAR).Put(data); return this; }
        public SafePacketWriter Put(double data) { this.Writer.Put((byte)TypeID.DOUBLE).Put(data); return this; }
        public SafePacketWriter Put(float data)  { this.Writer.Put((byte)TypeID.FLOAT).Put(data); return this; }
        public SafePacketWriter Put(int data)    { this.Writer.Put((byte)TypeID.INT).Put(data); return this; }
        public SafePacketWriter Put(byte data)   { this.Writer.Put((byte)TypeID.BYTE).Put(data); return this; }

        public SafePacketWriter Put(string data)
        {
            this.Writer.Put((byte)TypeID.STRING);
            this.Writer.Put(data.Length);
            this.Writer.Put(data);
            return this;
        }

        public SafePacketWriter Put(byte[] data)
        {
            this.Writer.Put((byte)TypeID.BYTES);
            this.Writer.Put(data.Length);
            this.Writer.Put(data);
            return this;
        }
    }
}
