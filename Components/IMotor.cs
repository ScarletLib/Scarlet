using System;

namespace Scarlet.Components
{
    public interface IMotor
    {
        /// <summary> Changes the speed of the motor, subject to filtering and capping. </summary>
        void SetSpeed(float Speed);

        /// <summary> Stops to motor immediately. </summary>
        void Stop();

        /// <summary> Used to send events to Motors. </summary>
        void EventTriggered(object Sender, EventArgs Event);
    }
}