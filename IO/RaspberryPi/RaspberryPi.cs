﻿using Scarlet.Utilities;
using System;
using System.Runtime.InteropServices;

namespace Scarlet.IO.RaspberryPi
{
    public static class RaspberryPi
    {
        private const string WIRING_PI_LIB = "libWiringPi-2.44.so";

        private static bool P_Initialized = false;
        internal static bool Initialized
        {
            get { return P_Initialized; }
            private set { P_Initialized = value; }
        }

        /// <returns>Always 0, You can ignore this.</returns>
        [DllImport(WIRING_PI_LIB, EntryPoint = "wiringPiSetupPhys")]
        private static extern int Ext_SetupGPIO();

        /// <summary> Prepares the Raspberry Pi's GPIO system for use. You should do this before using any GPIO functions. </summary>
        public static void Initialize()
        {
            Initialized = true;
            Ext_SetupGPIO();
        }

        internal enum PinMode
        {
            INPUT = 0,
            OUTPUT = 1,
            OUTPUT_PWM = 2,
            GPIO_CLOCK = 3
        }

        #region GPIO

        [DllImport(WIRING_PI_LIB, EntryPoint = "pinMode")]
        private static extern void Ext_SetPinMode(int Pin, int Mode);

        internal static void SetPinMode(int Pin, PinMode Mode)
        {
            if (!Initialized) { throw new InvalidOperationException("Cannot perform GPIO operations until the system is initialized. Call RasperryPi.Initialize()."); }
            Ext_SetPinMode(Pin, (int)Mode);
        }

        [DllImport(WIRING_PI_LIB, EntryPoint = "pullUpDnControl")]
        private static extern void Ext_SetResistor(int Pin, int ResMode);

        /// <summary> Sets or disconnects the pull-up/-down resistor. </summary>
        internal static void SetResistor(int Pin, ResistorState ResMode)
        {
            if (!Initialized) { throw new InvalidOperationException("Cannot perform GPIO operations until the system is initialized. Call RasperryPi.Initialize()."); }
            int ResistorVal = 0;
            if (ResMode == ResistorState.PULL_UP) { ResistorVal = 2; }
            else if (ResMode == ResistorState.PULL_DOWN) { ResistorVal = 1; }
            Ext_SetResistor(Pin, ResistorVal);
        }

        [DllImport(WIRING_PI_LIB, EntryPoint = "digitalRead")]
        private static extern int Ext_DigitalRead(int Pin);

        internal static bool DigitalRead(int Pin)
        {
            if (!Initialized) { throw new InvalidOperationException("Cannot perform GPIO operations until the system is initialized. Call RasperryPi.Initialize()."); }
            return Ext_DigitalRead(Pin) != 0;
        }


        [DllImport(WIRING_PI_LIB, EntryPoint = "digitalWrite")]
        private static extern void Ext_DigitalWrite(int Pin, int Value);

        internal static void DigitalWrite(int Pin, bool OutputVal)
        {
            if (!Initialized) { throw new InvalidOperationException("Cannot perform GPIO operations until the system is initialized. Call RasperryPi.Initialize()."); }
            Ext_DigitalWrite(Pin, OutputVal ? 1 : 0);
        }

        #endregion

        #region Interrupts

        internal delegate void InterruptCallback();

        [DllImport(WIRING_PI_LIB, EntryPoint = "wiringPiISR")]
        private static extern int Ext_AddInterrupt(int Pin, int InterruptType, InterruptCallback Delegate);

        internal static void AddInterrupt(int Pin, int InterruptType, InterruptCallback Delegate)
        {
            if (!Initialized) { throw new InvalidOperationException("Cannot perform GPIO operations until the system is initialized. Call RasperryPi.Initialize()."); }
            Ext_AddInterrupt(Pin, InterruptType, Delegate);
        }

        #endregion

        #region I2C

        [DllImport(WIRING_PI_LIB, EntryPoint = "wiringPiI2CSetup")]
        private static extern int Ext_I2CSetup(int DeviceID);

        internal static int I2CSetup(byte DeviceID)
        {
            int SetupVal = Ext_I2CSetup(DeviceID);
            if (SetupVal == -1) { throw new Exception("Unable to setup I2C device: " + DeviceID); }
            return SetupVal;
        }

        [DllImport(WIRING_PI_LIB, EntryPoint = "wiringPiI2CRead")]
        private static extern int Ext_I2CRead(int DeviceID);

        internal static byte I2CRead(int DeviceID)
        {
            int Data = Ext_I2CRead(DeviceID);
            return (byte)Data;
        }

