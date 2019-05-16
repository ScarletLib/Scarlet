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

        public enum VoltageReferenceMode { Reference2V048, Reference2V500, Reference4V096, ReferenceExternal };

        public enum Resolution { BitCount8, BitCount10, BitCount12 };

        public MAX571x(ISPIBus SPI, IDigitalOut ChipSelect, Resolution DACType)
        {

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
            CONFIG = 0b0110_0000,
            REF = 0b0111_0000,
            CODE_ALL = 0b1000_0000,
            LOAD_ALL = 0b1000_0001,
            CODE_ALL_LOAD_ALL = 0b1000_0010,
            NOP = 0b1100_0000
        }

        // internal reference initially powered down
        // All serial operations 24b long
        // DAC data left-justified. (8b: data << 8, 10b: data << 6, 12b: data << 4)

    }
}
