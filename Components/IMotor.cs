using System;

namespace Scarlet.Components
{
    public interface IMotor
    {
        /// <summary> Changes the speed of the motor, subject to filtering and capping. </summary>
        void SetSpeed(float Speed);

        /// <summary> Enables or disables the motor immediately. </summary>
        void SetEnabled(bool Enabled);
    }
}