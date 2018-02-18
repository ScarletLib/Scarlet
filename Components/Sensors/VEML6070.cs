﻿using Scarlet.IO;
using System;
using Scarlet.Utilities;

namespace Scarlet.Components.Sensors
{
    /// <summary>
    /// VEML6070 UV-A Light Sensor with I2C Interface
    /// Datasheet: http://www.vishay.com/docs/84277/veml6070.pdf
    /// </summary>
    public class VEML6070 : ISensor
    {
        // LSB address is also used for write functions.
        private byte AddressLSB, AddressMSB;
        private II2CBus Bus;
        private ushort LastReading;
        private byte Speed = (byte)RefreshSpeed.REGULAR;
        public string System { get; set; }

        /// <summary> Determines how often readins are taken. DOUBLE is fastest, QUARTER is slowest. </summary>
        /// <remarks>
        /// The actual time depends on R_SET. See here for times:
        /// (Note: The Adafruit breakout board uses 300k)
        /// 
        /// SETTING     TIME@300k   TIME@600k
        /// =================================
        /// DOUBLE      62.5ms      125ms
        /// REGULAR     125ms       250ms
        /// HALF        250ms       500ms
        /// QUARTER     500ms       1000ms
        /// 
        /// (From page 8 in datasheet)
        /// </remarks>
        public enum RefreshSpeed { DOUBLE, REGULAR, HALF, QUARTER }

        public VEML6070(II2CBus Bus, byte Address = 0x38)
        {
            this.AddressLSB = Address;
            this.AddressMSB = (byte)(Address + 1);
            this.Bus = Bus;
            SetRefreshSpeed((RefreshSpeed)this.Speed);
        }

        /// <summary> Instructs the sensor to use the gven refresh speed starting now. </summary>
        public void SetRefreshSpeed(RefreshSpeed Speed)
        {
            this.Speed = (byte)Speed;
            this.Bus.Write(this.AddressLSB, new byte[] { (byte)(((this.Speed & 0b0000_0011) << 2) | 0b0000_0010) });
        }

        /// <summary> Gets the current UV light level as of the last UpdateState() call. </summary>
        /// <returns> UV light level in μW/cm/cm, in increments of 5. </returns>
        public int GetReading() { return ConvertFromRaw(this.LastReading); }

        /// <summary> Gets the sensor's raw data as it was received at the last UpdateState() call. </summary>
        /// <returns> The bytes sent over I2C from the sensor at the last reading. </returns>
        public ushort GetRawData() { return this.LastReading; }

        /// <summary> Gets the UV reading at the last UpdateState() call. </summary>
        /// <returns> UV light level in μW/cm/cm, in increments of 5. </returns>
        public static int ConvertFromRaw(ushort RawData) { return RawData * 5; }

        public bool Test() { return true; } // TODO: See if there is a way to test.

        /// <summary> Gets a new reading from the sensor and stores it. </summary>
        public void UpdateState()
        {
            ushort Data = 0x00_00;
            Data = this.Bus.Read(this.AddressLSB, 1)[0];
            Data |= (ushort)(this.Bus.Read(this.AddressMSB, 1)[0] << 8);
            this.LastReading = Data;
        }

        /// <summary> This sensor does not process events. Will do nothing. </summary>
        public void EventTriggered(object Sender, EventArgs Event) { }

        public DataUnit GetData()
        {
            return new DataUnit("VEML6070")
            {
                { "UV", GetReading() }
            }
            .SetSystem(this.System);
        }
    }
}
