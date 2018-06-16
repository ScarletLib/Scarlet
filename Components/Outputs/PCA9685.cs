using Scarlet.IO;
using Scarlet.Utilities;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Scarlet.Components.Outputs
{
    /// <summary>
    /// 16-channel, 12-bit PWM Fm+ I2C bus LED controller.
    /// Datasheet: https://www.nxp.com/docs/en/data-sheet/PCA9685.pdf
    /// </summary>
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

            public void SetFrequency(float Frequency) => this.Parent.SetFrequency(Frequency);

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

            public void Reset() { this.Config = new byte[] { 0x00, 0x00, 0x00, 0x10 }; }

            private void SetConfig()
            {
                // Full on/off bits
                bool FullOn = (this.Enabled && this.DutyCycle == 1); // Full on only if we are not disabled, and duty cycle is 100%.
                bool FullOff = (!this.Enabled); // Full off only if we are disabled
                this.Config[1] = (byte)(FullOn ? (this.Config[1] | 0b0001_0000) : (this.Config[1] & 0b1110_1111)); // Sets full on enable bit
                this.Config[3] = (byte)(FullOff ? (this.Config[3] | 0b0001_0000) : (this.Config[3] & 0b1110_1111)); // Sets full off enable bit

                // On/off counts
                if (!FullOn && !FullOff) // No point setting timings if they aren't going to be used
                {
                    ushort DelayTicks = (ushort)(Math.Max((this.Delay * 4096) - 1, 0)); // The time when the state should be asserted.
                    ushort OnTicks = (ushort)(Math.Max((this.DutyCycle * 4096) - 1, 0)); // For how many ticks the output should be on.
                    ushort OffTime = (ushort)((DelayTicks + OnTicks) % 4096); // The time when the state should be negated.

                    if (!this.Polarity)
                    {
                        this.Config[1] = (byte)((this.Config[1] & 0b1111_0000) | ((DelayTicks >> 8) & 0b1111)); // ON_MSB
                        this.Config[0] = (byte)(DelayTicks & 0b1111_1111); // ON_LSB

                        this.Config[3] = (byte)((this.Config[3] & 0b1111_0000) | ((OffTime >> 8) & 0b1111)); // OFF_MSB
                        this.Config[2] = (byte)(OffTime & 0b1111_1111); // OFF_LSB
                    }
                    else
                    {
                        this.Config[1] = (byte)((this.Config[1] & 0b1111_0000) | ((OffTime >> 8) & 0b1111)); // ON_MSB
                        this.Config[0] = (byte)(OffTime & 0b1111_1111); // ON_LSB

                        this.Config[3] = (byte)((this.Config[3] & 0b1111_0000) | ((DelayTicks >> 8) & 0b1111)); // OFF_MSB
                        this.Config[2] = (byte)(DelayTicks & 0b1111_1111); // OFF_LSB
                    }
                }

                this.Parent.SetChannelData(this.Channel, this.Config);
            }

            public void Dispose() { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Determines if the output pins are inverted from regular logic. Common to all channels.
        /// Regular: The pin is LOW when not being driven, and HIGH when being driven.
        /// Inverted: The pin is HIGH when not being driven, and LOW when being driven. Useful if attached to some inverting secondary driver.
        /// </summary>
        public enum OutputInvert { Regular = 0, Inverted = 1 }

        /// <summary> Determines how output pins are driven. Common to all channels. </summary>
        public enum OutputDriverMode { OpenDrain = 0, TotemPole = 1 }

        /// <summary>
        /// Determines how the outputs will behave when the ~OE physical pin is brought HIGH (outputs disabled).
        /// Low: The pin is set to LOW
        /// DriverDependent:
        /// -   OutputDriverMode.TotemPole: The pin is set LOW
        /// -   OutputDriverMode.OpenDrain: The pin is in high-impedance mode
        /// HighImpedance: The pin is in high-impedance mode
        /// </summary>
        public enum OutputDisableBehaviour { Low = 0, DriverDependent = 1, HighImpedance = 2 }

        public PWMOutputPCA9685[] Outputs { get; private set; }
        private PWMOutputPCA9685 AllOutputs;
        private II2CBus Bus;
        private byte PartAddress;
        private int ExtOscFreq;

        private OutputInvert InvertMode;
        private OutputDriverMode DriverMode;
        private OutputDisableBehaviour DisableMode;

        private const byte Mode1Register = 0x00;
        private const byte Mode2Register = 0x01;
        private const byte FirstLEDRegister = 0x06;
        private const byte AllLEDRegister = 0xFA;
        private const byte PrescaleRegister = 0xFE;

        // TODO: Implement the following:
        // - Subaddress set (and use)
        // - All call set (and use)
        // - Software reset

        /// <summary> Prepares the PCA9685 device for use. </summary>
        /// <param name="Bus"> The I2C bus that the devie will communicate over. </param>
        /// <remarks> During setup, there will be quite a bit of communication with the device (70+ bytes). </remarks>
        /// <param name="Address"> The I2C address set via the device's physical address pins. </param>
        /// <param name="ExtOscFreq"> If there is an external oscillator, set the frequency here. If there isn't, set to -1. </param>
        /// <exception cref="ArgumentException"> If invalid oscillator frequency is provided. </exception>
        public PCA9685(II2CBus Bus, byte Address, int ExtOscFreq = -1, OutputInvert InvertMode = OutputInvert.Regular, OutputDriverMode DriverMode = OutputDriverMode.TotemPole, OutputDisableBehaviour DisableMode = OutputDisableBehaviour.Low)
        {
            this.Bus = Bus;
            this.PartAddress = Address;
            this.ExtOscFreq = ExtOscFreq;
            if (ExtOscFreq < -1 || ExtOscFreq > 50000000) { throw new ArgumentException("Invalid oscillator frequency supplied. Must be between 0 and 50 MHz, or -1 for internal oscillator."); }
            this.Outputs = new PWMOutputPCA9685[16];
            for (byte i = 0; i < this.Outputs.Length; i++) { this.Outputs[i] = new PWMOutputPCA9685(i, this); }
            this.AllOutputs = new PWMOutputPCA9685(255, this);
            this.InvertMode = InvertMode;
            this.DriverMode = DriverMode;
            this.DisableMode = DisableMode;
            SetupDevice();
            ReadAllStates();
        }

        private void SetupDevice()
        {
            // Enable register auto-increment to make reads/writes much faster
            byte ModeSettingPre = this.Bus.ReadRegister(this.PartAddress, Mode1Register, 1)[0];
            Log.Output(Log.Severity.DEBUG, Log.Source.HARDWAREIO, "PCA9685 mode register pre: 0x" + ModeSettingPre.ToString("X1"));
            byte ModeSettingNew = (byte)((ModeSettingPre & 0b0111_1111) | 0b0010_0000); // Set bit 5 (AI) to 1 to enable auto-increment, but don't set bit 7 (RESET).
            this.Bus.WriteRegister(this.PartAddress, Mode1Register, new byte[] { ModeSettingNew });

            // Set the output modes
            byte Mode2 = 0b0000_0000;
            Mode2 |= (byte)(((byte)this.InvertMode & 0b1) << 4);
            Mode2 |= (byte)(((byte)this.DriverMode & 0b1) << 2);
            Mode2 |= (byte)((byte)this.DisableMode & 0b11);
            this.Bus.WriteRegister(this.PartAddress, Mode2Register, new byte[] { Mode2 });
        }

        internal void ReadAllStates()
        {
            byte[] OutputData = this.Bus.ReadRegister(this.PartAddress, FirstLEDRegister, 64);
            Log.Output(Log.Severity.DEBUG, Log.Source.HARDWAREIO, "PCA9685 states: " + UtilMain.BytesToNiceString(OutputData, true));
            if (OutputData == null || OutputData.Length != 64) { throw new Exception("Reading PCA9685 output state data did not return the correct amount of bytes (64)."); }
            for (int i = 0; i < 16; i++)
            {
                byte[] ChannelData = new byte[] { OutputData[i * 4 + 0], OutputData[i * 4 + 1], OutputData[i * 4 + 2], OutputData[i * 4 + 3] };
                this.Outputs[i].Config = ChannelData;
            }
        }

        /// <summary> Sets all channel's enabled status at once (single bus transaction). </summary>
        public void SetEnabledAll(bool Enable) => this.AllOutputs.SetEnabled(Enable);

        /// <summary> Sets all channel's duty cycle at once (single bus transaction). </summary>
        public void SetOutputAll(float DutyCycle) => this.AllOutputs.SetOutput(DutyCycle);

        /// <summary> Sets the device's PWM output frequency (common to all channels). Note that outputs will stop working briefly during this process. </summary>
        /// <remarks> The range for this depends on the external oscillator frequency. If the internal oscillator is used, the range is approximately 24 - 1526 Hz. </remarks>
        /// <param name="Frequency"> The new PWM frequency to use. If the given value is not possible, it will be set to the min/max allowable as needed. </param>
        public void SetFrequency(float Frequency)
        {
            // 25 MHz internal oscillator
            int Oscillator = (this.ExtOscFreq == -1 ? 25000000 : this.ExtOscFreq);
            int TempPrescale = (int)Math.Round(Oscillator / (4096 * Frequency)) - 1;
            if(TempPrescale < 3) { TempPrescale = 3; }
            if(TempPrescale > 255) { TempPrescale = 255; }
            byte PrescaleVal = (byte)(TempPrescale);
            Log.Output(Log.Severity.DEBUG, Log.Source.HARDWAREIO, "Setting PCA9685 frequency prescaler value to " + PrescaleVal + ".");

            // Put the oscillator in sleep mode.
            byte ModeSettingPre = this.Bus.ReadRegister(this.PartAddress, Mode1Register, 1)[0];
            byte ModeSettingNew = (byte)((ModeSettingPre & 0b0111_1111) | 0b0001_0000); // Set bit 4 (SLEEP) to 1 to shut down the oscillator, and also make sure we're not setting bit 7 (RESET).
            this.Bus.WriteRegister(this.PartAddress, Mode1Register, new byte[] { ModeSettingNew });

            // Set the frequency prescaler register.
            this.Bus.WriteRegister(this.PartAddress, PrescaleRegister, new byte[] { PrescaleVal });

            //TODO Disabled
            // Set the SLEEP bit back to what it was.
            this.Bus.WriteRegister(this.PartAddress, Mode1Register, new byte[] { (byte)(ModeSettingPre & 0b0110_1111) }); // Make sure we don't set bit 7 (RESET).
            Thread.Sleep(1); // 0.5ms minimum
            Log.Output(Log.Severity.DEBUG, Log.Source.HARDWAREIO, "PCA9685 mode now: 0x" + this.Bus.ReadRegister(this.PartAddress, (byte)Mode1Register, 1)[0].ToString("X1"));
            // If SLEEP was previously 0, we may need to RESTART.
            if ((ModeSettingPre & 0b0001_0000) == 0b0001_0000)
            {
                byte AfterWake = this.Bus.ReadRegister(this.PartAddress, Mode1Register, 1)[0];
                if((AfterWake & 0b1000_0000) == 0b1000_0000) // We need to RESTART.
                {
                    Log.Output(Log.Severity.DEBUG, Log.Source.HARDWAREIO, "Rebooting PCA9685.");
                    this.Bus.WriteRegister(this.PartAddress, Mode1Register, new byte[] { (byte)(AfterWake & 0b1000_0000) });
                    Thread.Sleep(10);
                    SetupDevice();
                }
            }
        }

        /// <summary> Switches the PCA9685 to use the external oscillator. This can only be disabled via a power cycle or software reset. </summary>
        /// <remarks> External oscillator must be between 0 and 50MHz. </remarks>
        /// <param name="Frequency"> This is used in software to determine some timing values. Provide the actual frequency of the attached oscillator. </param>
        public void UseExternalOscillator(int Frequency)
        {
            if (Frequency < 0 || Frequency > 50000000) { throw new Exception("External oscillator must be between 0 and 50 MHz. Switch not made."); }
            this.ExtOscFreq = Frequency;

            // Put the internal oscillator into sleep mode.
            byte ModeSettingPre = this.Bus.ReadRegister(this.PartAddress, Mode1Register, 1)[0];
            byte ModeSettingNew = (byte)((ModeSettingPre & 0b0111_1111) | 0b0001_0000); // Set bit 4 (SLEEP) to 1 to shut down the oscillator, and also make sure we're not setting bit 7 (RESET).
            this.Bus.WriteRegister(this.PartAddress, Mode1Register, new byte[] { ModeSettingNew });

            // Now switch to EXTCLK.
            byte ModeSettingPreExt = this.Bus.ReadRegister(this.PartAddress, Mode1Register, 1)[0];
            byte ModeSettingNewExt = (byte)((ModeSettingPreExt & 0b0111_1111) | 0b0101_0000); // Set bits 6 and 4 (EXTCLK & SLEEP), but not bit 7 (SLEEP).
            this.Bus.WriteRegister(this.PartAddress, Mode1Register, new byte[] { ModeSettingNewExt });
            Thread.Sleep(1); // Probably not needed, but may as well.

        }

        internal void SetChannelData(byte Channel, byte[] Data)
        {
            if (Data == null || Data.Length != 4) { throw new Exception("Invalid data being set for channel " + Channel); }
            byte Register;
            if (Channel == 255) { Register = AllLEDRegister; }
            else { Register = (byte)((Channel * 4) + FirstLEDRegister); } // Single LED channel
            this.Bus.WriteRegister(this.PartAddress, Register, Data);
        }

        ~PCA9685()
        {
            try
            {
                this.Bus.WriteRegister(this.PartAddress, 0x00, new byte[] { 0x11 }); // Attempts to put this device into SLEEP mode to shut down outputs.
            }
            catch { } // Meh, oh well.
        }
    }
}
