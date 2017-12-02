using Scarlet.Utilities;

namespace Scarlet.Communications
{
    /// <summary> A helper class that can construct a <see cref="Scarlet.Communications.Packet"/>. </summary>
    public class PacketWriter
    {
        public Packet Packet; // The packet to write to

        /// <summary> Write into a new <see cref="Scarlet.Communications.Packet"/>. </summary>
        /// <param name="ID"> Packet ID. </param>
        /// <param name="IsUDP"> Whether it is a UDP packet. </param>
        public PacketWriter(byte ID, bool IsUDP = false, string Endpoint = null)
        {
            this.Packet = new Packet(new Message(ID), IsUDP, Endpoint);
        }

        /// <summary> Write into an existing <see cref="Scarlet.Communications.Packet"/>. </summary>
        /// <param name="Packet"> The packet to write into. </param>
        public PacketWriter(Packet Packet)
        {
            this.Packet = Packet;
        }

        /// <summary> Put data into the packet. </summary>
        /// <remarks> Those methods can be chained together because they return `this`. </remarks>
        /// <param name="data"> Data to append into the packet. </param>
        /// <returns> `this` </returns>
        public PacketWriter Put(bool data)   { this.Packet.Data.AppendData(UtilData.ToBytes(data)); return this; }
        public PacketWriter Put(char data)   { this.Packet.Data.AppendData(UtilData.ToBytes(data)); return this; }
        public PacketWriter Put(double data) { this.Packet.Data.AppendData(UtilData.ToBytes(data)); return this; }
        public PacketWriter Put(float data)  { this.Packet.Data.AppendData(UtilData.ToBytes(data)); return this; }
        public PacketWriter Put(int data)    { this.Packet.Data.AppendData(UtilData.ToBytes(data)); return this; }
        public PacketWriter Put(string data) { this.Packet.Data.AppendData(UtilData.ToBytes(data)); return this; }
        public PacketWriter Put(byte[] data) { this.Packet.Data.AppendData(data); return this; }
        public PacketWriter Put(byte data)   { this.Packet.Data.AppendData(new byte[1] { data }); return this; }
    }
}
