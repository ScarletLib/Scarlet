using System;

namespace Scarlet.IO
{
	public interface ICANBus
	{
		// TODO: I have no idea if this is an appropriate implementation. This will likely changes as we actually add CAN functionality, and is just here as a skeleton.

		/// <summary> Writes data to the given device by address over the CAN bus. </summary>
		void Write(uint Address, byte[] Data);

		/// <summary> Reads the specified amount of data from the device by address. </summary>
		Tuple<uint, byte[]> Read();

		/// <summary> Cleans up the bus object, freeing resources. </summary>
		void Dispose();
	}
}
