using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Scarlet;
using Scarlet.Components;
using Scarlet.IO;
using Scarlet.IO.BeagleBone;

namespace Scarlet.Components.Sensors
{
    public class MTK3339 : ISensor
    {
        const string UPDATE_200_MSEC = "$PMTK220,200*2C\r\n";
        const string MEAS_200_MSEC = "$PMTK300,200,0,0,0,0*2F\r\n";
        const string GPRMC_GPGGA = "$PMTK314,0,1,0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0*28\r\n";
        const string QUERY_GPGSA = "$PSRF103,02,01,00,01*27\r\n";

        public float Latitude
        {
            get; private set;
        }
        public float Longitude
        {
            get; private set;
        }

        IUARTBus UART;

        public MTK3339(IUARTBus uart)
        {
            this.UART = uart;
            WriteString(GPRMC_GPGGA);
            Thread.Sleep(1);
            WriteString(MEAS_200_MSEC);
            Thread.Sleep(1);
            WriteString(UPDATE_200_MSEC);
            Thread.Sleep(1);
            if (uart.BytesAvailable() > 0) { uart.Read(uart.BytesAvailable(), new byte[uart.BytesAvailable()]); }
        }

        //Returns true if the GPS has a fix and false otherwise. 
        public bool Test() => HasFix();

        public void EventTriggered(object sender, EventArgs e) => throw new NotImplementedException("MTK3339 doesn't have events");

        public void UpdateState() => GetCoords();

        public bool HasFix()
        {
            WriteString(QUERY_GPGSA);
            string[] result = Read();
            if (result.Length < 3)
                return false;
            return result[2] != "1";
        }

        private string[] Read()
        {
            string GpsResult = "";
            byte PrevChar = 0;
            while (PrevChar != '\n')
            {
                if (UART.BytesAvailable() < 1) { continue; }
                byte[] Result = new byte[UART.BytesAvailable()];
                UART.Read(Result.Length, Result);
                GpsResult += Encoding.ASCII.GetString(Result);
                PrevChar = Result[Result.Length - 1];
            }
            string[] GpSplit = GpsResult.Split(',');
            return GpSplit;
        }

        //Returns a tuple with the GPS coordinates, with Latitude first and Longitude second. 
        //Latitude is negative if the degrees are South and positive if the degrees are North.
        //Longitude is negative if the degrees are West and positive if the degrees are East. 
        public Tuple<float, float> GetCoords()
        {
            string[] Info = Read();
            switch (Info[0])
            {
                case "$GPGGA":
                    Latitude = RawToDeg(Info[2]);
                    string LatDir = Info[3];
                    Longitude = RawToDeg(Info[4]);
                    string LngDir = Info[5];
                    if (LatDir == "S")
                        Latitude = -Latitude;
                    if (LngDir == "W")
                        Longitude = -Longitude;
                    goto default;
                default:
                    return new Tuple<float, float>(Latitude, Longitude);

            }
        }

        private float RawToDeg(string Val)
        {
            string[] GPSplit = Val.Split('.');
            float Deg = float.Parse(GPSplit[0].Substring(0, GPSplit[0].Length - 2));
            float Min = float.Parse(GPSplit[0].Substring(GPSplit[0].Length - 2) + '.' + GPSplit[1]);
            return Deg + Min / 60.0f;
        }

        private void WriteString(string s) => UART.Write(Encoding.ASCII.GetBytes(s));
    }
}
