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
        public float Latitude { get; private set; }
        public float Longitude { get; private set; }
        public string System { get; set; }

        private IUARTBus UART;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Scarlet.Components.Sensors.MTK3339"/> class. 
        /// Sets it to send GPRMC and GPGGA every 200 milliseconds. 
        /// </summary>
        /// <param name="UART"> The UART bus to read from and write to. </param>
        public MTK3339(IUARTBus UART)
        {
            this.UART = UART ?? throw new Exception("Cannot initialize MTK3339 with null UART bus!");
            if (UART.BytesAvailable() > 0) { UART.Read(UART.BytesAvailable(), new byte[UART.BytesAvailable()]); }
            Thread ParseThread = new Thread(Parse);
            ParseThread.IsBackground = true;
            ParseThread.Start();
        }

        private void Parse()
        {
            StringBuilder Builder = new StringBuilder();

            while (true)
            {
                char Data = BlockingGetChar();
                Builder.Append(Data);
                if (Data == '\n')
                {
                    if (Builder.ToString().Contains("$GPGGA"))
                    {
                        int? LatIndex = null;
                        int? LngIndex = null;
                        string[] SplitData = Builder.ToString().Split(',');
                        for (int i = 0; i < SplitData.Length; i++)
                        {
                            switch (SplitData[i])
                            {
                                case "N": LatIndex = i - 1; break;
                                case "S": LatIndex = -(i - 1); break;
                                case "W": LngIndex = -(i - 1); break;
                                case "E": LngIndex = i - 1; break;
                            }
                        }

                        if (LatIndex.HasValue && LngIndex.HasValue)
                        {
                            Latitude = RawToDeg(SplitData[Math.Abs(LatIndex.Value)]) * Math.Sign(LatIndex.Value);
                            Longitude = RawToDeg(SplitData[Math.Abs(LngIndex.Value)]) * Math.Sign(LngIndex.Value);
                        }
                    }
                    Builder.Clear();
                }
            }
        }

        private char BlockingGetChar()
        {
            byte[] NewChar = new byte[1];
            while (UART.BytesAvailable() < 1) ;
            UART.Read(1, NewChar);
            return (char)NewChar[0];
        }

        /// <summary> Checks whether this GPS has a fix. </summary>        
        /// <returns> Returns true if the GPS has a fix and false otherwise. </returns>
        public bool Test() => HasFix();

        /// <summary> Gets new readings from GPS. </summary>
        public void UpdateState() => GetCoords();

        /// <summary> Queries the GPS to see if it has a fix. </summary>
        /// <returns> <c>true</c>, if fix was hased, <c>false</c> otherwise. </returns>
        public bool HasFix()
        {
            return true;
        }

        /// <summary> Gets the GPS coordinates of this GPS. </summary>
        /// <returns> Returns a tuple with the GPS coordinates, with Latitude first and Longitude second. </returns>
        public Tuple<float, float> GetCoords()
        {
            return new Tuple<float, float>(Latitude, Longitude);
        }

        /// <summary> Converts a string number to a degree value. </summary>
        /// <returns> The degree value. </returns>
        /// <param name="Val"> The string value. </param>
        private float RawToDeg(string Val)
        {
            string[] GPSplit = Val.Split('.');
            //Console.WriteLine(GPSplit[0]);
            float Deg = float.Parse(GPSplit[0].Substring(0, GPSplit[0].Length - 2));
            float Min = float.Parse(GPSplit[0].Substring(GPSplit[0].Length - 2) + '.' + GPSplit[1]);
            return Deg + Min / 60.0f;
        }

        public DataUnit GetData()
        {
            return new DataUnit("MTK3339")
            {
                { "HasFix", HasFix() }, // TODO: This needs to be stored from last reading instead of re-checked.
                { "Lat", this.Latitude},
                { "Lon", this.Longitude}
            }
            .SetSystem(this.System);
        }
    }
}