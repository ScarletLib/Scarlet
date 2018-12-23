namespace Scarlet.Communications
{

    internal static class Constants
    {
        #region Communication Defaults
        /// <summary> How long (in ms) client/server tolerate lack of watchdog packets before they attempt a re-connection. </summary>
        public const int WATCHDOG_WAIT = 5000;

        /// <summary> The interval (in ms) at which to send watchdog packets. </summary>
        public const int WATCHDOG_INTERVAL = 1000;

        /// <summary> How long (in ms) to wait before retrying a connection. </summary>
        public const int CONNECTION_RETRY_DELAY = 5000;

        /// <summary> How large (in bytes) the default send/receive buffers should be. </summary>
        public const int DEFAULT_RECEIVE_BUFFER_SIZE = 64;

        /// <summary> Defines how many bytes are in the header (non-data portion at the beginning) of all <see cref="Packet"/>s. </summary>
        public const int PACKET_HEADER_SIZE = sizeof(long) + sizeof(byte) + sizeof(ushort); // Timestamp + ID + Length
        public const int MIN_BUFFER_SIZE = PACKET_HEADER_SIZE + 4; // bytes (based on longest internal-use packets)
        #endregion

        #region Reserved Packet IDs
        public const byte WATCHDOG_FROM_SERVER = 0xF0;
        public const byte WATCHDOG_FROM_CLIENT = 0xF1;
        public const byte BUFFER_LENGTH_CHANGE = 0xF2;
        public const byte TIME_SYNCHRONIZATION = 0xF3;
        public const byte HANDSHAKE_FROM_CLIENT = 0xF4;
        public const byte HANDSHAKE_FROM_SERVER = 0xF5;
        #endregion
    }

    public enum ClientServerConnectionState : byte
    {
        OKAY = 0x00,
        INVALID_NAME = 0x01,
        INCOMPATIBLE_VERSIONS = 0x02,
        CONNECTION_FAILED = 0x03
    }

    public enum LatencyMeasurementMode : byte
    {
        NONE = 0x00,
        BASIC = 0x01,
        FULL = 0x02,
    }

}
