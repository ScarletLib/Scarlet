using Scarlet.IO;
using System;

namespace Scarlet.Components
{
    public class RGBLED
    {
        private IPWMOutput Red, Green, Blue;
        /// <summary> Flips colour outputs. E.g. 0x14 Green will become 0xEB if enabled. </summary>
        public bool Inverted { get; set; }
        /// <summary> Factor to scale red output by. Value of 1 will provide no scaling. </summary>
        public float RedScale { get; set; }
        /// <summary> Factor to scale green output by. Value of 1 will provide no scaling. </summary>
        public float GreenScale { get; set; }
        /// <summary> Factor to scale blue output by. Value of 1 will provide no scaling. </summary>
        public float BlueScale { get; set; }

        /// <summary> Provides easy output controls for an RBG LED. </summary>
        /// <param name="Red"> PWM output for the red LED channel. </param>
        /// <param name="Green"> PWM output for the green LED channel. </param>
        /// <param name="Blue"> PWM channel for the blue LED channel. </param>
        public RGBLED(IPWMOutput Red, IPWMOutput Green, IPWMOutput Blue)
        {
            this.Red = Red;
            this.Green = Green;
            this.Blue = Blue;
            this.RedScale = 1;
            this.GreenScale = 1;
            this.BlueScale = 1;
        }

        /// <summary> Sets the LED's colour. </summary>
        /// <remarks> If Inverted is set, colour gradients are reversed. E.g. 0x14 Green will become 0xEB. </remarks>
        /// <param name="Colour"> The colour to output, in standard form like 0x811426 for 0x81 Red, 0x14 Green, 0x26 Blue. </param>
        public void SetOutput(uint Colour)
        {
            float RedOut = (((Colour >> 16) & 0xFF) / 256.000F) * this.RedScale;
            float GreenOut = (((Colour >> 8) & 0xFF) / 256.000F) * this.GreenScale;
            float BlueOut = ((Colour & 0xFF) / 256.000F) * this.BlueScale;
            RedOut = Math.Max(Math.Min(RedOut, 1), 0); // Caps to 0 through 1.
            GreenOut = Math.Max(Math.Min(GreenOut, 1), 0);
            BlueOut = Math.Max(Math.Min(BlueOut, 1), 0);

            this.Red.SetOutput(this.Inverted ? (1 - RedOut) : RedOut);
            this.Green.SetOutput(this.Inverted ? (1 - GreenOut) : GreenOut);
            this.Blue.SetOutput(this.Inverted ? (1 - BlueOut) : BlueOut);
        }

        /// <summary> Sets the frequency of the PWM outputs for all channels. </summary>
        public void SetFrequency(int Frequency)
        {
            this.Red.SetFrequency(Frequency);
            this.Green.SetFrequency(Frequency);
            this.Blue.SetFrequency(Frequency);
        }

        /// <summary> Sets the enabled state of all three PWM channels. </summary>
        public void SetEnabled(bool Enable)
        {
            this.Red.SetEnabled(Enable);
            this.Green.SetEnabled(Enable);
            this.Blue.SetEnabled(Enable);
        }
    }
}
