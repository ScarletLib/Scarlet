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
            /// <summary> Gets the output range. </summary>
            /// <returns> The maximum output voltage. </returns>
            public double GetRange()
            {
                throw new NotImplementedException();
            }

            /// <summary> Sets the output to the specified level. </summary>
            /// <param name="Output"> The voltage to output. </param>
            public void SetOutput(double Output)
            {
                throw new NotImplementedException();
            }

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
        public enum Resolution { BitCount8, BitCount10, BitCount12 }

        public AnalogueOutMAX571x[] Outputs { get; private set; }
        private Resolution DeviceType;
        private VoltageReferenceMode ReferenceMode;
        private readonly ISPIBus SPI;
        private readonly IDigitalOut ChipSelect;

        public MAX571x(ISPIBus SPI, IDigitalOut ChipSelect, Resolution DACType, VoltageReferenceMode ReferenceMode)
        {
            this.SPI = SPI;
            this.ChipSelect = ChipSelect;
            this.DeviceType = DACType;
            this.ReferenceMode = ReferenceMode;
            ConfigureReference(this.ReferenceMode);
        }

        /// <summary> Sets the reference operation mode. This is done by default at startup, so is only needed if you want to change reference modes during runtime. </summary>
        /// <param name="ReferenceMode"> One of the external, or 3 available internal reference modes to use. </param>
        /// <param name="PowerSavingMode"> If this is true, the internal reference is shut down when all DAC outputs are disabled to save power. Used if outputs are only used occasionally, and minimum power usage is important. </param>
        public void ConfigureReference(VoltageReferenceMode ReferenceMode, bool PowerSavingMode = false)
        {
            this.ReferenceMode = ReferenceMode;
            byte Ref = (byte)Register.REF;
            Ref = (byte)((Ref & 0b1111_1000) | (PowerSavingMode ? 0b000 : 0b100) | ((byte)ReferenceMode & 0b11)); 
            this.SPI.Write(this.ChipSelect, new byte[] { Ref, 0x00, 0x00 }, 3);
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
