﻿using System;

namespace Scarlet.IO
{
    public interface IDigitalIn
    {
        /// <summary> Sets the input resistor. </summary>
        void SetResistor(ResistorState Resistor);

        /// <summary> Gets the current input state. </summary>
        bool GetInput();

        /// <summary> Releases handles to the pin, allowing it to be used by another component or application. </summary>
        void Dispose();
    }
}
