using System;
using Scarlet.IO;

namespace Scarlet.Components.Interfaces
{
    /// <summary>
    /// 16-bit Low-Power I/O Expander for I2C Bus with Interrupt
    /// Datasheet: https://www.onsemi.com/pub/Collateral/PCA9535E-D.PDF
    /// </summary>
    public class PCA9535E
    {
        public class PCA9535Input : IDigitalIn
        {
            private readonly PCA9535E Parent;
            private readonly byte ID;

            internal PCA9535Input(PCA9535E Parent, byte ChannelID)
            {
                this.Parent = Parent;
                this.ID = ChannelID;
            }

            /// <summary> Gets the current input state. </summary>
            /// <returns> The state of this input pin. </returns>
            /// <exception cref="InvalidOperationException"> If this pin is set to output mode. </exception>
            public bool GetInput()
            {
                if (this.Parent.IsChannelOutput[this.ID]) { throw new InvalidOperationException("Channel " + this.ID + " is set to output mode, cannot get input."); }
                return this.Parent.GetInput(this.ID);
            }

            /// <summary> PCA9535 does not have configurable input resistors, do not use this function. </summary>
            /// <exception cref="InvalidOperationException"> If this function is called, as the device does not support changing input resistors. </exception>
            /// <param name="Resistor"> Not used, as this function is not supported. </param>
            [Obsolete("PCA9535E does not have configurable input resistors.")]
            public void SetResistor(ResistorState Resistor) { throw new InvalidOperationException("PCA9535E does not have configurable input resistors."); }

            public void Dispose() { } // No action needed.
        }

        public class PCA9535Output : IDigitalOut
        {
            private readonly PCA9535E Parent;
            private readonly byte ID;

            internal PCA9535Output(PCA9535E Parent, byte ChannelID)
            {
                this.Parent = Parent;
                this.ID = ChannelID;
            }

            /// <summary>Sets the output state. </summary>
            /// <param name="Output"> Whether the pin should be logic high (true), or low (false). </param>
            public void SetOutput(bool Output)
            {
                if (!this.Parent.IsChannelOutput[this.ID]) { throw new InvalidOperationException("Channel " + this.ID + " is set to input mode, cannot set output."); }
                this.Parent.SetOutput(this.ID, Output);
            }

            public void Dispose() { } // No action needed.
        }

        private readonly II2CBus Bus;
        private readonly byte Address;

        /// <summary> Gets whether the given channel is currently in output mode (true), or input mode (false). </summary>
        public bool[] IsChannelOutput { get; private set; }
        public PCA9535Input[] Inputs { get; private set; }
        public PCA9535Output[] Outputs { get; private set; }

        /// <summary> Prepares the PCS9535E device for use. </summary>
        /// <param name="Bus"> The I2C bus used to communicate with the device. </param>
        /// <param name="Address"> The I2C address that the device is using. </param>
        public PCA9535E(II2CBus Bus, byte Address)
        {
            this.Bus = Bus;
            this.Address = Address;
            this.IsChannelOutput = new bool[16];
            this.Inputs = new PCA9535Input[16];
            this.Outputs = new PCA9535Output[16];
            for (byte i = 0; i < this.Inputs.Length; i++)
            {
                this.IsChannelOutput[i] = false;
                this.Inputs[i] = new PCA9535Input(this, i);
                this.Outputs[i] = new PCA9535Output(this, i);
            }
        }

        /// <summary> Sets the channel's I/O mode. </summary>
        /// <param name="Channel"> The channel to configure. Channels 0-15 available. </param>
        /// <param name="IsOutput"> Whether to make the specified channel an input or output. </param>
        /// <exception cref="ArgumentException"> If an invalid channel is selected. </exception>
        public void SetChannelMode(byte Channel, bool IsOutput)
        {
            if (Channel > 15) { throw new ArgumentException("Only channels 0-15 available, tried to set mode on channel " + Channel); }
            this.IsChannelOutput[Channel] = IsOutput;
            byte RegisterID = (byte)((Channel > 7) ? 7 : 6);
            byte Config = this.Bus.ReadRegister(this.Address, RegisterID, 1)[0];
            Config = (byte)(Config & ~(0b1 << (Channel % 8)) | ((IsOutput ? 0 : 1) << (Channel % 8)));
            this.Bus.WriteRegister(this.Address, RegisterID, new byte[] { Config });
        }

        private bool GetInput(byte Channel)
        {
            byte RegisterID = (byte)((Channel > 7) ? 1 : 0);
            byte State = this.Bus.ReadRegister(this.Address, RegisterID, 1)[0];
            return ((State >> (Channel % 8)) & 0b1) == 1;
        }

        private void SetOutput(byte Channel, bool State)
        {
            byte RegisterID = (byte)((Channel > 7) ? 3 : 2);
            byte Output = this.Bus.ReadRegister(this.Address, RegisterID, 1)[0];
            Output = (byte)(Output & ~(0b1 << (Channel % 8)) | ((State ? 1 : 0) << (Channel % 8)));
            this.Bus.WriteRegister(this.Address, RegisterID, new byte[] { Output });
        }
    }
}
