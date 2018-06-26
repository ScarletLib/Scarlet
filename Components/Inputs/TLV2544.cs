using System;
using System.Threading;
using Scarlet.IO;
using Scarlet.Utilities;

namespace Scarlet.Components.Inputs
{
    /// <summary>
    /// 200kSa/s 4-ch Analogue to Digital Converter
    /// Datasheet: http://www.ti.com/lit/ds/symlink/tlv2544.pdf
    /// </summary>
    public class TLV2544
    {
        public class AnalogueInTLV254x : IAnalogueIn
        {
            private readonly TLV2544 Parent;
            private readonly byte Channel;

            public AnalogueInTLV254x(TLV2544 Parent, byte Channel)
            {
                this.Parent = Parent;
                this.Channel = Channel;
            }

            /// <summary> Gets the current ADC input level. </summary>
            /// <returns> The current input in Volts. </returns>
            public double GetInput() => ((double)(this.Parent.GetRawInput(this.Channel)) / GetRawRange()) * GetRange();

            /// <summary> Gets the maximum input value. Depends on voltage reference used. </summary>
            /// <returns> The maximum input voltage, in Volts. </returns>
            public double GetRange() => this.Parent.GetRange();

            /// <summary> Gets the raw digital input value, without voltage reference scaling. </summary>
            /// <returns> The raw ADC input, in Volt-bits. Between 0 and <c>GetRawRange()</c>. </returns>
            public long GetRawInput() => this.Parent.GetRawInput(this.Channel);

            /// <summary> The maximum digital value that the ADC can output. THis is fixed per chip type. </summary>
            /// <returns> The maximum an unsigned 12bit number can hold, 4095, as this is a 12b ADC. </returns>
            public long GetRawRange() => 4095;

            public void Dispose() { }
        }

        public static readonly Configuration DefaultConfig = new Configuration()
        {
            VoltageRef = VoltageReference.INTERNAL_4V,
            UseLongSample = false,
            ConversionClockSrc = ConversionClockSrc.SCLK,
            ConversionMode = ConversionMode.SINGLE_SHOT,
            UseEOCPin = false,
            FIFOTriggerLevel = FIFOTrigger.FIFO_8b
        };
        public readonly AnalogueInTLV254x[] Inputs;
        public bool TraceLogging { get; set; }

        private readonly ISPIBus Bus;
        private readonly IDigitalOut CS;
        private Configuration Config = DefaultConfig;
        private readonly double ExtRefVoltage;

        /// <summary> Prepares a TI TLV2544 ADC for use. </summary>
        /// <remarks> If <c>ConversionClockSource.INTERNAL</c> is used, there must be at least 4us or 8us of delay between SPI transactions for short and long sampling respectively. </remarks>
        /// <param name="SPIBus"> The SPI bus used to communicate with the device. </param>
        /// <param name="ChipSelect"> The output used as chip select for the device. </param>
        /// <param name="ExtRefVoltage"> Set this to the reference voltage only if using an external voltage reference. Expected values are between 0 and 5.5V. Leave as NaN if using internal reference, then select your desired reference via <c>Configure(...)</c>. </param>
        public TLV2544(ISPIBus SPIBus, IDigitalOut ChipSelect, double ExtRefVoltage = double.NaN)
        {
            this.Bus = SPIBus;
            this.CS = ChipSelect;
            this.ExtRefVoltage = ExtRefVoltage;
            this.Inputs = new AnalogueInTLV254x[4];
            for (byte i = 0; i < this.Inputs.Length; i++) { this.Inputs[i] = new AnalogueInTLV254x(this, i); }
        }

