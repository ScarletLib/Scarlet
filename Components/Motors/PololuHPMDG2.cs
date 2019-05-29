using System;
using System.Threading;
using Scarlet.Filters;
using Scarlet.IO;
using Scarlet.Utilities;

namespace Scarlet.Components.Motors
{
    /// <summary>
    /// Pololu High-Power Motor Driver G2
    /// Used for this product:
    /// https://www.pololu.com/product/2994/
    /// Other variations of device not tested and may not be supported.
    /// </summary>
    public class PololuHPMDG2 : IMotor
    {
        private readonly IFilter<float> Filter; // Filter for speed output
        private readonly IPWMOutput PWMOut;
        private readonly float MaxSpeed;
        private readonly IDigitalOut Direction;

        /// <summary> Sleep pin. (Active Low) </summary>
        private readonly IDigitalOut SleepPin;
        private readonly IDigitalIn FaultPin;

        private bool SleepHelper;
        private bool Stopped; // Whether or not the motor is stopped
        private bool OngoingSpeedThread; // Whether or not a thread is running to set the speed

        /// <summary> 
        /// Gets the target speed of the motor. 
        /// If the motor output is filtered, this speed may not be the current PWM output. 
        /// </summary>
        public float TargetSpeed { get; private set; } // Target speed (-1.0 to 1.0) of the motor

        /// <summary> Gets or sets a value indicating whether to log detailed info about this object to the console for debugging. </summary>
        public bool TraceLogging { get; set; }

        /// <summary> 
        /// Gets or sets a value indicating whether motor will be disabled if a Fault is detected. Must be re-enabled by <see cref="SetEnabled(bool)"/>. 
        /// Ignored if fault pin not set in constructor.
        /// </summary>
        public bool StopOnFault { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating the sleep state of the motor.
        /// No effect if Sleep pin not set in constructor.
        /// </summary>
        public bool Sleep
        {
            get => this.SleepHelper;
            set
            {
                this.SleepHelper = value;
                this.SleepPin?.SetOutput(!this.SleepHelper);
            }
        }

        /// <summary> Gets a value indicating whether or not the motor is in a fault state. </summary>
        public bool Fault { get { return this.FaultPin != null ? this.FaultPin.GetInput() : false; } }

        /// <summary> Initializes a Pololu HPMDG2 Motor controller </summary>
        /// <param name="PWMOut"> PWM Output to set the speed of the motor controller </param>
        /// <param name="DirectionPin"> GPIO Output that sets the direction of the motor </param>
        /// <param name="MaxSpeed"> Limiting factor for speed (should never exceed + or - this val) </param>
        /// <param name="PWMFrequency"> Frequency of PWM Output </param>
        /// <param name="SleepPin"> Output for sleep pin. Can be null if unused. </param>
        /// <param name="FaultPin"> Input for fault pin. Can be null if unused. </param>
        /// <param name="SpeedFilter"> Filter to use with MC. Good for ramp-up protection and other applications </param>
        public PololuHPMDG2(IPWMOutput PWMOut, IDigitalOut DirectionPin, float MaxSpeed, int PWMFrequency = 20000, IDigitalOut SleepPin = null, IDigitalIn FaultPin = null, IFilter<float> SpeedFilter = null)
        {
            this.PWMOut = PWMOut;
            this.MaxSpeed = Math.Abs(MaxSpeed);
            this.Filter = SpeedFilter;
            this.PWMOut.SetFrequency(PWMFrequency);
            this.PWMOut.SetEnabled(true);
            this.Direction = DirectionPin;
            this.Direction.SetOutput(false);
            this.FaultPin = FaultPin;
            this.SleepPin = SleepPin;

            // Set sleep false by default
            this.Sleep = false;
            SetSpeedDirectly(0.0f);
        }

        /// <summary> Immediately sets the enabled status of the motor. </summary>
        /// <param name="Enabled"> If false, resets TargetSpeed to 0 and stops the motor. </param>
        public void SetEnabled(bool Enabled)
        {
            this.Stopped = !Enabled;
            if (!Enabled)
            {
                this.TargetSpeed = 0;
                SetSpeedDirectly(0);
            }
        }

        /// <summary> Sets the speed on a thread for filtering. </summary>
        private void SetSpeedThread()
        {
            float Output = this.Filter.GetOutput();
            while (!this.Filter.IsSteadyState())
            {
                if (this.Stopped) { SetSpeedDirectly(0); }
                else
                {
                    this.Filter.Feed(this.TargetSpeed);
                    SetSpeedDirectly(this.Filter.GetOutput());
                }
                Thread.Sleep(Constants.DEFAULT_MIN_THREAD_SLEEP);
            }
            this.OngoingSpeedThread = false;
        }

        /// <summary>
        /// Sets the motor speed. Output may vary from the given value under the following conditions:
        /// - Input exceeds maximum speed. Capped to given maximum.
        /// - Filter changes value. Filter's output used instead.
        ///     (If filter is null, this does not occur)
        /// - The motor is disabled. You must first re-enable the motor.
        /// </summary>
        /// <param name="Speed"> The new speed to set the motor at. From -1.0 to 1.0. </param>
        public void SetSpeed(float Speed)
        {
            if (this.Filter != null && !this.Filter.IsSteadyState() && !OngoingSpeedThread)
            {
                this.Filter.Feed(Speed);
                SetSpeedThreadFactory().Start();
                OngoingSpeedThread = true;
            }
            else { SetSpeedDirectly(Speed); }
            this.TargetSpeed = Speed;
        }

        /// <summary> Creates a new thread for setting speed during motor filtering output </summary>
        /// <returns> A new thread for changing the motor speed. </returns>
        private Thread SetSpeedThreadFactory() { return new Thread(new ThreadStart(SetSpeedThread)); }

        /// <summary>
        /// Sets the speed directly given an input from -1.0 to 1.0
        /// Takes into consideration motor stop signal and max speed restriction.
        /// </summary>
        /// <param name="Speed"> Speed from -1.0 to 1.0 </param>
        private void SetSpeedDirectly(float Speed)
        {
            // Check fault conditions
            if (this.StopOnFault && this.Fault) { SetEnabled(false); }
            if (Speed > this.MaxSpeed) { Speed = this.MaxSpeed; }
            if (Speed * -1 > this.MaxSpeed) { Speed = -1 * this.MaxSpeed; }
            if (this.Stopped) { Speed = 0; }
            this.PWMOut.SetOutput(Math.Abs(Speed));
            this.Direction.SetOutput(Math.Sign(Speed) < 0);
        }
    }
}
