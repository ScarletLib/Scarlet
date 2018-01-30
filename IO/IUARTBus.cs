namespace Scarlet.IO
{
    public interface IUARTBus
    {
        UARTRate BaudRate { get; set; }
        UARTBitCount BitLength { get; set; }
        UARTStopBits StopBits { get; set; }
        UARTParity Parity { get; set; }

        /// <summary> Writes the given data onto the UART bus. </summary>
        void Write(byte[] Data);

        /// <summary> Attempts to read <c>Length</c> bytes from the UART bus. Returns the actual number of bytes read (will be lower if there wasn't enough data available). Consider re-using <c>Buffer</c> if you're doing a lot of I/O to reduce Garbage Collection. </summary>
        int Read(int Length, byte[] Buffer);

        /// <summary> Tells you how many bytes have been received and are ready to be read. </summary>
        int BytesAvailable();

        void Dispose();

    }

    public enum UARTRate
    {
        BAUD_0, // ???
        BAUD_50,
        BAUD_75,
        BAUD_110,
        BAUD_134,
        BAUD_150,
        BAUD_200,
        BAUD_300,
        BAUD_600,
        BAUD_1200,
        BAUD_1800,
        BAUD_2400,
        BAUD_4800,
        BAUD_9600,
        BAUD_19200,
        BAUD_38400,
        BAUD_57600,
        BAUD_115200,
        BAUD_230400,
        BAUD_460800
    }

    public enum UARTBitCount
    {
        BITS_NONE, // ???
        BITS_5,
        BITS_6,
        BITS_7,
        BITS_8
    }

    public enum UARTParity
    {
        PARITY_NONE,
        PARITY_ODD,
        PARITY_EVEN
    }

    public enum UARTStopBits
    {
        STOPBITS_1,
        STOPBITS_2
    }
}
