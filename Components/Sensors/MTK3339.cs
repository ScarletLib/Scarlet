using System;
using System.Collections.Generic;
using System.Globalization;
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

        public Data GPSData { get; private set; }

        public MTK3339(IUARTBus UART)
        {
            this.UART = UART;
            
        }

        private bool InterpretPacket(string Packet)
        {
            if (string.IsNullOrEmpty(Packet)) { return false; }
            byte? ExpectedChecksum = GetChecksum(Packet);
            int End = Packet.IndexOf('*');
            if (End == -1 || ExpectedChecksum == null) { return false; }
            if (!byte.TryParse(Packet.Substring(End + 1, 2), NumberStyles.HexNumber, CultureInfo.CurrentCulture, out byte ActualChecksum)) { return false; }
            if (ActualChecksum != ExpectedChecksum) { return false; }

            string[] Words = Packet.Split(',');
            if (Words.Length < 2) { return false; }
            switch (Words[0])
            {
                case "GPGGA": // GPS Fix Data
                    break;
                case "GPVTG": // Track made good + Ground speed
                    break;
                case "GPGSA": // GPS DOP and active satellites
                    break;
                case "GPGSV": // Satellites in view
                    break;
                default:
                    Log.Output(Log.Severity.DEBUG, Log.Source.SENSORS, "MTK3339 received unknown packet: \"" + Packet + "\".");
                    return false;
            }

            return true;
        }

        /// <summary> Calculates the checksum of a given packet. </summary>
        /// <param name="PacketContents"> The packet to calculate the checksum of. </param>
        /// <param name="UseFull"> If false, then only text between '$' and '*' is taken into account. If true, the entire string is used. </param>
        /// <returns> A 1-byte standard NMEA checksum of the packet contents, or null if the packet is not valid. </returns>
        private byte? GetChecksum(string PacketContents, bool UseFull = false)
        {
            string ToCheck = PacketContents;
            if (!UseFull) { ToCheck = TrimPacket(PacketContents); }
            if (string.IsNullOrEmpty(ToCheck)) { return null; }
            
            byte Checksum = 0;
            for (int i = 0; i < ToCheck.Length; i++) { Checksum ^= Convert.ToByte(ToCheck[i]); }
            return Checksum;
        }

        /// <summary> SPlits out the packet data, starting at '$' (exclusive) and ending at '*' (exclusive). </summary>
        /// <param name="PacketContents"> The packet to trim. </param>
        /// <returns> A packet, without leading $ or trailing checksum or '*'. </returns>
        private string TrimPacket(string PacketContents)
        {
            if (string.IsNullOrEmpty(PacketContents)) { return null; }
            int Start = PacketContents.IndexOf('$');
            int End = PacketContents.IndexOf('*');
            if (Start == -1 || End == -1) { return null; }
            return PacketContents.Substring(Start + 1, (End - (Start + 1)));
        }

        public bool Test() => false;//HasFix();

        /// <summary> This sensor does not UpdateState(), as data is pushed rather than pulled. Does nothing. </summary>
        public void UpdateState() { }

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

        public struct Data
        {
            public Satellite[] Satellites;
            public int SatellitesInView;
            public int SatellitesInUse;
            public bool HaveFix;
            public bool FixIs3D;
        }

        public struct Satellite
        {
            /// <summary> The "pseudo-random noise" sequence, or Gold code, that is transmitted to differentiate from other satellites in the constellation. </summary>
            public int PRNID;

            /// <summary> The current elevation, in degrees. </summary>
            public short Elevation;

            /// <summary> The current azimuth, in degrees from true North. </summary>
            public short Azimuth;

            /// <summary> The current signal-to-noise ratio, a measure for connection quality, in dB. Higher is better. </summary>
            public short SNR;
        }
    }
}