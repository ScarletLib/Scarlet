﻿namespace Scarlet.IO.RaspberryPi
{
    public class DigitalOutPi : IDigitalOut
    {
        public int PinNumber { get; private set; }

        public DigitalOutPi(int PinNumber)
        {
            this.PinNumber = PinNumber;
            RaspberryPi.SetPinMode(this.PinNumber, RaspberryPi.PinMode.OUTPUT);
        }

        /// <summary> Sets the logic-level ouput. </summary>
        public void SetOutput(bool Output) { RaspberryPi.DigitalWrite(this.PinNumber, Output); }

        /// <summary> Does nothing currently. You should still call this when finished in case it does gain functionality later on. </summary>
        public void Dispose()
        {
            // TODO: Do we need to do anything here?
        }
    }
}
