﻿using System;
using System.Collections.Generic;
using System.Text;
using Scarlet.Utilities;

namespace Scarlet.Communications
{
    /// <summary> This class is intended to contain packet data. </summary>
    public class Message : ICloneable
    {
        public byte[] Timestamp;  // Stores message timestamp (Unix time format)
        public byte ID;           // Stores message ID
        public byte[] Payload;    // Stored message data (discluding timestamp and ID)

        /// <summary>
        /// Constructs a message given raw data, likes when received via network.
        /// Data encoded as such:
        /// Timestamp: RawData[0] through RawData[3]
        /// ID: RawData[4]
        /// Payload: Remainder (RawData[5] though end)
        /// </summary>
        /// <param name="RawData"> Incoming data array </param>
        public Message(byte[] RawData)
        {
            if (RawData.Length < 5) { throw new ArgumentException("Raw data not sufficient for packet. Must be at least 5 bytes long."); }
            this.Timestamp = UtilMain.SubArray(RawData, 0, 4);
            this.ID = RawData[4];
            if (RawData.Length > 5) { this.Payload = UtilMain.SubArray(RawData, 5, RawData.Length - 5); }
            else { this.Payload = new byte[0]; }
        }

        /// <summary> Constructs a message given data that is already split. </summary>
        /// <param name="ID"> The packet ID, used to determine how it is handled at the recipient. </param>
        /// <param name="Payload"> The packet's data content. </param>
        /// <param name="Timestamp"> The timestamp of the packet. If null or invalid, the current time gets set. </param>
        public Message(byte ID, byte[] Payload = null, byte[] Timestamp = null)
        {
            this.Payload = Payload ?? new byte[0];
            if (Timestamp == null || Timestamp.Length != 4) { this.Timestamp = Packet.GetCurrentTime(); }
            else { this.Timestamp = Timestamp; }
            this.ID = ID;
        }

        /// <summary> Sets the timestamp. Must be 4 or more bytes (only first 4 used). </summary>
        /// <param name="Time"> The new timestamp. </param>
        public void SetTime(byte[] Time)
        {
            if (Time.Length < 4) { throw new ArgumentException("Timestamp must be 4 bytes."); }
            this.Timestamp = UtilMain.SubArray(Time, 0, 4);
        }

        /// <summary> Appends data to the end of payload. If payload is currently null, create it. </summary>
        /// <param name="NewData"> New Data to append. </param>
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

        /// <summary> Get a part of current data. </summary>
        /// <param name="Offset"> Index to start copying. </param>
        /// <param name="Size"> Size of data to copy. </param>
        /// <returns> Data with index in range [Offset, Offset + Size). </returns>
        public byte[] GetDataSlice(int Offset, int Size)
        {
            byte[] Data = new byte[Size];
            Buffer.BlockCopy(this.Payload, Offset, Data, 0, Size);
            return Data;
        }


        /// <summary> Get a part of current data from a offset to the end. </summary>
        /// <param name="Offset"> Index to start copying. </param>
        /// <returns> Data from `Offset` to the end. </returns>
        public byte[] GetDataSlice(int Offset)
        {
            return GetDataSlice(Offset, this.Payload.Length - Offset);
        }

        /// <summary>
        /// Returns the raw data
        /// in structure:
        /// Timestamp data[0] to data[3]
        /// ID at data[4]
        /// Data encoded after data[4], i.e. data[5:]
        /// </summary>
        /// <returns> Returns all data in message </returns>
        public byte[] GetRawData()
        {
            List<byte> Output = new List<byte>();
            Output.AddRange(this.Timestamp);
            Output.Add(this.ID);
            Output.AddRange(this.Payload);
            return Output.ToArray();
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
            Message ClonedMsg = (Message)this.MemberwiseClone();
            ClonedMsg.Timestamp = this.Timestamp != null ? (byte[])this.Timestamp.Clone() : null;
            ClonedMsg.Payload = this.Payload != null ? (byte[])this.Payload.Clone() : null;
            return ClonedMsg;
        }

    }

}
