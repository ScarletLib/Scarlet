using Scarlet.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scarlet.Components.Outputs
{
    /// <summary>
    /// Ultra-Small, Quad-Channel, 8-/10-/12-bit Buffered Output DAC with Internal Reference and SPI Interface.
    /// Datasheet: https://datasheets.maximintegrated.com/en/ds/MAX5713-MAX5715.pdf
    /// </summary>
    public class MAX571x
    {
        public class AnalogueOutMAX571x : IAnalogueOut
        {
            private readonly MAX571x Parent;
            private readonly byte ChannelID;

            internal AnalogueOutMAX571x(MAX571x Parent, byte ChannelID)
            {
                this.Parent = Parent;
                this.ChannelID = ChannelID;
            }

            /// <summary> Gets the output range. </summary>
            /// <returns> The maximum output voltage. </returns>
            public double GetRange() { return this.Parent.GetRange(); }

            /// <summary> Sets the output to the specified level. </summary>
            /// <param name="Output"> The voltage to output. </param>
            public void SetOutput(double Output) { this.Parent.SetOutput(this.ChannelID, Output); }

            /// <summary> Gives the DAC the next output value without actually outputting it yet. Use <see cref="UpdateOutput"/> to update one channel or <see cref="MAX571x.UpdateAllOutputs"/> to update all channels simultaneously after using this. </summary>
            /// <param name="Output"> The voltage value to prepare for outputting. </param>
            public void PrepareOutput(double Output) { this.Parent.PrepareOutput(this.ChannelID, Output); }

            /// <summary> Sets the output value to the previously prepared one. Use <see cref="PrepareOutput"/> or <see cref="MAX571x.PrepareAllOutputs"/> to set this. If no value is prepared, no change will occur. </summary>
            public void UpdateOutput() { this.Parent.UpdateOutput(this.ChannelID); }

            // TODO: Consider implementing channel shutdown / shutdown mode configuration.

            public void Dispose() { }
        }

        /// <summary> Determines the voltage reference mode to use. Select between an external reference, or one of three internal ones (2.048V, 2.500V, or 4.096V). </summary>
        public enum VoltageReferenceMode : byte
        {
            /// <summary> The internal 2.048V reference. Gives resolution of 8mV / 2mV / 0.5mV, depending on chip. </summary>
            Reference2V048 = 0b10,

            /// <summary> The internal 2.500V reference. Gives resolution of 9.77mV / 2.44mV / 0.61mV, depending on chip. </summary>
            Reference2V500 = 0b01,

            /// <summary> The internal 4.096V reference. Chip must be powered from 5V supply for this to work. Gives resolution of 16mV / 4mV / 1mV, depending on chip. </summary>
            Reference4V096 = 0b11,

            /// <summary> Uses the external reference input. Resolution depends on reference voltage. </summary>
            ReferenceExternal = 0b00
        }

        /// <summary> The resolution of the device, set by which model is being used. MAX5713 = 8b, MAX5714 = 10b, MAX5715 = 12b. </summary>
        public enum Resolution : uint { BitCount8 = 256, BitCount10 = 1024, BitCount12 = 4096 }

        public AnalogueOutMAX571x[] Outputs { get; private set; }
        private readonly Resolution DeviceType;
        private VoltageReferenceMode ReferenceMode;
        public double ExternalRefVoltage { get; private set; }
        private readonly ISPIBus SPI;
        private readonly IDigitalOut ChipSelect;

        /// <summary> Prepares the DAC for use. </summary>
        /// <remarks> The DAC powers up with all outputs set to 0V. This is not changed until you set the voltage of the channels yourself. </remarks>
        /// <param name="SPI"> The SPI bus to use to communicate with the DAC. </param>
        /// <param name="ChipSelect"> The chip select line connected to the DAC. </param>
        /// <param name="DACType"> The type of DAC you are connecting to. Choosing incorrectly will cause outputs to be off by a factor of 4 or 16. </param>
        /// <param name="ReferenceMode"> Whether to use an external reference, or one of the 3 internal references. </param>
        /// <param name="ExternalRefVoltage"> When operating in external reference mode, you must provide the voltage of the external reference here. </param>
        public MAX571x(ISPIBus SPI, IDigitalOut ChipSelect, Resolution DACType, VoltageReferenceMode ReferenceMode, double ExternalRefVoltage = double.NaN)
        {
            this.SPI = SPI;
            this.ChipSelect = ChipSelect;
            this.DeviceType = DACType;
            this.ReferenceMode = ReferenceMode;
            this.ExternalRefVoltage = ExternalRefVoltage;
            this.Outputs = new AnalogueOutMAX571x[4];
            for (byte i = 0; i < this.Outputs.Length; i++) { this.Outputs[i] = new AnalogueOutMAX571x(this, i); }
            ConfigureReference(this.ReferenceMode, ExternalRefVoltage);
        }

        /// <summary> Sets the reference operation mode. This is done by default at startup, so is only needed if you want to change reference modes during runtime. </summary>
        /// <param name="ReferenceMode"> One of the external, or 3 available internal reference modes to use. </param>
        /// <param name="ExternalRefVoltage"> When operating in external reference mode, you must provide the voltage of the external reference here. </param>
        /// <param name="PowerSavingMode"> If this is true, the internal reference is shut down when all DAC outputs are disabled to save power. Used if outputs are only used occasionally, and minimum power usage is important. </param>
        public void ConfigureReference(VoltageReferenceMode ReferenceMode, double ExternalRefVoltage = double.NaN, bool PowerSavingMode = false)
        {
            if (ReferenceMode == VoltageReferenceMode.ReferenceExternal && (double.IsNaN(ExternalRefVoltage) || ExternalRefVoltage < 1.2 || ExternalRefVoltage > 6)) { throw new ArgumentException("When using an external voltage reference, you must provide the reference voltage. Either none was provided, or it was out of range."); }
            this.ReferenceMode = ReferenceMode;
            this.ExternalRefVoltage = ExternalRefVoltage;
            byte Ref = (byte)Register.REF;
            Ref = (byte)((Ref & 0b1111_1000) | (PowerSavingMode ? 0b000 : 0b100) | ((byte)ReferenceMode & 0b11)); 
            this.SPI.Write(this.ChipSelect, new byte[] { Ref, 0x00, 0x00 }, 3);
        }

        /// <summary> Updates all channels with previously prepared values at once. Use <see cref="PrepareAllOutputs"/> or <see cref="AnalogueOutMAX571x.PrepareOutput(double)"/> to prepare one or more outputs. Outputs without prepared values will not change from their current values. </summary>
        public void UpdateAllOutputs() { this.SPI.Write(this.ChipSelect, new byte[] { (byte)Register.LOAD_ALL, 0x00, 0x00 }, 3); }

        /// <summary> Prepares all channels with new data, but does not output yet. Use <see cref="UpdateAllOutputs"/> or <see cref="AnalogueOutMAX571x.UpdateOutput"/> to output this prepared value. Useful for synchronizing updates. </summary>
        /// <param name="Output"> THe new voltage level to output after an update is performed. </param>
        public void PrepareAllOutputs(double Output)
        {
            ushort Code = VoltageToCode(Output);
            this.SPI.Write(this.ChipSelect, new byte[3] { (byte)Register.CODE_ALL, (byte)(Code >> 8), (byte)Code }, 3);
        }

        /// <summary> Immediately, simultaneously sets all outputs to the specified value. </summary>
        /// <param name="Output"> THe voltage to set all DAC outputs to. </param>
        public void SetAllOutputs(double Output)
        {
            ushort Code = VoltageToCode(Output);
            this.SPI.Write(this.ChipSelect, new byte[3] { (byte)Register.CODE_ALL_LOAD_ALL, (byte)(Code >> 8), (byte)Code}, 3);
        }

        /// <summary> Updates the specified channel with previously prepared value. Use <see cref="PrepareAllOutputs"/> or <see cref="AnalogueOutMAX571x.PrepareOutput(double)"/> to prepare one or more outputs. If no value was prepared for this channel, output will not change from the current value. </summary>
        /// <param name="Channel"> THe channel to update. </param>
        private void UpdateOutput(byte Channel) { this.SPI.Write(this.ChipSelect, new byte[] { (byte)((byte)Register.LOADn | (Channel & 0b11)), 0x00, 0x00 }, 3); }

        /// <summary> Prepares the channel with new data, but does not output yet. Use <see cref="UpdateAllOutputs"/> or <see cref="AnalogueOutMAX571x.UpdateOutput"/> to output this prepared value. Useful for synchronizing updates. </summary>
        /// <param name="Channel"> The channel to prepare data on. </param>
        /// <param name="Output"> The voltage value to prepare for output. </param>
        private void PrepareOutput(byte Channel, double Output)
        {
            ushort Code = VoltageToCode(Output);
            this.SPI.Write(this.ChipSelect, new byte[3] { (byte)((byte)Register.CODEn | (Channel & 0b11)), (byte)(Code >> 8), (byte)Code }, 3);
        }

        /// <summary> Immediately outputs the given value on the specified channel. </summary>
        /// <param name="Channel"> The channel to set the output on. </param>
        /// <param name="Output"> The new voltage to output on the given channel. </param>
        private void SetOutput(byte Channel, double Output)
        {
            ushort Code = VoltageToCode(Output);
            this.SPI.Write(this.ChipSelect, new byte[3] { (byte)((byte)Register.CODEn_LOADn | (Channel & 0b11)), (byte)(Code >> 8), (byte)Code }, 3);
        }

        /// <summary> Gets the maximum value that can be output. This depends on the reference voltage. </summary>
        /// <returns> THe highest voltage that the DAC can output. </returns>
        public double GetRange()
        {
            switch (this.ReferenceMode)
            {
                case VoltageReferenceMode.Reference2V048: return 2.048;
                case VoltageReferenceMode.Reference2V500: return 2.500;
                case VoltageReferenceMode.Reference4V096: return 4.096;
                default: return this.ExternalRefVoltage;
            }
        }

        /// <summary> Translates a desired output voltage into the DAC's CODE value. Handles values outside available range by capping. </summary>
        /// <param name="Voltage"> The voltage to convert. </param>
        /// <returns> The register-fitted code value. Already left-aligned. </returns>
        private ushort VoltageToCode(double Voltage)
        {
            double Range = GetRange();
            if (Voltage < 0) { Voltage = 0; }
            else if (Voltage > Range) { Voltage = Range; }

            double BitValue = Range / (uint)this.DeviceType;
            ushort RawValue = (ushort)Math.Round(Voltage / BitValue);
            switch(this.DeviceType)
            {
                case Resolution.BitCount8: return (ushort)(RawValue << 8);
                case Resolution.BitCount10: return (ushort)(RawValue << 6);
                case Resolution.BitCount12: return (ushort)(RawValue << 4);
                default: return RawValue;
            }
        }

        private enum Register : byte
        { // Register = Internal data storage. Latching = setting output to register value.
            CODEn = 0b0000_0000, // Updates register without latching to output
            LOADn = 0b0001_0000, // Latches register contents to output without changing
            CODEn_LOAD_ALL = 0b0010_0000, // Updates register, latches all outputs
            CODEn_LOADn = 0b0011_0000, // Updates register, latches selected output
            POWER = 0b0100_0000, // Sets the selected output(s) to on, or off by 1k to GND, 100k to GND, or Hi-Z
            SW_CLEAR = 0b0101_0000, // Sets all registers to 0 and latches
            SW_RESET = 0b0101_0001, // Sets all registers to 0, latches, and resets configuration to defaults
            CONFIG = 0b0110_0000, // Configure how the latches operate
            REF = 0b0111_0000, // Configures voltage reference
            CODE_ALL = 0b1000_0000, // Updates all registers without latching outputs
            LOAD_ALL = 0b1000_0001, // Latches to all outputs without changing registers
            CODE_ALL_LOAD_ALL = 0b1000_0010, // Updates all registers, then latches to all outputs
            NOP = 0b1100_0000 // Does nothing
        }

        // internal reference initially powered down
        // All serial operations 24b long
        // DAC data left-justified. (8b: data << 8, 10b: data << 6, 12b: data << 4)

    }
}
