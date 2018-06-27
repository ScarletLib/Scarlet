using System;
using BBBCSIO;

namespace Scarlet.IO.BeagleBone
{
    public static class UARTBBB
    {
        public static UARTBusBBB UARTBus1 { get; private set; }
        public static UARTBusBBB UARTBus2 { get; private set; }

        /// <summary> Note that this bus can only transmit. </summary>
        public static UARTBusBBB UARTBus3 { get; private set; }

        public static UARTBusBBB UARTBus4 { get; private set; }

        /// <summary> Prepares the given UART buses for use. Should only be called from <see cref="BeagleBone.Initialize(SystemMode, bool)"/>. </summary>
        /// <param name="EnableBuses"> Whether to enable each of the UART buses. </param>
        internal static void Initialize(bool[] EnableBuses)
        {
            if (EnableBuses == null || EnableBuses.Length != 4) { throw new Exception("Invalid enable array given to UARTBBB.Initialize."); }
            if (EnableBuses[0]) { UARTBus1 = new UARTBusBBB(BBBPin.P9_24, BBBPin.P9_26); }
            if (EnableBuses[1]) { UARTBus2 = new UARTBusBBB(BBBPin.P9_21, BBBPin.P9_22); }
            if (EnableBuses[2]) { UARTBus3 = new UARTBusBBB(BBBPin.P9_42, BBBPin.NONE); }
            if (EnableBuses[3]) { UARTBus4 = new UARTBusBBB(BBBPin.P9_13, BBBPin.P9_11); }
        }

        /// <summary> Converts a pin number to the corresponding UART bus ID.</summary>
        /// <param name="Pin"> The pin to translate. </param>
        /// <returns> The corresponding UART bus ID, or 255 if the input is invalid.</returns>
        internal static byte PinToUARTBus(BBBPin Pin)
        {
            switch (Pin)
            {
                case BBBPin.P9_24:
                case BBBPin.P9_26: return 1;

                case BBBPin.P9_21:
                case BBBPin.P9_22: return 2;

                case BBBPin.P9_42: return 3;

                case BBBPin.P9_13:
                case BBBPin.P9_11: return 4;
            }
            return 255;
        }

        internal static bool PinIsTX(BBBPin Pin)
        {
            switch (Pin)
            {
                case BBBPin.P9_24:
                case BBBPin.P9_21:
                case BBBPin.P9_42:
                case BBBPin.P9_13: return true;
            }
            return false;
        }
    }

    public class UARTBusBBB : IUARTBus
    {
        private SerialPortFS Port;

