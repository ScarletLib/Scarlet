using Scarlet.Utilities;

namespace Scarlet.Communications
{
    public class PacketWriter
    {
        public Packet Packet;

        public PacketWriter(byte ID, bool IsUDP)
        {
            this.Packet = new Packet(new Message(ID), IsUDP);
        }

        public PacketWriter(Packet Packet)
        {
            this.Packet = Packet;
        }

        public PacketWriter Put(bool data)   { this.Packet.AppendData(UtilData.ToBytes(data)); return this; }
        public PacketWriter Put(char data)   { this.Packet.AppendData(UtilData.ToBytes(data)); return this; }
        public PacketWriter Put(double data) { this.Packet.AppendData(UtilData.ToBytes(data)); return this; }
        public PacketWriter Put(float data)  { this.Packet.AppendData(UtilData.ToBytes(data)); return this; }
        public PacketWriter Put(int data)    { this.Packet.AppendData(UtilData.ToBytes(data)); return this; }
        public PacketWriter Put(string data) { this.Packet.AppendData(UtilData.ToBytes(data)); return this; }
        public PacketWriter Put(byte[] data)   { this.Packet.AppendData(data); return this; }
    }
}
