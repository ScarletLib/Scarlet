using System;
using Scarlet.Filters;
using Scarlet.IO;
using Scarlet.Utilities;
using System.Threading;
using System.Collections.Generic;

namespace Scarlet.Components.Motors
{
	public class VESC : IMotor
	{
		private IFilter<float> Filter; // Filter for speed output
		private readonly IUARTBus UARTBus;
		private readonly ICANBus CANBus;
		private readonly int CANForwardID;
		private readonly uint CANID;
		private readonly float MaxSpeed;
		private readonly bool IsCAN;

		private bool OngoingSpeedThread; // Whether or not a thread is running to set the speed
		private bool Stopped; // Whether or not the motor is stopped
		public float TargetSpeed { get; private set; } // Target speed (-1.0 to 1.0) of the motor

		/// <summary> Initializes a VESC Motor controller </summary>
		/// <param name="UARTBus"> UART output to control the motor controller </param>
		/// <param name="MaxSpeed"> Limiting factor for speed (should never exceed + or - this val) </param>
		/// <param name="CANForwardID"> CAN ID of the motor controller (-1 to disable CAN forwarding) </param>
		/// <param name="SpeedFilter"> Filter to use with MC. Good for ramp-up protection and other applications </param>
		public VESC(IUARTBus UARTBus, float MaxSpeed, int CANForwardID = -1, IFilter<float> SpeedFilter = null)
		{
			IsCAN = false;
			this.UARTBus = UARTBus;
			this.UARTBus.BaudRate = UARTRate.BAUD_115200;
			this.UARTBus.BitLength = UARTBitCount.BITS_8;
			this.UARTBus.StopBits = UARTStopBits.STOPBITS_1;
			this.UARTBus.Parity = UARTParity.PARITY_NONE;
			this.CANForwardID = CANForwardID;
			this.MaxSpeed = Math.Abs(MaxSpeed);
			this.Filter = SpeedFilter;
			this.SetSpeedDirectly(0.0f);
		}

		public VESC(ICANBus CANBus, float MaxSpeed, uint CANID, IFilter<float> SpeedFilter = null)
		{
			IsCAN = true;
			this.CANBus = CANBus;
			this.MaxSpeed = Math.Abs(MaxSpeed);
			this.CANID = CANID;
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
			this.SendSpeed(Speed);
		}

		/// <summary>
		/// Sends the speed between -1.0 and 1.0 to the motor controller
		/// </summary>
		/// <param name="Speed"> Speed from -1.0 to 1.0 </param>
		private void SendSpeed(float Speed)
		{
			byte[] SpeedArray = UtilData.ToBytes((int)(Speed * 100000.0f));
			if (this.IsCAN) { this.CANBus.Write(this.CANID, SpeedArray); }
			else
			{
				List<byte> payload = new List<byte>();
				payload.Add((byte)PacketID.SET_DUTY);
				payload.AddRange(SpeedArray);
				// Duty Cycle (100000.0 mysterious magic number from https://github.com/VTAstrobotics/VESC_BBB_UART/blob/master/bldc_interface.c)
				this.UARTBus.Write(ConstructPacket(payload));
			}
		}

		/// <summary> Generates the packet for the motor controller: </summary>
		/// <remarks>
		/// One Start byte (value 2 for short packets and 3 for long packets)
		/// One or two bytes specifying the packet length
		/// The payload of the packet
		/// Two bytes with a CRC checksum on the payload
		/// One stop byte (value 3)
		/// </remarks>
		/// <param name="Speed"> Speed from -1.0 to 1.0 </param>
		private byte[] ConstructPacket(List<byte> Payload)
		{
			List<byte> Packet = new List<byte>();

			Packet.Add(2); // Start byte (short packet - payload <= 256 bytes)

			if (this.CANForwardID >= 0)
			{
				Payload.Add((byte)PacketID.FORWARD_CAN);
				Payload.Add((byte)CANForwardID);
			}

			Packet.Add((byte)Payload.Count); // Length of payload
			Packet.AddRange(Payload); // Payload

			ushort Checksum = UtilData.CRC16(Payload.ToArray());
			Packet.AddRange(UtilData.ToBytes(Checksum)); // Checksum

			Packet.Add(3); // Stop byte

			return Packet.ToArray();
		}

		#region enums
		private enum PacketID : byte
		{
			// Full enum list here: https://github.com/vedderb/bldc_uart_comm_stm32f4_discovery/blob/master/datatypes.h
			FW_VERSION = 0,
			GET_VALUES = 4,
			SET_DUTY = 5,
			SET_CURRENT = 6,
			SET_CURRENT_BRAKE = 7,
			SET_RPM = 8,
			SET_POS = 9,
			SET_DETECT = 10,
			REBOOT = 28,
			ALIVE = 29,
			FORWARD_CAN = 33
		}
		#endregion
	}

}