        public UARTRate BaudRate
        {
            get
            {
                switch (this.Port.BaudRate)
                {
                    case SerialPortBaudRateEnum.BAUDRATE_0: return UARTRate.BAUD_0;
                    case SerialPortBaudRateEnum.BAUDRATE_50: return UARTRate.BAUD_50;
                    case SerialPortBaudRateEnum.BAUDRATE_75: return UARTRate.BAUD_75;
                    case SerialPortBaudRateEnum.BAUDRATE_110: return UARTRate.BAUD_110;
                    case SerialPortBaudRateEnum.BAUDRATE_134: return UARTRate.BAUD_134;
                    case SerialPortBaudRateEnum.BAUDRATE_150: return UARTRate.BAUD_150;
                    case SerialPortBaudRateEnum.BAUDRATE_200: return UARTRate.BAUD_200;
                    case SerialPortBaudRateEnum.BAUDRATE_300: return UARTRate.BAUD_300;
                    case SerialPortBaudRateEnum.BAUDRATE_600: return UARTRate.BAUD_600;
                    case SerialPortBaudRateEnum.BAUDRATE_1200: return UARTRate.BAUD_1200;
                    case SerialPortBaudRateEnum.BAUDRATE_1800: return UARTRate.BAUD_1800;
                    case SerialPortBaudRateEnum.BAUDRATE_2400: return UARTRate.BAUD_2400;
                    case SerialPortBaudRateEnum.BAUDRATE_4800: return UARTRate.BAUD_4800;
                    case SerialPortBaudRateEnum.BAUDRATE_9600: return UARTRate.BAUD_9600;
                    case SerialPortBaudRateEnum.BAUDRATE_19200: return UARTRate.BAUD_19200;
                    case SerialPortBaudRateEnum.BAUDRATE_38400: return UARTRate.BAUD_38400;
                    case SerialPortBaudRateEnum.BAUDRATE_57600: return UARTRate.BAUD_57600;
                    case SerialPortBaudRateEnum.BAUDRATE_115200: return UARTRate.BAUD_115200;
                    case SerialPortBaudRateEnum.BAUDRATE_230400: return UARTRate.BAUD_230400;
                    case SerialPortBaudRateEnum.BAUDRATE_460800: return UARTRate.BAUD_460800;
                }
                return UARTRate.BAUD_0;
            }
            set
            {
                switch (value)
                {
                    case UARTRate.BAUD_0: this.Port.BaudRate = SerialPortBaudRateEnum.BAUDRATE_0; break;
                    case UARTRate.BAUD_50: this.Port.BaudRate = SerialPortBaudRateEnum.BAUDRATE_50; break;
                    case UARTRate.BAUD_75: this.Port.BaudRate = SerialPortBaudRateEnum.BAUDRATE_75; break;
                    case UARTRate.BAUD_110: this.Port.BaudRate = SerialPortBaudRateEnum.BAUDRATE_110; break;
                    case UARTRate.BAUD_134: this.Port.BaudRate = SerialPortBaudRateEnum.BAUDRATE_134; break;
                    case UARTRate.BAUD_150: this.Port.BaudRate = SerialPortBaudRateEnum.BAUDRATE_150; break;
                    case UARTRate.BAUD_200: this.Port.BaudRate = SerialPortBaudRateEnum.BAUDRATE_200; break;
                    case UARTRate.BAUD_300: this.Port.BaudRate = SerialPortBaudRateEnum.BAUDRATE_300; break;
                    case UARTRate.BAUD_600: this.Port.BaudRate = SerialPortBaudRateEnum.BAUDRATE_600; break;
                    case UARTRate.BAUD_1200: this.Port.BaudRate = SerialPortBaudRateEnum.BAUDRATE_1200; break;
                    case UARTRate.BAUD_1800: this.Port.BaudRate = SerialPortBaudRateEnum.BAUDRATE_1800; break;
                    case UARTRate.BAUD_2400: this.Port.BaudRate = SerialPortBaudRateEnum.BAUDRATE_2400; break;
                    case UARTRate.BAUD_4800: this.Port.BaudRate = SerialPortBaudRateEnum.BAUDRATE_4800; break;
                    case UARTRate.BAUD_9600: this.Port.BaudRate = SerialPortBaudRateEnum.BAUDRATE_9600; break;
                    case UARTRate.BAUD_19200: this.Port.BaudRate = SerialPortBaudRateEnum.BAUDRATE_19200; break;
                    case UARTRate.BAUD_38400: this.Port.BaudRate = SerialPortBaudRateEnum.BAUDRATE_38400; break;
                    case UARTRate.BAUD_57600: this.Port.BaudRate = SerialPortBaudRateEnum.BAUDRATE_57600; break;
                    case UARTRate.BAUD_115200: this.Port.BaudRate = SerialPortBaudRateEnum.BAUDRATE_115200; break;
                    case UARTRate.BAUD_230400: this.Port.BaudRate = SerialPortBaudRateEnum.BAUDRATE_230400; break;
                    case UARTRate.BAUD_460800: this.Port.BaudRate = SerialPortBaudRateEnum.BAUDRATE_460800; break;
                    default: this.Port.BaudRate = SerialPortBaudRateEnum.BAUDRATE_0; break;
                }
            }
        }

