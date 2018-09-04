using System;
using System.Collections.Generic;
using System.Text;
using Scarlet.Utilities;

namespace Scarlet.Communications
{
    public class Message : ICloneable
    {
        public byte[] Timestamp; // Length 8
        public byte ID;
        public byte[] Payload; // The actual data

        /// <summary>
        /// Constructs a message given raw data, likes when received via network.
        /// Data encoded as such:
        /// Timestamp: RawData[0] through RawData[7]
        /// ID: RawData[8]
        /// Length: RawData[9] through RawData[10] (Checked, not stored)
        /// Payload: Remainder (RawData[11] though end)
        /// </summary>
        /// <param name="RawData"> Incoming data array </param>
        public Message(byte[] RawData)
        {
            if (RawData.Length < Packet.HEADER_LENGTH) { throw new ArgumentException("Raw data not sufficient for packet. Must be at least " + Packet.HEADER_LENGTH + " bytes long."); }
            this.Timestamp = UtilMain.SubArray(RawData, 0, sizeof(long));
            this.ID = RawData[8];
            ushort ExpectedLength = UtilData.ToUShort(RawData, 9);
            if (ExpectedLength != RawData.Length) { Log.Output(Log.Severity.WARNING, Log.Source.NETWORK, "Packet data length does not match its descriptor. Expected " + ExpectedLength + " bytes, got " + RawData.Length + "."); }
            if (RawData.Length > Packet.HEADER_LENGTH) { this.Payload = UtilMain.SubArray(RawData, Packet.HEADER_LENGTH, (RawData.Length - Packet.HEADER_LENGTH)); }
            else { this.Payload = new byte[0]; }
        }

        /// <summary> Constructs a message given data that is already split. </summary>
        /// <param name="ID"> The packet ID, used to determine how it is handled at the recipient. </param>
        /// <param name="Payload"> The packet's data content. </param>
        /// <param name="Timestamp"> The timestamp of the packet. If null or invalid, the current time gets set. </param>
        public Message(byte ID, byte[] Payload = null, byte[] Timestamp = null)
        {
            this.Payload = Payload ?? new byte[0];
            if (Timestamp == null || Timestamp.Length != sizeof(long)) { this.Timestamp = Packet.GetCurrentTime(); }
            else { this.Timestamp = Timestamp; }
            this.ID = ID;
        }

        /// <summary> Constructs a message given data that is already split. </summary>
        /// <param name="ID"> The packet ID, used to determine how it is handled at the recipient. </param>
        /// <param name="Payload"> The packet's data content. Will be converted to bytes for you. </param>
        /// <param name="Timestamp"> The timestamp of the packet. If null or invalid, the current time gets set. </param>
        public Message(byte ID, string Payload = null, byte[] Timestamp = null) : this(ID, UtilData.ToBytes(Payload), Timestamp) { }

        /// <summary> Sets the timestamp. Must be 8 bytes. </summary>
        /// <param name="Time"> The new timestamp. </param>
        public void SetTime(byte[] Time)
        {
            if (Time.Length != sizeof(long)) { throw new ArgumentException("Timestamp must be 8 bytes."); }
            this.Timestamp = new byte[8];
            Array.Copy(Time, this.Timestamp, 8);
        }

        /// <summary> Appends data to the end of <see cref="Payload"/>. If <see cref="Payload"/> is currently null, create it. </summary>
        /// <param name="NewData"> New data to append. </param>
        public void AppendData(byte[] NewData)
        {
            if (this.Payload == null) { this.Payload = NewData; }
            else
            {
                byte[] UpdatedPayload = new byte[this.Payload.Length + NewData.Length];
                Buffer.BlockCopy(this.Payload, 0, UpdatedPayload, 0, this.Payload.Length);
                Buffer.BlockCopy(NewData, 0, UpdatedPayload, this.Payload.Length, NewData.Length);
                this.Payload = UpdatedPayload;
            }
        }

        /// <summary>
        /// Returns the raw data in structure:
        /// Timestamp data[0] to data[7]
        /// ID at data[8]
        /// Length at data[9] to data[10]
        /// Data encoded data[11] and onwards
        /// </summary>
        /// <returns> Returns all data, ready for sending over the network. </returns>
        public byte[] GetRawData()
        {
            if (this.Payload == null) { this.Payload = new byte[0]; }
            byte[] Output = new byte[Packet.HEADER_LENGTH + this.Payload.Length];
            Buffer.BlockCopy(this.Timestamp, 0, Output, 0, this.Timestamp.Length);
            Output[8] = this.ID;
            Buffer.BlockCopy(UtilData.ToBytes((ushort)(Packet.HEADER_LENGTH + this.Payload.Length)), 0, Output, 8, sizeof(ushort));
            Buffer.BlockCopy(this.Payload, 0, Output, Packet.HEADER_LENGTH - 1, this.Payload.Length);
            return Output;
        }

        /// <summary> Formats the Messages's contents to be human-readable. </summary>
        public override string ToString()
        {
            StringBuilder Str = new StringBuilder();
            Str.Append("Packet = Time:(0x");
            Str.Append(UtilMain.BytesToNiceString(this.Timestamp, false));
            Str.Append(") ID:(0x");
            Str.Append(this.ID.ToString("X2"));
            Str.Append(") Data:(0x");
            Str.Append(UtilMain.BytesToNiceString(this.Payload, true));
            Str.Append(')');
            return Str.ToString();
        }

        public object Clone()
        {
            Message ClonedMsg = (Message)MemberwiseClone();
            ClonedMsg.Timestamp = this.Timestamp != null ? (byte[])this.Timestamp.Clone() : null;
            ClonedMsg.Payload = this.Payload != null ? (byte[])this.Payload.Clone() : null;
            return ClonedMsg;
        }

    }

}