        /// <summary> Applies the specified configuration, and prepares the device for use. </summary>
        /// <param name="Config"> The configuration to apply. </param>
        public void Configure(Configuration Config)
        {
            if (Config.VoltageRef == VoltageReference.EXTERNAL && this.ExtRefVoltage == double.NaN) { throw new InvalidOperationException("If using an external reference voltage, you must supply it in the TLV2544 constructor."); }
            this.Config = Config;
            DoCommand(Command.WRITE_CONF, 0x000); // Power-up requirement
            ushort ConfigReg = 0x000;
            ConfigReg = (ushort)(ConfigReg | ((Config.VoltageRef != VoltageReference.EXTERNAL) ? (0b1 << 11) : (0b0 << 11)));
            ConfigReg = (ushort)(ConfigReg | ((Config.VoltageRef == VoltageReference.INTERNAL_2V) ? (0b1 << 10) : (0b0 << 10)));
            ConfigReg = (ushort)(ConfigReg | (Config.UseLongSample ? (0b1 << 9) : (0b0 << 9)));
            ConfigReg = (ushort)(ConfigReg | (((byte)Config.ConversionClockSrc) & 0b11) << 7);
            ConfigReg = (ushort)(ConfigReg | (((byte)Config.ConversionMode) & 0b1111) << 3);
            ConfigReg = (ushort)(ConfigReg | (Config.UseEOCPin ? (0b1 << 2) : (0b0 << 2)));
            ConfigReg = (ushort)(ConfigReg | ((byte)Config.FIFOTriggerLevel) & 0b11);
            DoCommand(Command.WRITE_CONF, ConfigReg);
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

        /// <summary> Tests the internal reference voltages to check if output values are reasonable. </summary>
        /// <returns> Whether each reading was within 0.5% of the expected value. </returns>
        public bool Test()
        {
            ushort HalfScale = DoInputRead(-1);
            ushort GND = DoInputRead(-2);
            ushort FullScale = DoInputRead(-3);
            return (Math.Abs(HalfScale - (4095 / 2)) < 21) && (Math.Abs(FullScale - 4095) < 21) && (Math.Abs(GND) < 21);
        }

        /// <summary> Gets the currently applied configuration. </summary>
        /// <returns> The configuration that the ADC is using, as from the CONFIG register. </returns>
        public ushort ReadConfig() => DoCommand(Command.READ_CONF);

        private ushort GetRawInput(byte Channel) => DoInputRead((sbyte)Channel);

        /// <summary> Gets the current full-scale voltage. </summary>
        /// <returns> The voltage represented by a reading of 4095 on an input. </returns>
        private double GetRange()
        {
            switch (this.Config.VoltageRef)
            {
                case VoltageReference.EXTERNAL: return this.ExtRefVoltage;
                case VoltageReference.INTERNAL_2V: return 2;
                case VoltageReference.INTERNAL_4V: return 4;
                default: return double.NaN;
            }
        }

        /// <summary> Reads an input channel. </summary>
        /// <param name="Channel"> 0 to 4 for regular input channels, -1 to -3 for test voltages. </param>
        /// <returns> The raw ADC reading, which needs further processing and referencing in order to be a usable voltage. </returns>
        private ushort DoInputRead(sbyte Channel)
        {
            Command ChSel;
            switch (Channel)
            {
                case 0: ChSel = Command.SEL_CH0; break;
                case 1: ChSel = Command.SEL_CH1; break;
                case 2: ChSel = Command.SEL_CH2; break;
                case 3: ChSel = Command.SEL_CH3; break;
                case -1: ChSel = Command.SEL_TEST1; break;
                case -2: ChSel = Command.SEL_TEST2; break;
                case -3: ChSel = Command.SEL_TEST3; break;
                default: ChSel = Command.SEL_CH0; break;
            }
            if (this.Config.ConversionMode == ConversionMode.SINGLE_SHOT)
            {
                ushort StartRead = DoCommand(ChSel, Long: true);
                return DoCommand(Command.READ_FIFO);
            }
            return 0;
        }

        /// <summary> Does a 12b read/write with the specified command. </summary>
        /// <param name="Command"> The command (4 MSb) to send. </param>
        /// <param name="Data"> The data (12 LSb) to send. </param>
        /// <param name="Long"> Whether to send out additional SCLK pulses for the ADC to do sampling. </param>
        /// <returns> The 12b data returned by the device. </returns>
        private ushort DoCommand(Command Command, ushort Data = 0x000, bool Long = false)
        {
            byte[] DataOut;
            if (!Long) { DataOut = new byte[2]; } // Regular command, no sampling.
            else
            {
                switch (this.Config.ConversionClockSrc) // We are doing sampling, determine the correct number of SCLKs.
                {
                    case ConversionClockSrc.SCLK:
                        DataOut = (this.Config.UseLongSample ? new byte[2 + 4] : new byte[2 + 2]);
                        break;
                    case ConversionClockSrc.SCLK_HALF:
                        DataOut = (this.Config.UseLongSample ? new byte[2 + 7] : new byte[2 + 4]);
                        break;
                    case ConversionClockSrc.SCLK_QUARTER:
                        DataOut = (this.Config.UseLongSample ? new byte[2 + 14] : new byte[2 + 7]);
                        break;
                    case ConversionClockSrc.INTERNAL: // We are assuming the time between The two SPI transactions is at least 4us for short, or 8us for long sampling. This seems to always be true on the Pi, but is not guaranteed.
                        DataOut = new byte[2]; // No need for additional SCLKs.
                        break;
                    default:
                        DataOut = new byte[2];
                        break;
                }
            }

            DataOut[0] = (byte)((((byte)Command << 4) & 0b1111_0000) | ((Data >> 8) & 0b0000_1111));
            DataOut[1] = (byte)(Data & 0b1111_1111);
            if (this.TraceLogging) { Log.Trace(this, "Sending command: " + UtilMain.BytesToNiceString(DataOut, true)); }
            byte[] DataIn = this.Bus.Write(this.CS, DataOut, DataOut.Length);
            if (this.TraceLogging) { Log.Trace(this, "Received: " + UtilMain.BytesToNiceString(DataIn, true)); }
            return (ushort)(Command == Command.READ_CONF ?
                (((DataIn[0] & 0b0000_1111) << 8) | (DataIn[1])) :
                (DataIn[0] << 4) | ((DataIn[1] & 0b1111_0000) >> 4));
        }

        #region Structs and Enums
        public struct Configuration
        {
            public VoltageReference VoltageRef;
            public bool UseLongSample;

            /// <summary> If true, pin 4 outputs "End of Conversion" signal. Otherwise, outputs "~Interrupt" signal. </summary>
            /// End of Conversion: "This output goes from a high-to-low logic level at the end of the sampling period and remains low until the conversion is complete and data are ready for transfer. EOC is used in conversion mode 00 only."
            /// ~Interrupt: "This pin can also be programmed as an interrupt output signal to the host processor. The falling edge of ~INT indicates data are ready for output. The following ~CS↓ or ~FS clears ~INT."
            public bool UseEOCPin;

            public ConversionClockSrc ConversionClockSrc;
            internal ConversionMode ConversionMode; // Internal because currently we only support single-shot mode.
            internal FIFOTrigger FIFOTriggerLevel; // Internal because currently we only support single-shot mode (and FIFO doesn't matter in that case).
        }

        /// <summary> Note that to use the 4V internal reference, Vcc must be at 5V (does not work in 3.3V mode). </summary>
        public enum VoltageReference { INTERNAL_4V, INTERNAL_2V, EXTERNAL }

        public enum ConversionClockSrc : byte
        {
            INTERNAL = 0b00,
            SCLK = 0b01,
            SCLK_HALF = 0b11, // Yes, these are supposed to be out of order.
            SCLK_QUARTER = 0b10
        }

        public enum ConversionMode : byte
        {
            SINGLE_SHOT = 0b00_00,
            REPEAT = 0b01_00,
            SWEEP_MODE0 = 0b10_00,
            SWEEP_MODE1 = 0b10_01,
            SWEEP_MODE2 = 0b10_10,
            SWEEP_MODE3 = 0b10_11,
            REPEAT_SWEEP_MODE0 = 0b11_00,
            REPEAT_SWEEP_MODE1 = 0b11_01,
            REPEAT_SWEEP_MODE2 = 0b11_10,
            REPEAT_SWEEP_MODE3 = 0b11_11
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
            SEL_TEST1 = 0xB, // Half-scale
            SEL_TEST2 = 0xC, // GND
            SEL_TEST3 = 0xD, // Full-scale
            READ_FIFO = 0xE
        }
        #endregion
    }
}
