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
        public bool TraceLogging { get; set; }

        public Data GPSData { get; private set; }

        private readonly IUARTBus UART;
        private readonly Thread ParseThread;
        private bool Continue = true;

        public MTK3339(IUARTBus UART)
        {
            this.UART = UART;
            this.GPSData = new Data();
            this.ParseThread = new Thread(new ThreadStart(DoParse));
            this.ParseThread.Start();
        }

        public bool Test() => false;//HasFix();

        /// <summary> This sensor does not use UpdateState(), as data is pushed rather than pulled. Does nothing. </summary>
        public void UpdateState() { }

        /// <summary> Stops parsing data from the sensor. </summary>
        public void Stop() { this.Continue = false; }

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

        /// <summary> Sets the rate at which the GPS sends data back. </summary>
        /// <param name="IntervalMS"> The time between samples, in ms. Must be between 100 and 10000. (0.1Hz to 10Hz) </param>
        /// <returns> Always true. (To be implemented) </returns>
        /// <exception cref="InvalidOperationException"> If the requested update rate is out of the acceptable range. </exception>
        public bool SetUpdateInterval(int IntervalMS)
        {
            if (IntervalMS > 10000 || IntervalMS < 100) { throw new InvalidOperationException("Update interval must be between 100 and 10000 ms."); }
            string Packet = AppendChecksum("$PMTK220," + IntervalMS + "*");
            return DoCommand(Encoding.ASCII.GetBytes(Packet + "\r\n"), 3000);
        }

        private void DoParse()
        {
            string DataStream = "";
            this.UART.Flush();
            while(this.Continue)
            {
                byte[] Buffer = new byte[64];
                int ReadQty = this.UART.Read(Buffer.Length, Buffer);
                if (ReadQty <= 0) { continue; }
                DataStream += Encoding.ASCII.GetString(UtilMain.SubArray(Buffer, 0, ReadQty));
                int IndexOfEnd = DataStream.IndexOf("\n");
                if (IndexOfEnd != -1)
                {
                    string Packet = DataStream.Substring(0, IndexOfEnd + 2).Trim();
                    bool Success = InterpretPacket(Packet);
                    Log.Output(Log.Severity.DEBUG, Log.Source.SENSORS, "[MTK3339] Decoded packet \"" + Packet + "\", with success: " + Success);
                    DataStream = DataStream.Substring(IndexOfEnd + 2); // If there is data left, it's the beginning of the next packet.
                }
                else { Thread.Sleep(10); }
            }
        }

        /// <summary> Attempts to interpret and process a packet. </summary>
        /// <param name="Packet"> The received packet to process. </param>
        /// <returns> Whether packet processing succeeded. </returns>
        private bool InterpretPacket(string Packet)
        {
            if (string.IsNullOrEmpty(Packet)) { return false; }
            byte? ExpectedChecksum = GetChecksum(Packet);
            int End = Packet.IndexOf('*');
            if (End == -1 || ExpectedChecksum == null) { return false; }
            if (!byte.TryParse(Packet.Substring(End + 1, 2), NumberStyles.HexNumber, CultureInfo.CurrentCulture, out byte ActualChecksum)) { return false; }
            if (ActualChecksum != ExpectedChecksum) { return false; }

            string[] Words = TrimPacket(Packet).Split(',');
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
                case "GPRMC": // Minimum data set
                    break;
                case "PMTK001": // Command acknowledge
                    break;
                default:
                    Log.Output(Log.Severity.DEBUG, Log.Source.SENSORS, "MTK3339 received unknown packet: \"" + Packet + "\".");
                    return false;
            }

            return true;
        }

        // TODO: Implement response checking and timeout.
        private bool DoCommand(byte[] DataOut, int TimeoutMS)
        {
            this.UART.Write(DataOut);
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

        private string AppendChecksum(string Packet)
        {
            byte? Checksum = GetChecksum(Packet, false);
            if (Checksum == null) { return ""; } // Discard the packet
            else { return Packet + ((byte)Checksum).ToString("X2"); }
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

        public struct Data
        {
            public Satellite[] Satellites;
            public int SatellitesInUse;
            public bool HaveFix;
            public bool FixIs3D;

            public DateTime Time;
            public double Latitude;
            public double Longitude;
            public byte FixQuality;

            public double HorizontalDOP;
            public double Altitude;
            public double GeoidalSeparation;
            public double TimeSinceLastDiffUpdate;
            public int DiffStationID;
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