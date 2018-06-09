using Scarlet.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Scarlet.Components.Inputs
{
    public class TLV2544
    {
        public class AnalogueInTLV254x : IAnalogueIn
        {
            private TLV2544 Parent;
            private byte Channel;

            public void Dispose() { }

            public double GetInput()
            {
                return 0;
            }

            public double GetRange()
            {
                return 0;
            }

            public long GetRawInput()
            {
                return 0;
            }

            public long GetRawRange()
            {
                return 0;
            }
        }

        public static readonly Configuration DefaultConfig;

        private readonly ISPIBus Bus;
        private readonly IDigitalOut CS;
        private Configuration Config = DefaultConfig;

        public TLV2544(ISPIBus SPIBus, IDigitalOut ChipSelect)
        {
            this.Bus = SPIBus;
            this.CS = ChipSelect;
        }

        /// <summary> Applies the specified configuration, and prepares the device for use. </summary>
        /// <param name="Config"> The configuration to apply. </param>
        public void Configure(Configuration Config)
        {
            this.Config = Config;
        }

        /// <summary> Applies the default configuration, and prepares the device for use. </summary>
        public void Configure() => Configure(DefaultConfig);

        /// <summary> Puts the ADC to sleep, minimizing power usage. TUrn back on with <c>PowerUp</c>. </summary>
        public void PowerDown() => DoCommand(Command.POWER_DOWN, 0x000);

        /// <summary> Brings the ADC out of sleep mode. Only needed after <c>PowerDown</c> was called. Takes 20ms. </summary>
        public void PowerUp()
        {
            DoCommand(Command.WRITE_CONF, 0x000); // Power-up requirement
            Configure(this.Config);
            Thread.Sleep(20);
        }

        /// <summary> Does a 12b read/write with the specified command. </summary>
        /// <param name="Command"> The command (4 MSb) to send. </param>
        /// <param name="Data"> The data (12 LSb) to send. </param>
        /// <returns> The 12b data returned by the device. </returns>
        private ushort DoCommand(Command Command, ushort Data = 0x000)
        {
            byte[] DataOut = new byte[] { (byte)((((byte)Command << 4) & 0b1111_0000) | ((Data >> 8) & 0b0000_1111)), (byte)(Data & 0b1111_1111) };
            byte[] DataIn = this.Bus.Write(this.CS, DataOut, DataOut.Length);
            return (ushort)(Command == Command.READ_CONF ?
                (((DataIn[0] & 0b0000_1111) << 8) | (DataIn[1])) :
                (DataIn[0] << 4) | ((DataIn[1] & 0b1111_0000) >> 4));
        }

        public struct Configuration
        {
            VoltageReference VoltageRef;
            bool UseLongSample;
            ConversionClockSrc ConversionClockSrc;
            ConversionMode ConversionMode;

            /// <summary> If true, pin 4 outputs "End of COnversion" signal. Otherwise, outputs "~Interrupt" signal. </summary>
            /// End of Conversion: "This output goes from a high-to-low logic level at the end of the sampling period and remains low until the conversion is complete and data are ready for transfer. EOC is used in conversion mode 00 only."
            /// ~Interrupt: "This pin can also be programmed as an interrupt output signal to the host processor. The falling edge of ~INT indicates data are ready for output. The following ~CS↓ or ~FS clears ~INT."
            bool UseEOCPin;

            FIFOTrigger FIFOTriggerLevel;
        }

        public enum VoltageReference { INTERNAL_4V, INTERNAL_2V, EXTERNAL }

        public enum ConversionClockSrc : byte
        {
            INTERNAL = 0b00,
            SCLK = 0b01,
            SCLK_HALF = 0b11, // Yes, these are supposed to be out of order.
            SCLK_QUARTER = 0b10
        }

        public enum ConversionMode
        {
            SINGLE_SHOT,
            REPEAT,
            SWEEP_MODE0,
            SWEEP_MODE1,
            SWEEP_MODE2,
            SWEEP_MODE3,
            REPEAT_SWEEP_MODE0,
            REPEAT_SWEEP_MODE1,
            REPEAT_SWEEP_MODE2,
            REPEAT_SWEEP_MODE3
        }

        public enum FIFOTrigger : byte
        {
            FIFO_8b = 0b00,
            FIFO_6b = 0b01,
            FIFO_4b = 0b10,
            FIFO_2b = 0b11
        }

        private enum Command : byte
        {
            SEL_CH0 = 0x0,
            SEL_CH1 = 0x2,
            SEL_CH2 = 0x4,
            SEL_CH3 = 0x6,
            POWER_DOWN = 0x8,
            READ_CONF = 0x9,
            WRITE_CONF = 0xA,
            SEL_TEST1 = 0xB,
            SEL_TEST2 = 0xC,
            SEL_TEST3 = 0xD,
            FIFO_READ = 0xE
        }
    }
}
