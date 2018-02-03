using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Scarlet;
using Scarlet.Components;
using Scarlet.IO;
using Scarlet.IO.BeagleBone;

namespace UseRobot
{
    public class MTK3339 : ISensor
    {
        string UPDATE_200_MSEC = "$PMTK220,200*2C\r\n";
        string MEAS_200_MSEC = "$PMTK300,200,0,0,0,0*2F\r\n";
        string GPRMC_GPGGA = "$PMTK314,0,1,0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0*28\r\n";
        string QUERY_GPGSA = "$PSRF103,02,01,00,01*27\r\n";

        public bool Fix
        {
            get
            {
                write_string(QUERY_GPGSA);
                string[] result = read();
                if (result.Length < 3)
                    return false;
                return result[2] != "1";
            }
        }

        public float Latitude
        {
            get;
            private set;
        }
        public float Longitude
        {
            get;
            private set;
        }

        IUARTBus uart;

        public MTK3339(IUARTBus uart)
        {
            this.uart = uart;
            write_string(GPRMC_GPGGA);
            Thread.Sleep(1);
            write_string(MEAS_200_MSEC);
            Thread.Sleep(1);
            write_string(UPDATE_200_MSEC);
            Thread.Sleep(1);
            if (uart.BytesAvailable() > 0)
                uart.Read(uart.BytesAvailable(), new byte[uart.BytesAvailable()]);
        }

        public bool Test()
        {
            return Fix;
        }

        public void EventTriggered(object sender, EventArgs e)
        {
            throw new NotImplementedException("MTK3339 doesn't have events");
        }

        public void UpdateState()
        {
            GetCoords();
        }

        private string[] read()
        {
            string gps_result = "";
            byte prev_char = 0;
            while (prev_char != '\n')
            {
                if (uart.BytesAvailable() < 1)
                    continue;
                byte[] result = new byte[uart.BytesAvailable()];
                uart.Read(result.Length, result);
                gps_result += Encoding.ASCII.GetString(result);
                prev_char = result[result.Length - 1];
            }
            string[] gpsplit = gps_result.Split(',');
            return gpsplit;
        }

        public Tuple<float, float> GetCoords()
        {
            string[] info = read();
            switch (info[0])
            {
                case "$GPGGA":
                    Latitude = raw_to_deg(info[2]);
                    string lat_dir = info[3];
                    Longitude = raw_to_deg(info[4]);
                    string lng_dir = info[5];
                    if (lat_dir == "S")
                        Latitude = -Latitude;
                    if (lng_dir == "W")
                        Longitude = -Longitude;
                    goto default;
                default:
                    return new Tuple<float, float>(Latitude, Longitude);

            }
        }

        private float raw_to_deg(string val)
        {
            string[] gpsplit = val.Split('.');
            float deg = float.Parse(gpsplit[0].Substring(0, gpsplit[0].Length - 2));
            float min = float.Parse(gpsplit[0].Substring(gpsplit[0].Length - 2) + '.' + gpsplit[1]);
            return deg + min / 60.0f;
        }

        private void write_string(string s)
        {
            uart.Write(Encoding.ASCII.GetBytes(s));
        }
    }
}
