using System;

namespace Scarlet.IO
{
	public interface ICANBus
	{
		/// <summary>
		/// Write a payload with specified ID.
		/// </summary>
		/// <param name="ID">ID of CAN Frame</param>
		/// <param name="Data">Payload of CAN Frame. Must be at most 8 bytes.</param>
		void Write(uint Address, byte[] Data);

		/// <summary>
		/// Blocks the current thread and reads a CAN frame, returning the payload and the addres of the received CAN frame. 
		/// </summary>
		/// <returns>A tuple, with the first element being the ID of the received CAN frame and the second being the payload</returns>
		Tuple<uint, byte[]> Read();

		/// <summary> Cleans up the bus object, freeing resources. </summary>
		void Dispose();
	}
}
