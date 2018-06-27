using System;
using System.Runtime.InteropServices;

namespace Scarlet.IO.RaspberryPi
{
    public static class RaspberryPi
    {
        public static bool Initialized { get; private set; }

        /// <summary> Prepares the Raspberry Pi's GPIO system for use. You should do this before using any GPIO functions. </summary>
        public static void Initialize()
        {
            Initialized = true;
            External.SetupGPIO();
        }

        internal enum PinMode
        {
            INPUT = 0,
            OUTPUT = 1,
            OUTPUT_PWM = 2,
            GPIO_CLOCK = 3
        }

        /// <summary> Sets the given GPIO pin to the given mode. </summary>
        /// <param name="Pin"> The pin to configure. </param>
        /// <param name="Mode"> The functionality to select. </param>
        internal static void SetPinMode(int Pin, PinMode Mode)
        {
            if (!Initialized) { throw new InvalidOperationException("Cannot perform GPIO operations until the system is initialized. Call RasperryPi.Initialize()."); }
            External.SetPinMode(Pin, (int)Mode);
        }

        /// <summary> Configures the internal resistor for the given pin. </summary>
        /// <param name="Pin"> THe pin to configure. </param>
        /// <param name="ResMode"> The resistor to apply. </param>
        internal static void SetResistor(int Pin, ResistorState ResMode)
        {
            if (!Initialized) { throw new InvalidOperationException("Cannot perform GPIO operations until the system is initialized. Call RasperryPi.Initialize()."); }
            int ResistorVal = 0;
            if (ResMode == ResistorState.PULL_UP) { ResistorVal = 2; }
            else if (ResMode == ResistorState.PULL_DOWN) { ResistorVal = 1; }
            External.SetResistor(Pin, ResistorVal);
        }

        /// <summary> Gets the GPIO pin's current input state. </summary>
        /// <param name="Pin"> THe pin to get. </param>
        /// <returns> Whether the pin sees a logic high level on the input. </returns>
        internal static bool DigitalRead(int Pin) => External.DigitalRead(Pin) != 0;

        /// <summary> Set's the GPIO pin's output state. </summary>
        /// <param name="Pin"> The pin to set. </param>
        /// <param name="OutputVal"> Whether to output logic high or low. </param>
        internal static void DigitalWrite(int Pin, bool OutputVal) => External.DigitalWrite(Pin, OutputVal ? 1 : 0);

        /// <summary> Method template for interrupt callbacks. </summary>
        internal delegate void InterruptCallback();

        /// <summary> Adds an interrupt to the given GPIO pin. </summary>
        /// <param name="Pin"> The pin to monitor. </param>
        /// <param name="InterruptType"> The type of edge to listen for. </param>
        /// <param name="Delegate"> The delegate to call when the given type of event happens. </param>
        internal static void AddInterrupt(int Pin, int InterruptType, InterruptCallback Delegate)
        {
            if (!Initialized) { throw new InvalidOperationException("Cannot perform GPIO operations until the system is initialized. Call RasperryPi.Initialize()."); }
            External.AddInterrupt(Pin, InterruptType, Delegate);
        }

        /// <summary> Prepares an I2C device for use. </summary>
        /// <param name="DeviceID"> The I2C address of the device. </param>
        /// <returns> A handle to use for communicating with the device. </returns>
        internal static int I2CSetup(byte DeviceID)
        {
            if (!Initialized) { throw new InvalidOperationException("Cannot perform I2C operations until the system is initialized. Call RasperryPi.Initialize()."); }
            int SetupVal = External.I2CSetup(DeviceID);
            if (SetupVal == -1) { throw new Exception("Unable to setup I2C device: " + DeviceID); }
            return SetupVal;
        }

        /// <summary> Listens for data coming from the I2C device. </summary>
        /// <param name="DeviceID"> The device handle to receive data from. </param>
        /// <returns> 1 byte of data. </returns>
        internal static byte I2CRead(int DeviceID) => (byte)External.I2CRead(DeviceID);
        
        /// <summary> Sends data to the I2C device. </summary>
        /// <param name="DeviceID"> The device handle to send data to. </param>
        /// <param name="Data"> 1 byte of data to send. </param>
        internal static void I2CWrite(int DeviceID, byte Data) => External.I2CWrite(DeviceID, Data);

