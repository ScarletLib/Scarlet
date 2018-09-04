using System;
using System.Net.Sockets;
using Scarlet.Utilities;

namespace Scarlet.Communications
{
    public class Packet : ICloneable
    {
        /// <summary> Defines how many bytes are in the header (non-data portion at the beginning) of all <see cref="Packet"/>s. </summary>
        public const int HEADER_LENGTH = sizeof(long) + sizeof(byte) + sizeof(ushort); // Timestamp + ID + Length

        /// <summary> The name of the recipient for sent packets, or the name of the sender for received packets. </summary>
        public string Endpoint { get; set; }

        /// <summary> Whether this packet [will be sent / was received] via UDP. </summary>
        public bool IsUDP { get; set; }

        /// <summary> The minimum connection quality to wait for before sending this packet. Only relevant if <see cref="Client"/> was configured with <see cref="LatencyMeasurementMode.FULL"/>. </summary>
        /// <remarks> Not set for received packets. </remarks>
        public byte MinimumConnectionQuality { get; set; }

        /// <summary> How long the <see cref="Packet"/> can wait, in 10s of ms (i.e. 20 = 200ms), in a queue before sending is no longer desirable. 0 for no timeout. </summary>
        /// <remarks> Not set for received packets. </remarks>
        public ushort Timeout { get; set; }

        // Relayed through the underlying Message object.
        public byte ID { get => this.Data.ID; }
        public byte[] TimestampRaw { get => this.Data.Timestamp; }
        public DateTime Timestamp { get => new DateTime(UtilData.ToLong(this.Data.Timestamp)); }
        public byte[] Payload { get => this.Data.Payload; }

        private Message Data;

        /// <summary> Meant for received packets. </summary>
        /// <param name="Data"> The packet data. </param>
        /// <param name="IsUDP"> Defines whether or not packet is a UDP message. </param>
        /// <param name="Endpoint"> The endpoint where this packet was received from. </param>
        internal Packet(Message Data, bool IsUDP, string Endpoint = null)
        {
            this.IsUDP = IsUDP;
            this.Data = Data;
            this.Endpoint = Endpoint;
        }

        /// <summary> Meant for packets to be sent. </summary>
        /// <param name="ID"> The packet ID, determining what action will be taken upon receipt. </param>
        /// <param name="IsUDP"> Defines whether or not packet is a UDP message. </param>
        /// <param name="Endpoint"> The destination where this packet will be sent. (Only used with <see cref="Server"/>). </param>
        public Packet(byte ID, bool IsUDP, string Endpoint = null) : this(new Message(ID, new byte[0]), IsUDP, Endpoint) { }
        
        /// <summary> Appends data to the packet. </summary>
        /// <param name="NewData"> Data to append to the packet. </param>
        public void AppendData(byte[] NewData) { this.Data.AppendData(NewData); }

        /// <summary> Prepares the packet for sending, then returns the raw data. </summary>
        /// <param name="Timestamp"> If you want to apply a custom timestamp, set this. Otherwise, it will be set to the current time. Ignored if null or not length 8. </param>
        /// <returns> The raw data, ready to be sent. </returns>
        public byte[] GetForSend(byte[] Timestamp = null)
        {
            if (Timestamp == null || Timestamp.Length != sizeof(long)) { UpdateTimestamp(); } // Sets the timestamp to the current time.
            else { this.Data.SetTime(Timestamp); } // Sets the timestamp to the one provided.
            return this.Data.GetRawData();
        }

        /// <summary> Return the length of packet in bytes. </summary>
        /// <returns> Length of packet in bytes. </returns>
        public int GetLength() { return GetForSend().Length; }

        /// <summary> Updates the packet timestamp to the current time. </summary>
        public void UpdateTimestamp() { this.Data.SetTime(GetCurrentTime()); }

        /// <summary> Gets the current time as a byte array for use in packets. </summary>
        public static byte[] GetCurrentTime() => UtilData.ToBytes(DateTime.Now.Ticks);

        /// <summary> Formats the Packet's contents to be human-readable. </summary>
        public override string ToString()
        {
            return (this.IsUDP ? "UDP " : "TCP ") + this.Data.ToString();
        }

        public object Clone()
        {
            Packet Clone = (Packet)this.MemberwiseClone(); // This leaves reference objects as references.
            Clone.Data = this.Data != null ? (Message)this.Data.Clone() : null;
            Clone.Endpoint = string.Copy(this.Endpoint);
            return Clone;
        }

        /// <summary> Returns a packet from given bytes and a protocol type for the packet. </summary>
        /// <param name="PacketBytes"> Raw bytes of packet data. </param>
        /// <param name="Protocol"> Protocol to use for the Packet. </param>
        internal static Packet FromBytes(byte[] PacketBytes, ProtocolType Protocol)
        {
            return new Packet(new Message(PacketBytes), Protocol == ProtocolType.Udp);
        }
    }
}