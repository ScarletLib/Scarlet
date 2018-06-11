using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Scarlet.IO;
using Scarlet.Utilities;

namespace Scarlet.Components.Sensors
{
    public class MTK3339 : ISensor
    {
        public string System { get; set; }

        private IUARTBus UART;

        public MTK3339(IUARTBus UART)
        {
            
            
        }

        private void InterpretPacket()
        {

        }

        private bool CheckPacket()
        {
            return false;
        }

        /// <summary> Calculates the checksum of a given packet. </summary>
        /// <param name="PacketContents"> The packet to calculate the checksum of. </param>
        /// <param name="UseFull"> If false, then only text between '$' and '*' is taken into account. If true, the entire string is used. </param>
        /// <returns> A 1-byte standard NMEA checksum of the packet contents, or null if the packet is not valid. </returns>
        private byte? GetChecksum(string PacketContents, bool UseFull = false)
        {
            if (string.IsNullOrEmpty(PacketContents)) { return null; }
            string ToCheck = PacketContents;
            if (!UseFull)
            {
                int Start = PacketContents.IndexOf('$');
                int End = PacketContents.IndexOf('*');
                if (Start == -1 || End == -1) { return null; }
                ToCheck = PacketContents.Substring(Start + 1, (End - (Start + 1)));
            }

            byte Checksum = 0;
            for (int i = 0; i < ToCheck.Length; i++) { Checksum ^= Convert.ToByte(ToCheck[i]); }
            return Checksum;
        }

        public bool Test() => false;//HasFix();

        public void UpdateState() { }// => GetCoords();

        public DataUnit GetData()
        {
            return new DataUnit("MTK3339")
            {
                //{ "HasFix", HasFix() }, // TODO: This needs to be stored from last reading instead of re-checked.
                //{ "Lat", this.Latitude},
               // { "Lon", this.Longitude}
            }
            .SetSystem(this.System);
        }
    }
}