        /// <summary> Reads an 8-bit register. </summary>
        /// <param name="DeviceID"> The device handle to read from. </param>
        /// <param name="Register"> The register to request. </param>
        /// <returns> The data contained in the given register. </returns>
        internal static byte I2CReadRegister8(int DeviceID, byte Register) => (byte)External.I2CReadReg8(DeviceID, Register);

        /// <summary> Writes an 8-bit register. </summary>
        /// <param name="DeviceID"> The device handle to write to. </param>
        /// <param name="Register"> The register to set. </param>
        /// <param name="Data"> The data to write to the register. </param>
        internal static void I2CWriteRegister8(int DeviceID, byte Register, byte Data) => External.I2CWriteReg8(DeviceID, Register, Data);

        /// <summary> Reads a 16-bit register. </summary>
        /// <param name="DeviceID"> The device handle to write to. </param>
        /// <param name="Register"> The register to request. </param>
        /// <returns> The data contained in the given register. </returns>
        internal static ushort I2CReadRegister16(int DeviceID, byte Register) => (ushort)External.I2CReadReg16(DeviceID, Register);

        /// <summary> Writes a 16-bit register. </summary>
        /// <param name="DeviceID"> The device handle to write to. </param>
        /// <param name="Register"> The register to set. </param>
        /// <param name="Data"> The data to write to the register. </param>
        internal static void I2CWriteRegister16(int DeviceID, byte Register, ushort Data) => External.I2CWriteReg16(DeviceID, Register, Data);

        /// <summary> Prepares an SPI bus for use. </summary>
        /// <param name="BusNum"> The ID of the bus to set up. </param>
        /// <param name="Speed"> The speed to use for the bus. </param>
        internal static void SPISetup(int BusNum, int Speed)
        {
            if (!Initialized) { throw new InvalidOperationException("Cannot perform SPI operations until the system is initialized. Call RasperryPi.Initialize()."); }
            External.SPISetup(BusNum, Speed);
        }
        
        /// <summary> Does an SPI transaction. </summary>
        /// <param name="BusNum"> The bus to send/receive from. </param>
        /// <param name="Data"> The data to send out (not modified). </param>
        /// <param name="Length"> THe amount of data to send/receive. </param>
        /// <returns> The data received from the bus. </returns>
        internal static byte[] SPIRW(int BusNum, byte[] Data, int Length)
        {
            byte[] DataNew = new byte[Data.Length];
            Array.Copy(Data, DataNew, Data.Length);
            int ReturnDataLen = External.SPIRW(BusNum, DataNew, Length);
            return DataNew;
        }

        /// <summary> Attempts to prepare a UART device for use. </summary>
        /// <param name="Device"> The ID of the UART bus to set up. </param>
        /// <param name="Baud"> The baudrate to set the bus to. </param>
        /// <returns> Device handle, or -1 on error </returns>
        internal static int SerialOpen(byte Device, int Baud)
        {
            if (!Initialized) { throw new InvalidOperationException("Cannot perform UART operations until the system is initialized. Call RasperryPi.Initialize()."); }
            return External.SerialOpen(Device, Baud);
        }
        
        /// <summary> Closes resources used by the UART bus. </summary>
        /// <param name="DeviceID"> The bus handle to free resources for. </param>
        internal static void SerialClose(int DeviceID)
        {
            External.SerialClose(DeviceID);
        }
        
        /// <summary> Sends data over the UART bus. </summary>
        /// <param name="DeviceID"> The bus handle to use. </param>
        /// <param name="Data"> The data to send. </param>
        internal static void SerialPut(int DeviceID, byte[] Data) => External.SerialPut(DeviceID, Data);
        
        /// <summary> Checks how many bytes are available for read. </summary>
        /// <param name="DeviceID"> The bus handle to use. </param>
        internal static int SerialDataAvailable(int DeviceID) => External.SerialDataAvailable(DeviceID);
        
        /// <summary> Clears all data waiting to be sent/received. </summary>
        /// <param name="DeviceID"> The bus handle to use. </param>
        internal static void SerialFlush(int DeviceID) => External.SerialFlush(DeviceID);
        
        /// <summary> Gets a single byte of data from the UART bus. </summary>
        /// <param name="DeviceID"> The bus handle to use. </param>
        /// <returns> One byte of data from the bus. </returns>
        internal static byte SerialGetChar(int DeviceID) => External.SerialGetChar(DeviceID);

