﻿using System;
using Scarlet.Filters;
using Scarlet.IO;
using Scarlet.Utilities;
using System.Threading;

namespace Scarlet.Components.Motors
{
    public class TalonMC : IMotor
    {
        private IFilter<float> Filter; // Filter for speed output
        private readonly IPWMOutput PWMOut;
        private readonly float MaxSpeed; 

        private bool OngoingSpeedThread; // Whether or not a thread is running to set the speed
        private bool Stopped; // Whether or not the motor is stopped
        public float TargetSpeed { get; private set; } // Target speed (-1.0 to 1.0) of the motor

        /// <summary> Initializes a Talon Motor controller </summary>
        /// <param name="PWMOut"> PWM output to control the motor controller </param>
        /// <param name="MaxSpeed"> Limiting factor for speed (should never exceed + or - this val) </param>
        /// <param name="SpeedFilter"> Filter to use with MC. Good for ramp-up protection and other applications </param>
        public TalonMC(IPWMOutput PWMOut, float MaxSpeed, IFilter<float> SpeedFilter = null)
        {
            this.PWMOut = PWMOut;
            this.MaxSpeed = Math.Abs(MaxSpeed);
            this.Filter = SpeedFilter;
            this.PWMOut.SetFrequency(333);
            this.PWMOut.SetEnabled(true);
            this.SetSpeedDirectly(0.0f);
        }

        public void EventTriggered(object Sender, EventArgs Event) { }

        /// <summary> 
        /// Immediately sets the enabled status of the motor. 
        /// If false, resets TargetSpeed to 0 and stops the motor.
        /// </summary>
        public void SetEnabled(bool Enabled)
        {
            this.Stopped = Enabled;
            if (!Enabled) { this.SetSpeed(0); }
        }

        /// <summary> Sets the speed on a thread for filtering. </summary>
        private void SetSpeedThread()
        {
            float Output = this.Filter.GetOutput();
            while(!this.Filter.IsSteadyState())
            {
                this.Filter.Feed(this.TargetSpeed);
                SetSpeedDirectly(this.Filter.GetOutput());
                Thread.Sleep(Constants.DEFAULT_MIN_THREAD_SLEEP);
            }
            OngoingSpeedThread = false;
        }

        /// <summary> Creates a new thread for setting speed during motor filtering output </summary>
        /// <returns> A new thread for changing the motor speed. </returns>
        private Thread SetSpeedThreadFactory() { return new Thread(new ThreadStart(SetSpeedThread)); }

        /// <summary>
        /// Sets the motor speed. Output may vary from the given value under the following conditions:
        /// - Input exceeds maximum speed. Capped to given maximum.
        /// - Filter changes value. Filter's output used instead.
        ///     (If filter is null, this does not occur)
        /// - The motor is disabled. You must first re-enable the motor.
        /// </summary>
        /// <param name="Speed"> The new speed to set the motor at. From -1.0 to 1.0 </param>
        public void SetSpeed(float Speed)
        {
            if (this.Filter != null)
            {
                this.Filter.Feed(Speed);
                if (!this.Filter.IsSteadyState() && !OngoingSpeedThread)
                {
                    SetSpeedThreadFactory().Start();
                    OngoingSpeedThread = true;
                }
            }
            else { SetSpeedDirectly(Speed); }
            this.TargetSpeed = Speed;
        }

        /// <summary>
        /// Sets the speed directly given an input from -1.0 to 1.0
        /// Takes into consideration motor stop signal and max speed restriction.
        /// </summary>
        /// <param name="Speed"> Speed from -1.0 to 1.0 </param>
        private void SetSpeedDirectly(float Speed)
        {
            if (Speed > this.MaxSpeed) { Speed = this.MaxSpeed; }
            if (Speed * -1 > this.MaxSpeed) { Speed = -1 * this.MaxSpeed; }
            if (this.Stopped) { Speed = 0; }
            this.PWMOut.SetOutput((Speed / 2) + 0.5F);
        }
    }
}
