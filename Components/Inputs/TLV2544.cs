using Scarlet.IO;
using Scarlet.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
            private TLV2544 Parent;
            private byte Channel;

            public AnalogueInTLV254x(TLV2544 Parent, byte Channel)
            {
                this.Parent = Parent;
                this.Channel = Channel;
            }

            public void Dispose() { }

            public double GetInput() => ((double)(this.Parent.GetRawInput(this.Channel)) / GetRawRange()) / GetRange();

            public double GetRange() => this.Parent.GetRange();

            public long GetRawInput() => this.Parent.GetRawInput(this.Channel);

            public long GetRawRange() => 2 << 12;
        }

        public static readonly Configuration DefaultConfig = new Configuration()
        {
            VoltageRef = VoltageReference.INTERNAL_4V,
            UseLongSample = false,
            ConversionClockSrc = ConversionClockSrc.INTERNAL,
            ConversionMode = ConversionMode.SINGLE_SHOT,
            UseEOCPin = false,
            FIFOTriggerLevel = FIFOTrigger.FIFO_8b
        };
        public readonly AnalogueInTLV254x[] Inputs;

        private readonly ISPIBus Bus;
        private readonly IDigitalOut CS;
        private Configuration Config = DefaultConfig;
        private sbyte ReuseChannel = -1;
        private double ExtRefVoltage;

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
            ConfigReg = (ushort)(ConfigReg | ((Config.VoltageRef == VoltageReference.EXTERNAL) ? (0b1 << 11) : (0b0 << 11)));
            ConfigReg = (ushort)(ConfigReg | ((Config.VoltageRef == VoltageReference.INTERNAL_2V) ? (0b1 << 10) : (0b0 << 10)));
            ConfigReg = (ushort)(ConfigReg | (Config.UseLongSample ? (0b1 << 9) : (0b0 << 9)));
            ConfigReg = (ushort)(ConfigReg | (((byte)Config.ConversionClockSrc) & 0b11) << 7);
            ConfigReg = (ushort)(ConfigReg | (((byte)Config.ConversionMode) & 0b1111) << 3);
            ConfigReg = (ushort)(ConfigReg | (Config.UseEOCPin ? (0b1 << 2) : (0b0 << 2)));
            ConfigReg = (ushort)(ConfigReg | ((byte)Config.FIFOTriggerLevel) & 0b11);
            DoCommand(Command.WRITE_CONF, ConfigReg);
            if (Config.ConversionMode != ConversionMode.SINGLE_SHOT && Config.ConversionMode != ConversionMode.REPEAT) { this.ReuseChannel = -1; }
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

        public ushort Test1()
        {
            ushort Read = DoCommand(Command.SEL_TEST1, 0, true);
            Read = DoCommand(Command.SEL_TEST1);
            return Read;
        }

        public ushort Test2()
        {
            ushort Read = DoCommand(Command.SEL_TEST2, 0, true);
            Read = DoCommand(Command.SEL_TEST2);
            return Read;
        }

        public ushort Test3()
        {
            ushort Read = DoCommand(Command.SEL_TEST3, 0, true);
            Read = DoCommand(Command.SEL_TEST3);
            return Read;
        }

        public ushort ReadConfig()
        {
            ushort Read = DoCommand(Command.READ_CONF);
            return Read;
        }

        private ushort GetRawInput(byte Channel)
        {
            Command ChSel;
            switch(Channel)
            {
                case 0: ChSel = Command.SEL_CH0; break;
                case 1: ChSel = Command.SEL_CH1; break;
                case 2: ChSel = Command.SEL_CH2; break;
                case 3: ChSel = Command.SEL_CH3; break;
                default: ChSel = Command.SEL_CH0; break;
            }
            if (this.Config.ConversionMode == ConversionMode.SINGLE_SHOT)
            {
                ushort ReturnCmd = DoCommand(ChSel);
                ushort ReturnCmd2 = DoCommand(ChSel);
                ushort Read = DoCommand(Command.READ_FIFO);
                Log.Output(Log.Severity.DEBUG, Log.Source.HARDWAREIO, "[TLV2544Cai] Read back " + ReturnCmd + "and " + ReturnCmd2 + " and " + Read);
                return Read;
            }
            return 0;
        }

        private double GetRange()
        {
            switch (this.Config.VoltageRef)
            {
                case VoltageReference.EXTERNAL: return this.ExtRefVoltage;
                case VoltageReference.INTERNAL_2V: return 2; // TODO: Check this.
                case VoltageReference.INTERNAL_4V: return 4;
                default: return double.NaN;
            }
        }

        /// <summary> Does a 12b read/write with the specified command. </summary>
        /// <param name="Command"> The command (4 MSb) to send. </param>
        /// <param name="Data"> The data (12 LSb) to send. </param>
        /// <returns> The 12b data returned by the device. </returns>
        private ushort DoCommand(Command Command, ushort Data = 0x000, bool Long = false)
        {
            byte[] DataOut;
            if (!Long) { DataOut = new byte[] { (byte)((((byte)Command << 4) & 0b1111_0000) | ((Data >> 8) & 0b0000_1111)), (byte)(Data & 0b1111_1111) }; }
            else { DataOut = new byte[] { (byte)((((byte)Command << 4) & 0b1111_0000) | ((Data >> 8) & 0b0000_1111)), (byte)(Data & 0b1111_1111), 0, 0, 0 }; }
            byte[] DataOutCopy = new byte[DataOut.Length];
            Array.Copy(DataOut, DataOutCopy, DataOut.Length);
            byte[] DataIn = this.Bus.Write(this.CS, DataOutCopy, DataOutCopy.Length);
            Log.Output(Log.Severity.DEBUG, Log.Source.HARDWAREIO, "[TLV2544Cai] Sent " + UtilMain.BytesToNiceString(DataOut, true) + ", got " + UtilMain.BytesToNiceString(DataIn, true));
            return (ushort)(Command == Command.READ_CONF ?
                (((DataIn[0] & 0b0000_1111) << 8) | (DataIn[1])) :
                (DataIn[0] << 4) | ((DataIn[1] & 0b1111_0000) >> 4));
        }

        public struct Configuration
        {
            public VoltageReference VoltageRef;
            public bool UseLongSample;
            public ConversionClockSrc ConversionClockSrc;
            public ConversionMode ConversionMode;

            /// <summary> If true, pin 4 outputs "End of COnversion" signal. Otherwise, outputs "~Interrupt" signal. </summary>
            /// End of Conversion: "This output goes from a high-to-low logic level at the end of the sampling period and remains low until the conversion is complete and data are ready for transfer. EOC is used in conversion mode 00 only."
            /// ~Interrupt: "This pin can also be programmed as an interrupt output signal to the host processor. The falling edge of ~INT indicates data are ready for output. The following ~CS↓ or ~FS clears ~INT."
            public bool UseEOCPin;
            public FIFOTrigger FIFOTriggerLevel;
        }

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
    }
}
