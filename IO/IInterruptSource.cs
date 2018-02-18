using System;

namespace Scarlet.IO
{
    public interface IInterruptSource
    {
        /// <summary> Registers a new interrupt handler, asking for the specificed type of interrupt. </summary>
        void RegisterInterruptHandler(EventHandler<InputInterrupt> Handler, InterruptType Type);
    }
}