        public UARTBitCount BitLength
        {
            get
            {
                switch (this.Port.BitLength)
                {
                    case SerialPortBitLengthEnum.BITLENGTH_5: return UARTBitCount.BITS_5;
                    case SerialPortBitLengthEnum.BITLENGTH_6: return UARTBitCount.BITS_6;
                    case SerialPortBitLengthEnum.BITLENGTH_7: return UARTBitCount.BITS_7;
                    case SerialPortBitLengthEnum.BITLENGTH_8: return UARTBitCount.BITS_8;
                }
                return UARTBitCount.BITS_NONE;
            }
            set
            {
                switch (value)
                {
                    case UARTBitCount.BITS_5: this.Port.BitLength = SerialPortBitLengthEnum.BITLENGTH_5; break;
                    case UARTBitCount.BITS_6: this.Port.BitLength = SerialPortBitLengthEnum.BITLENGTH_6; break;
                    case UARTBitCount.BITS_7: this.Port.BitLength = SerialPortBitLengthEnum.BITLENGTH_7; break;
                    case UARTBitCount.BITS_8: this.Port.BitLength = SerialPortBitLengthEnum.BITLENGTH_8; break;
                    default: this.Port.BitLength = SerialPortBitLengthEnum.BITLENGTH_NONE; break;
                }
            }
        }

        public UARTParity Parity
        {
            get
            {
                switch (this.Port.Parity)
                {
                    case SerialPortParityEnum.PARITY_EVEN: return UARTParity.PARITY_EVEN;
                    case SerialPortParityEnum.PARITY_ODD: return UARTParity.PARITY_ODD;
                }
                return UARTParity.PARITY_NONE;
            }
            set
            {
                switch (value)
                {
                    case UARTParity.PARITY_EVEN: this.Port.Parity = SerialPortParityEnum.PARITY_EVEN; break;
                    case UARTParity.PARITY_ODD: this.Port.Parity = SerialPortParityEnum.PARITY_ODD; break;
                    default: this.Port.Parity = SerialPortParityEnum.PARITY_NONE; break;
                }
            }
        }

        public UARTStopBits StopBits
        {
            get => ((this.Port.StopBits == SerialPortStopBitsEnum.STOPBITS_ONE) ? UARTStopBits.STOPBITS_1 : UARTStopBits.STOPBITS_2);
            set => this.Port.StopBits = ((value == UARTStopBits.STOPBITS_1) ? SerialPortStopBitsEnum.STOPBITS_ONE : SerialPortStopBitsEnum.STOPBITS_TWO);
        }

        internal UARTBusBBB(BBBPin TX, BBBPin RX)
        {
            SerialPortEnum PortNum = SerialPortEnum.UART_NONE;
            switch (TX)
            {
                case BBBPin.P9_24:
                case BBBPin.P9_26: PortNum = SerialPortEnum.UART_1; break;

                case BBBPin.P9_21:
                case BBBPin.P9_22: PortNum = SerialPortEnum.UART_2; break;

                case BBBPin.P9_42: PortNum = SerialPortEnum.UART_NONE; break; // TODO: See if UART3 is even usable.

                case BBBPin.P9_13:
                case BBBPin.P9_11: PortNum = SerialPortEnum.UART_4; break;
                
                // TODO: Implement UART5?
            }
            this.Port = new SerialPortFS(PortNum, SerialPortOpenModeEnum.OPEN_NONBLOCK);
            if (!this.Port.PortIsOpen) { throw new Exception("Could not open UART port."); }
        }

        public int BytesAvailable() => this.Port.BytesInRxBuffer;

        public int Read(int Length, byte[] Buffer) { return this.Port.ReadByteArray(Buffer, Length); }

        public void Write(byte[] Data) { this.Port.Write(Data, Data.Length); }

        public void Flush() { this.Port.Flush(); }

        public void Dispose() { this.Port.Dispose(); }
    }
}
