using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;

namespace Scarlet.IO.BeagleBone
{
    public class UARTFileBusBBB : IUARTBus
    {
        private SerialPort Port;

        public UARTRate BaudRate
        {
            get
            {
                switch (this.Port.BaudRate)
                {
                    case 0: return UARTRate.BAUD_0;
                    case 50: return UARTRate.BAUD_50;
                    case 75: return UARTRate.BAUD_75;
                    case 110: return UARTRate.BAUD_110;
                    case 134: return UARTRate.BAUD_134;
                    case 150: return UARTRate.BAUD_150;
                    case 200: return UARTRate.BAUD_200;
                    case 300: return UARTRate.BAUD_300;
                    case 600: return UARTRate.BAUD_600;
                    case 1200: return UARTRate.BAUD_1200;
                    case 1800: return UARTRate.BAUD_1800;
                    case 2400: return UARTRate.BAUD_2400;
                    case 4800: return UARTRate.BAUD_4800;
                    case 9600: return UARTRate.BAUD_9600;
                    case 19200: return UARTRate.BAUD_19200;
                    case 38400: return UARTRate.BAUD_38400;
                    case 57600: return UARTRate.BAUD_57600;
                    case 115200: return UARTRate.BAUD_115200;
                    case 230400: return UARTRate.BAUD_230400;
                    case 460800: return UARTRate.BAUD_460800;
                }
                return UARTRate.BAUD_0;
            }
            set
            {
                switch (value)
                {
                    case UARTRate.BAUD_0: this.Port.BaudRate = 0; break;
                    case UARTRate.BAUD_50: this.Port.BaudRate = 50; break;
                    case UARTRate.BAUD_75: this.Port.BaudRate = 75; break;
                    case UARTRate.BAUD_110: this.Port.BaudRate = 110; break;
                    case UARTRate.BAUD_134: this.Port.BaudRate = 134; break;
                    case UARTRate.BAUD_150: this.Port.BaudRate = 150; break;
                    case UARTRate.BAUD_200: this.Port.BaudRate = 200; break;
                    case UARTRate.BAUD_300: this.Port.BaudRate = 300; break;
                    case UARTRate.BAUD_600: this.Port.BaudRate = 600; break;
                    case UARTRate.BAUD_1200: this.Port.BaudRate = 1200; break;
                    case UARTRate.BAUD_1800: this.Port.BaudRate = 1800; break;
                    case UARTRate.BAUD_2400: this.Port.BaudRate = 2400; break;
                    case UARTRate.BAUD_4800: this.Port.BaudRate = 4800; break;
                    case UARTRate.BAUD_9600: this.Port.BaudRate = 9600; break;
                    case UARTRate.BAUD_19200: this.Port.BaudRate = 19200; break;
                    case UARTRate.BAUD_38400: this.Port.BaudRate = 38400; break;
                    case UARTRate.BAUD_57600: this.Port.BaudRate = 57600; break;
                    case UARTRate.BAUD_115200: this.Port.BaudRate = 115200; break;
                    case UARTRate.BAUD_230400: this.Port.BaudRate = 230400; break;
                    case UARTRate.BAUD_460800: this.Port.BaudRate = 460800; break;
                    default: this.Port.BaudRate = 0; break;
                }
            }
        }
        public UARTBitCount BitLength
        {
            get
            {
                switch (this.Port.DataBits)
                {
                    case 5: return UARTBitCount.BITS_5;
                    case 6: return UARTBitCount.BITS_6;
                    case 7: return UARTBitCount.BITS_7;
                    case 8: return UARTBitCount.BITS_8;
                }
                return UARTBitCount.BITS_NONE;
            }
            set
            {
                switch (value)
                {
                    case UARTBitCount.BITS_5: this.Port.DataBits = 5; break;
                    case UARTBitCount.BITS_6: this.Port.DataBits = 6; break;
                    case UARTBitCount.BITS_7: this.Port.DataBits = 7; break;
                    case UARTBitCount.BITS_8: this.Port.DataBits = 8; break;
                    default: this.Port.DataBits = 0; break;
                }
            }
        }
        public UARTParity Parity
        {
            get
            {
                switch (this.Port.Parity)
                {
                    case System.IO.Ports.Parity.Even: return UARTParity.PARITY_EVEN;
                    case System.IO.Ports.Parity.Odd: return UARTParity.PARITY_ODD;
                }
                return UARTParity.PARITY_NONE;
            }
            set
            {
                switch (value)
                {
                    case UARTParity.PARITY_EVEN: this.Port.Parity = System.IO.Ports.Parity.Even; break;
                    case UARTParity.PARITY_ODD: this.Port.Parity = System.IO.Ports.Parity.Odd; break;
                    default: this.Port.Parity = System.IO.Ports.Parity.None; break;
                }
            }
        }
        public UARTStopBits StopBits
        {
            get => ((this.Port.StopBits == System.IO.Ports.StopBits.One) ? UARTStopBits.STOPBITS_1 : UARTStopBits.STOPBITS_2);
            set => this.Port.StopBits = ((value == UARTStopBits.STOPBITS_1) ? System.IO.Ports.StopBits.One : System.IO.Ports.StopBits.Two);
        }
        
        /// <summary>
        /// Communication with serial devices that are represented in the Unix filesystem.
        /// These are devices connected over USB as opposed to directly on the Beaglebone UART pins.
        /// </summary>
        /// <param name="DevicePath"> Unix path to file representing the serial connection to device (e.g.: /dev/ttyACM0) </param>
        public UARTFileBusBBB(string DevicePath)
        {
            this.Port = new SerialPort(DevicePath);

            try {
                this.Port.Open();
            }
            catch (Exception)
            {
                throw new Exception("Could not open UART port.");
            }
        }

        /// <summary> Writes the given data onto the UART bus. </summary>
        public void Write(byte[] Data) { this.Port.Write(Data, 0, Data.Length); }

        /// <summary> Attempts to read <c>Length</c> bytes from the UART bus. Returns the actual number of bytes read (will be lower if there wasn't enough data available). Consider re-using <c>Buffer</c> if you're doing a lot of I/O to reduce Garbage Collection. </summary>
        public int Read(int Length, byte[] Buffer) { return this.Port.Read(Buffer, 0, Length); }

        /// <summary> Tells you how many bytes have been received and are ready to be read. </summary>
        public int BytesAvailable() => this.Port.BytesToRead;

        /// <summary> Clears input and output buffers. </summary>
        public void Flush() { this.Port.DiscardInBuffer(); }

        /// <summary> Closes the UART stream </summary>
        public void Dispose() { Port.Dispose(); }
    }
}
