using Scarlet.IO;
using System;
using System.Collections.Generic;

namespace Scarlet.Components.Outputs
{
    public class PCA9685
    {
        public class PWMOutputPCA9685 : IPWMOutput
        {
            private PCA9685 Parent;
            private byte Channel;

            private bool Enabled, Polarity;
            private float DutyCycle, Delay;

            internal byte[] Config { private get; set; }

            // Configuration bits:
            // [0000_0000] [XXX 0 0000] [0000_0000] [XXX 1 0000] (Default values)
            //  ^^^^-^^^^---------^^^^---------------------------- On count (12b, LSB+MSB)
            //                           ^^^^-^^^^---------^^^^--- Off count (12b, LSB+MSB)
            //                  ^--------------------------------- Full on mode enable
            //                                           ^-------- Full off mode enable
            //              ^^^----------------------^^^---------- Reserved, non-writable

            internal PWMOutputPCA9685(byte Channel, PCA9685 Parent)
            {
                this.Channel = Channel;
                this.Parent = Parent;
                this.Enabled = false;
                this.Polarity = false;
                this.DutyCycle = 0;
                this.Delay = 0;
                this.Config = new byte[4];
            }

            // TODO: Convert to float.
            public void SetFrequency(int Frequency) => this.Parent.SetFrequency(Frequency);

            public void SetEnabled(bool Enable)
            {
                this.Enabled = Enable;
                SetConfig();
            }

            public void SetPolarity(bool NormalHigh)
            {
                this.Polarity = NormalHigh;
                SetConfig();
            }

            public void SetOutput(float DutyCycle)
            {
                if (DutyCycle > 1 || DutyCycle < 0) { throw new InvalidOperationException("Duty cycle must be between 0.0 and 1.0."); }
                this.DutyCycle = DutyCycle;
                SetConfig();
            }

            public void SetDelay(float Delay)
            {
                if (Delay >= 1 || Delay < 0) { throw new InvalidOperationException("Delay must be >= 0.0 and < 1.0."); }
                this.Delay = Delay;
                SetConfig();
            }

            private void SetConfig()
            {
                // Full on/off bits
                bool FullOn = (!this.Enabled && this.Polarity); // Full on only if we are disabled and normally high
                bool FullOff = (!this.Enabled && !this.Polarity); // Full off only if we are disabled and normally low
                this.Config[1] = (byte)(FullOn ? (this.Config[1] | 0b0001_0000) : (this.Config[1] & 0b1110_1111)); // Sets full on enable bit
                this.Config[3] = (byte)(FullOff ? (this.Config[3] | 0b0001_0000) : (this.Config[3] & 0b1110_1111)); // Sets full off enable bit

                // On/off counts
                if (!this.Polarity)
                {
                    ushort DelayTicks = (ushort)(Math.Max((this.Delay * 4096) - 1, 0)); // The time when the state should be asserted.
                    ushort OnTicks = (ushort)(Math.Max((this.DutyCycle * 4096) - 1, 0)); // For how many ticks the output should be on.
                    ushort OffTime = (ushort)((DelayTicks + OnTicks) % 4096); // The time when the state should be negated.
                }
                // TODO Implement on/off count setting.

                this.Parent.SetChannelData(this.Channel, this.Config);
            }

            public void Dispose() { throw new NotImplementedException(); }
        }

        public PWMOutputPCA9685[] Outputs { get; private set; }
        private II2CBus Bus;
        private byte PartAddress;
        private int ExtOscFreq;

        private const byte FirstModeRegisterOffset = 0x00;
        private const byte FirstLEDRegisterOffset = 0x06;
        private const byte PrescaleRegister = 0xFE;

        /// <summary> </summary>
        /// <param name="Bus"> The I2C bus that the devie will communicate over. </param>
        /// <param name="Address"> The I2C address set via the device's physical address pins. </param>
        /// <param name="ExtOscFreq"> If there is an external oscillator, set the frequency here. If there isn't, set to -1. </param>
        public PCA9685(II2CBus Bus, byte Address, int ExtOscFreq = -1)
        {
            this.Bus = Bus;
            this.PartAddress = Address;
            this.ExtOscFreq = ExtOscFreq;
            if (ExtOscFreq < -1 || ExtOscFreq > 50000000) { throw new ArgumentException("Ivalid oscillator frequency supplied. Must be bwetween 0 and 50 MHz, or -1 for internal oscillator."); }
            this.Outputs = new PWMOutputPCA9685[16];
            for (byte i = 0; i < this.Outputs.Length; i++) { this.Outputs[i] = new PWMOutputPCA9685(i, this); }
            SetupDevice();

            ReadAllStates();
        }

        private void SetupDevice()
        {
            // TODO: Implement the following:
            // - Auto-increment enable
            // - Subaddress set
            // - All call set
            // - Output change time set
            // - Invert?
            // - Output driver settings
        }

        internal void ReadAllStates()
        {
            this.Bus.Write(this.PartAddress, new byte[] { FirstLEDRegisterOffset }); // Listen up, we're about to grab output state data
            byte[] OutputData = this.Bus.Read(this.PartAddress, 64); // Gimme all your output state data!
            if (OutputData == null || OutputData.Length != 64) { throw new Exception("Reading PCA9685 output state data did not return the correct amount of bytes (64)."); }
            for (int i = 0; i < 16; i++)
            {
                byte[] ChannelData = new byte[] { OutputData[i * 4 + 0], OutputData[i * 4 + 1], OutputData[i * 4 + 2], OutputData[i * 4 + 3] };
                this.Outputs[i].Config = ChannelData;
            }
        }

        public void SetEnabledAll(bool Enable) { }

        public void SetOutputAll(float DutyCycle) { }

        public void SetFrequency(float Frequency)
        {
            // 25 MHz internal oscillator
            int Oscillator = (this.ExtOscFreq == -1 ? 25000000 : this.ExtOscFreq);
            int TempPrescale = (int)Math.Round(Oscillator / (4096 * Frequency)) - 1;
            if(TempPrescale < 3) { TempPrescale = 3; }
            if(TempPrescale > 255) { TempPrescale = 255; }
            byte PrescaleVal = (byte)(TempPrescale);

            // Put the oscillator in sleep mode.
            byte ModeSettingPre = this.Bus.ReadRegister(this.PartAddress, FirstModeRegisterOffset, 1)[0];
            byte ModeSettingNew = (byte)(ModeSettingPre | 0b0001_0000); // Set bit 4 (SLEEP) to 1 to shut down the oscillator.
            this.Bus.WriteRegister(this.PartAddress, FirstModeRegisterOffset, new byte[] { ModeSettingNew });
            // TODO: See if a delay is needed here.

            // Set the frequency prescaler register.
            this.Bus.WriteRegister(this.PartAddress, PrescaleRegister, new byte[] { PrescaleVal });

            // Set the SLEEP bit back to what it was.
            this.Bus.WriteRegister(this.PartAddress, FirstModeRegisterOffset, new byte[] { ModeSettingPre });
            // TODO: See if a delay is needed here.
        }

        internal void SetChannelData(int Channel, byte[] Data)
        {

        }
    }
}