        [DllImport(WIRING_PI_LIB, EntryPoint = "wiringPiI2CWrite")]
        private static extern int Ext_I2CWrite(int DeviceID, int Data);

        internal static void I2CWrite(int DeviceID, byte Data)
        {
            Ext_I2CWrite(DeviceID, Data);
        }

        [DllImport(WIRING_PI_LIB, EntryPoint = "wiringPiI2CReadReg8")]
        private static extern int Ext_I2CReadReg8(int DeviceID, int Register);
        internal static byte I2CReadRegister8(int DeviceID, byte Register) => (byte)Ext_I2CReadReg8(DeviceID, Register);

        [DllImport(WIRING_PI_LIB, EntryPoint = "wiringPiI2CWriteReg8")]
        private static extern int Ext_I2CWriteReg8(int DeviceID, int Register, int Data);
        internal static void I2CWriteRegister8(int DeviceID, byte Register, byte Data) => Ext_I2CWriteReg8(DeviceID, Register, Data);


        [DllImport(WIRING_PI_LIB, EntryPoint = "wiringPiI2CReadReg16")]
        private static extern int Ext_I2CReadReg16(int DeviceID, int Register);
        internal static ushort I2CReadRegister16(int DeviceID, byte Register) => (ushort)Ext_I2CReadReg16(DeviceID, Register);

        [DllImport(WIRING_PI_LIB, EntryPoint = "wiringPiI2CWriteReg16")]
        private static extern int Ext_I2CWriteReg16(int DeviceID, int Register, int Data);
        internal static void I2CWriteRegister16(int DeviceID, byte Register, ushort Data) => Ext_I2CWriteReg16(DeviceID, Register, Data);

        #endregion

        #region SPI

        [DllImport(WIRING_PI_LIB, EntryPoint = "wiringPiSPISetup")]
        private static extern void Ext_SPISetup(int BusNum, int Speed);

        internal static void SPISetup(int BusNum, int Speed) { Ext_SPISetup(BusNum, Speed); }

        [DllImport(WIRING_PI_LIB, EntryPoint = "wiringPiSPIDataRW")]
        private static extern int Ext_SPIRW(int BusNum, [In,Out] byte[] Data, int Length);

        internal static byte[] SPIRW(int BusNum, byte[] Data, int Length)
        {
            byte[] DataNew = Data; // Unsure if Data will be changed if sent through WiringPI, but this should clone
            int ReturnDataLen = Ext_SPIRW(BusNum, DataNew, Length);
            return DataNew;
        }

        #endregion

        #region UART

        [DllImport(WIRING_PI_LIB, EntryPoint = "serialOpen")]
        private static extern int Ext_SerialOpen(byte Device, int Baud);

        /// <summary> Attempts to prepare a UART device for use. </summary>
        /// <returns> Device ID, or -1 on error </returns>
        internal static int SerialOpen(byte Device, int Baud) { return Ext_SerialOpen(Device, Baud); }

        [DllImport(WIRING_PI_LIB, EntryPoint = "serialClose")]
        private static extern void Ext_SerialClose(int DeviceID);

        internal static void SerialClose(int DeviceID) { Ext_SerialClose(DeviceID); }

        [DllImport(WIRING_PI_LIB, EntryPoint = "serialPuts")]
        private static extern void Ext_SerialPut(int DeviceID, [In,Out] byte[] Data);

        internal static void SerialPut(int DeviceID, byte[] Data) { Ext_SerialPut(DeviceID, Data); }

        [DllImport(WIRING_PI_LIB, EntryPoint = "serialDataAvail")]
        private static extern int Ext_SerialDataAvailable(int DeviceID);

        /// <summary> Checks how many bytes are available for read. </summary>
        internal static int SerialDataAvailable(int DeviceID) { return Ext_SerialDataAvailable(DeviceID); }

        [DllImport(WIRING_PI_LIB, EntryPoint = "serialFlush")]
        private static extern void Ext_SerialFlush(int DeviceID);

        /// <summary> Clears all data waiting to be sent. </summary>
        internal static void SerialFlush(int DeviceID) { Ext_SerialFlush(DeviceID); }

        [DllImport(WIRING_PI_LIB, EntryPoint = "serialGetchar")]
        private static extern byte Ext_SerialGetChar(int DeviceID);

        internal static byte SerialGetChar(int DeviceID) { return Ext_SerialGetChar(DeviceID); }

        [DllImport(WIRING_PI_LIB, EntryPoint = "serialPrintf")]
        private static extern void Ext_SerialPrintf(int DeviceID, [In,Out] byte[] Data);

        internal static void SerialPrintf(int DeviceID, byte[] Data) { Ext_SerialPrintf(DeviceID, Data); }

        #endregion

    }
}
