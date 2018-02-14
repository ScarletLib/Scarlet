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
            internal byte[] Config { private get; set; }

            internal PWMOutputPCA9685(byte Channel, PCA9685 Parent)
            {
                this.Channel = Channel;
                this.Parent = Parent;
            }

            public void SetEnabled(bool Enable)
            {
                throw new NotImplementedException();
            }

            public void SetFrequency(int Frequency)
            {
                throw new NotImplementedException();
            }

            public void SetOutput(float DutyCycle)
            {
                throw new NotImplementedException();
            }

            private void UpdateConfig()
            {
                //this.Parent.SetChannelData(this.Channel, null);
            }

            public void Dispose() { throw new NotImplementedException(); }
        }

        public PWMOutputPCA9685[] Outputs { get; private set; }
        private II2CBus Bus;
        private byte PartAddress;

        private const byte FirstLEDRegisterOffset = 0x06;

        public PCA9685(II2CBus Bus, byte Address)
        {
            this.Bus = Bus;
            this.PartAddress = Address;
            this.Outputs = new PWMOutputPCA9685[16];
            for (byte i = 0; i < this.Outputs.Length; i++) { this.Outputs[i] = new PWMOutputPCA9685(i, this); }
            ReadAllStates();
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

        internal void SetChannelData(int Channel, byte[] Data)
        {

        }
    }
}
