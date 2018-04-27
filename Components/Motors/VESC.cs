using System;
using Scarlet.Filters;
using Scarlet.IO;
using Scarlet.Utilities;
using System.Threading;
using System.Collections.Generic;

namespace Scarlet.Components.Motors
{
    class VESC : IMotor
    {
        private IFilter<float> Filter; // Filter for speed output
        private readonly IUARTBus UARTBus;
        private readonly int CANForwardId;
        private readonly float MaxSpeed;

        private bool OngoingSpeedThread; // Whether or not a thread is running to set the speed
        private bool Stopped; // Whether or not the motor is stopped
        public float TargetSpeed { get; private set; } // Target speed (-1.0 to 1.0) of the motor

        /// <summary> Initializes a Talon Motor controller </summary>
        /// <param name="UARTBus"> UART output to control the motor controller </param>
        /// <param name="MaxSpeed"> Limiting factor for speed (should never exceed + or - this val) </param>
        /// <param name="CANForwardId"> CAN ID of the motor controller (-1 to disable CAN forwarding) </param>
        /// <param name="SpeedFilter"> Filter to use with MC. Good for ramp-up protection and other applications </param>
        public VESC(IUARTBus UARTBus, float MaxSpeed, int CANForwardId = -1, IFilter<float> SpeedFilter = null)
        {
            this.UARTBus = UARTBus;
            this.UARTBus.BaudRate = UARTRate.BAUD_115200;
            this.UARTBus.BitLength = UARTBitCount.BITS_8;
            this.UARTBus.StopBits = UARTStopBits.STOPBITS_1;
            this.UARTBus.Parity = UARTParity.PARITY_NONE;
            this.CANForwardId = CANForwardId;
            this.MaxSpeed = Math.Abs(MaxSpeed);
            this.Filter = SpeedFilter;
            this.SetSpeedDirectly(0.0f);
        }

        public void EventTriggered(object Sender, EventArgs Event) { }

        /// <summary> 
        /// Immediately sets the enabled status of the motor.
        /// Stops the motor if given parameter is false.
        /// Does not reset the target speed to zero, so beware
        /// of resetting this to enabled.
        /// </summary>
        public void SetEnabled(bool Enabled)
        {
            this.Stopped = !Enabled;
            if (Enabled) { this.SetSpeed(this.TargetSpeed); }
            else { this.SetSpeedDirectly(0); }
        }

        /// <summary> Sets the speed on a thread for filtering. </summary>
        private void SetSpeedThread()
        {
            float Output = this.Filter.GetOutput();
            while (!this.Filter.IsSteadyState())
            {
                if (Stopped) { SetSpeedDirectly(0); }
                else
                {
                    this.Filter.Feed(this.TargetSpeed);
                    SetSpeedDirectly(this.Filter.GetOutput());
                }
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
            if (this.Filter != null && !this.Filter.IsSteadyState() && !OngoingSpeedThread)
            {
                this.Filter.Feed(Speed);
                SetSpeedThreadFactory().Start();
                OngoingSpeedThread = true;
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
            if (-Speed > this.MaxSpeed) { Speed = -this.MaxSpeed; }
            if (this.Stopped) { Speed = 0; }
            UARTBus.Write(ConstructPacket(Speed));
        }


        /// <summary>
        /// Generates the packet for the motor controller:
        /// One Start byte (value 2 for short packets and 3 for long packets)
        /// One or two bytes specifying the packet length
        /// The payload of the packet
        /// Two bytes with a CRC checksum on the payload
        /// One stop byte (value 3)
        /// </summary>
        /// <param name="Speed"> Speed from -1.0 to 1.0 </param>
        private byte[] ConstructPacket(float Speed)
        {
            List<byte> packet = new List<byte>();
            List<byte> payload = new List<byte>();
            byte ShortPacket = 2;

            packet.Add(ShortPacket);
            
            if (CANForwardId >= 0)
            {
                payload.Add((byte) PacketId.FORWARD_CAN);
                payload.Add((byte) CANForwardId);
            }


            payload.Add((byte) PacketId.SET_DUTY);
            // Duty Cycle (100000 mysterious magic number from https://github.com/VTAstrobotics/VESC_BBB_UART/blob/master/bldc_interface.c)
            payload.AddRange(UtilData.ToBytes(Speed * 100000));

            packet.Add((byte) payload.Count); // Length of payload
            packet.AddRange(payload);

            ushort checksum = UtilData.Crc16.ComputeChecksum(payload.ToArray());
            packet.AddRange(UtilData.ToBytes(checksum));

            packet.Add(3); // Stop byte

            return packet.ToArray();
        }
        private enum PacketId : byte
        {
            FW_VERSION,
            JUMP_TO_BOOTLOADER,
            ERASE_NEW_APP,
            WRITE_NEW_APP_DATA,
            GET_VALUES,
            SET_DUTY,
            SET_CURRENT,
            SET_CURRENT_BRAKE,
            SET_RPM,
            SET_POS,
            SET_DETECT,
            SET_SERVO_POS,
            SET_MCCONF,
            GET_MCCONF,
            GET_MCCONF_DEFAULT,
            SET_APPCONF,
            GET_APPCONF,
            GET_APPCONF_DEFAULT,
            SAMPLE_PRINT,
            TERMINAL_CMD,
            PRINT,
            ROTOR_POSITION,
            EXPERIMENT_SAMPLE,
            DETECT_MOTOR_PARAM,
            DETECT_MOTOR_R_L,
            DETECT_MOTOR_FLUX_LINKAGE,
            DETECT_ENCODER,
            DETECT_HALL_FOC,
            REBOOT,
            ALIVE,
            GET_DECODED_PPM,
            GET_DECODED_ADC,
            GET_DECODED_CHUK,
            FORWARD_CAN,
            SET_CHUCK_DATA,
            CUSTOM_APP_DATA
        }
    }

}