        /// <summary> Sends data over the UART bus. </summary>
        /// <param name="DeviceID"> The bus handle to use. </param>
        /// <param name="Data"> The data to send. </param>
        internal static void SerialPrintf(int DeviceID, byte[] Data) => External.SerialPrintf(DeviceID, Data);

        private static class External
        {
            private const string WIRING_PI_LIB = "libWiringPi-2.44.so";

            // Setup
            [DllImport(WIRING_PI_LIB, EntryPoint = "wiringPiSetupPhys")]
            internal static extern int SetupGPIO(); // Always returns 0.

            // GPIO
            [DllImport(WIRING_PI_LIB, EntryPoint = "pinMode")]
            internal static extern void SetPinMode(int Pin, int Mode);

            [DllImport(WIRING_PI_LIB, EntryPoint = "pullUpDnControl")]
            internal static extern void SetResistor(int Pin, int ResMode);

            [DllImport(WIRING_PI_LIB, EntryPoint = "digitalRead")]
            internal static extern int DigitalRead(int Pin);

            [DllImport(WIRING_PI_LIB, EntryPoint = "digitalWrite")]
            internal static extern void DigitalWrite(int Pin, int Value);

            // Interrupts
            [DllImport(WIRING_PI_LIB, EntryPoint = "wiringPiISR")]
            internal static extern int AddInterrupt(int Pin, int InterruptType, InterruptCallback Delegate);

            // I2C
            [DllImport(WIRING_PI_LIB, EntryPoint = "wiringPiI2CSetup")]
            internal static extern int I2CSetup(int DeviceID);

            [DllImport(WIRING_PI_LIB, EntryPoint = "wiringPiI2CRead")]
            internal static extern int I2CRead(int DeviceID);

            [DllImport(WIRING_PI_LIB, EntryPoint = "wiringPiI2CWrite")]
            internal static extern int I2CWrite(int DeviceID, int Data);

            [DllImport(WIRING_PI_LIB, EntryPoint = "wiringPiI2CReadReg8")]
            internal static extern int I2CReadReg8(int DeviceID, int Register);

            [DllImport(WIRING_PI_LIB, EntryPoint = "wiringPiI2CWriteReg8")]
            internal static extern int I2CWriteReg8(int DeviceID, int Register, int Data);

            [DllImport(WIRING_PI_LIB, EntryPoint = "wiringPiI2CReadReg16")]
            internal static extern int I2CReadReg16(int DeviceID, int Register);

            [DllImport(WIRING_PI_LIB, EntryPoint = "wiringPiI2CWriteReg16")]
            internal static extern int I2CWriteReg16(int DeviceID, int Register, int Data);

            // SPI
            [DllImport(WIRING_PI_LIB, EntryPoint = "wiringPiSPISetup")]
            internal static extern void SPISetup(int BusNum, int Speed);

            [DllImport(WIRING_PI_LIB, EntryPoint = "wiringPiSPIDataRW")]
            internal static extern int SPIRW(int BusNum, [In, Out] byte[] Data, int Length);

            // UART
            [DllImport(WIRING_PI_LIB, EntryPoint = "serialOpen")]
            internal static extern int SerialOpen(byte Device, int Baud);

            [DllImport(WIRING_PI_LIB, EntryPoint = "serialClose")]
            internal static extern void SerialClose(int DeviceID);

            [DllImport(WIRING_PI_LIB, EntryPoint = "serialPuts")]
            internal static extern void SerialPut(int DeviceID, [In, Out] byte[] Data);

            [DllImport(WIRING_PI_LIB, EntryPoint = "serialDataAvail")]
            internal static extern int SerialDataAvailable(int DeviceID);

            [DllImport(WIRING_PI_LIB, EntryPoint = "serialFlush")]
            internal static extern void SerialFlush(int DeviceID);

            [DllImport(WIRING_PI_LIB, EntryPoint = "serialGetchar")]
            internal static extern byte SerialGetChar(int DeviceID);

            [DllImport(WIRING_PI_LIB, EntryPoint = "serialPrintf")]
            internal static extern void SerialPrintf(int DeviceID, [In, Out] byte[] Data);
        }
    }
}
