using System;
using Scarlet.IO;

namespace Scarlet.Components.Outputs
{
    public class RGBLED
    {
        private readonly IPWMOutput Red, Green, Blue;

        /// <summary> Gets or sets a value indicating whether colour outputs should be flipped. E.g. 0x14 Green will become 0xEB if enabled. </summary>
        public bool Inverted { get; set; }

        /// <summary> Gets or sets the factor to scale red output by. Value of 1 will provide no scaling. </summary>
        public float RedScale { get; set; }

        /// <summary> Gets or sets the factor to scale green output by. Value of 1 will provide no scaling. </summary>
        public float GreenScale { get; set; }

        /// <summary> Gets or sets the factor to scale blue output by. Value of 1 will provide no scaling. </summary>
        public float BlueScale { get; set; }

        /// <summary> Provides easy output controls for an RGB LED. </summary>
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
            SetOutput(0x811426);
        }

        /// <summary> Sets the LED's colour. </summary>
        /// <remarks> If <see cref="Inverted"/> is set, colour gradients are reversed. E.g. 0x14 Green will become 0xEB. </remarks>
        /// <param name="Colour"> The colour to output, in standard form like 0x811426 for 0x81 Red, 0x14 Green, 0x26 Blue. </param>
        public void SetOutput(uint Colour)
        {
            float RedOut = (((Colour >> 16) & 0xFF) / 255.000F) * this.RedScale;
            float GreenOut = (((Colour >> 8) & 0xFF) / 255.000F) * this.GreenScale;
            float BlueOut = ((Colour & 0xFF) / 255.000F) * this.BlueScale;
            RedOut = Math.Max(Math.Min(RedOut, 1), 0); // Caps to 0 through 1.
            GreenOut = Math.Max(Math.Min(GreenOut, 1), 0);
            BlueOut = Math.Max(Math.Min(BlueOut, 1), 0);

            this.Red.SetOutput(this.Inverted ? (1 - RedOut) : RedOut);
            this.Green.SetOutput(this.Inverted ? (1 - GreenOut) : GreenOut);
            this.Blue.SetOutput(this.Inverted ? (1 - BlueOut) : BlueOut);
        }

        /// <summary> Converts a given value to a range in a red-green colour gradient. </summary>
        /// <param name="Now"> The current value to plot. </param>
        /// <param name="Worst"> The value that should correspond to full red. </param>
        /// <param name="Best"> The value that should correspond to full green. </param>
        /// <returns> Either a red-green gradient value, or white (0xFFFFFF) if the value falls outside of the range. </returns>
        public static uint RedGreenGradient(double Now, double Worst, double Best)
        {
            bool Increasing = Worst < Best;
            if ((Increasing && (Now > Best || Now < Worst)) || // Outside range
                ((!Increasing) && (Now < Best || Now > Worst)) ||
                Worst == Best || Worst == double.NaN || Best == double.NaN) { return 0xFFFFFF; }

            double Fraction = 0;
            if (Increasing) { Fraction = (Now - Worst) / (Best - Worst); }
            else { Fraction = 1 - ((Now - Best) / (Worst - Best)); }
            byte Red = (byte)((1 - Fraction) * 0xFF);
            byte Green = (byte)(Fraction * 0xFF);
            return (uint)(((Red & 0xFF) << 16) | ((Green & 0xFF) << 8));
        }

        /// <summary> Sets the frequency of the PWM outputs for all channels. </summary>
        /// <param name="Frequency"> The frequency to set the PWM output to. </param>
        public void SetFrequency(int Frequency)
        {
            this.Red.SetFrequency(Frequency);
            this.Green.SetFrequency(Frequency);
            this.Blue.SetFrequency(Frequency);
        }

        /// <summary> Sets the enabled state of all three PWM channels. </summary>
        /// <param name="Enable"> Enables/disables the PWM outputs. </param>
        public void SetEnabled(bool Enable)
        {
            this.Red.SetEnabled(Enable);
            this.Green.SetEnabled(Enable);
            this.Blue.SetEnabled(Enable);
        }
    }
}
