using System;
using System.Threading;

namespace Scarlet.IO.Utilities
{
    public class SoftwareInterrupt : IDigitalIn, IInterruptSource
    {
        private readonly IDigitalIn Input;
        private readonly int PollTime;
        private bool Continue;
        private bool PreviousState;

        private event EventHandler<InputInterrupt> InterruptAny, InterruptRising, InterruptFalling;

        /// <summary> Takes a non-interrupt capable <see cref="IDigitalIn"/> and does software-based interrupt detection. </summary>
        /// <remarks> Because this is done in software on a non-realtime OS, there is no guarantee that input events will be captured if they are too short. </remarks>
        /// <param name="Input"> The digital input to listen for interrupts on. </param>
        /// <param name="PollingTime"> The time in between checks for state changes, in milliseconds. </param>
        public SoftwareInterrupt(IDigitalIn Input, int PollingTime = 5)
        {
            if (PollingTime < 1) { throw new ArgumentException("PollingTime must be 1 ms or greater."); }
            this.PollTime = PollingTime;
            this.Input = Input;
            this.Continue = true;
            Thread StateCheck = new Thread(CheckForEvents);
        }

        public void RegisterInterruptHandler(EventHandler<InputInterrupt> Handler, InterruptType Type)
        {
            if (Handler == null) { throw new ArgumentNullException("Handler must not be null."); }
            switch (Type)
            {
                case InterruptType.ANY_EDGE: this.InterruptAny += Handler; break;
                case InterruptType.FALLING_EDGE: this.InterruptFalling += Handler; break;
                case InterruptType.RISING_EDGE: this.InterruptRising += Handler; break;
            }
        }

        public void SetResistor(ResistorState Resistor) => this.Input.SetResistor(Resistor);

        public bool GetInput() => this.Input.GetInput();

        public void Dispose()
        {
            this.Continue = false;
            this.Input.Dispose();
        }

        private void CheckForEvents()
        {
            while (this.Continue)
            {
                bool NewState = this.Input.GetInput();
                if (NewState != this.PreviousState) // Event happened.
                {
                    if (this.PreviousState && !NewState) // Falling
                    {
                        this.InterruptFalling?.Invoke(this, new InputInterrupt(NewState));
                        this.InterruptAny?.Invoke(this, new InputInterrupt(NewState));
                    }
                    else if (!this.PreviousState && NewState) // Rising
                    {
                        this.InterruptRising?.Invoke(this, new InputInterrupt(NewState));
                        this.InterruptAny?.Invoke(this, new InputInterrupt(NewState));
                    }
                }
                this.PreviousState = NewState;
                Thread.Sleep(this.PollTime);
            }
        }
    }
}
