namespace Scarlet.Communications
{
    /// <summary> Represents data type. </summary>
    public enum TypeID
    {
        BOOL = 0,
        CHAR = 1,
        DOUBLE = 2,
        FLOAT = 3,
        INT = 4,
        LONG = 5,
        SHORT = 6,
        UINT = 7,
        ULONG = 8,
        USHORT = 9,
        BYTE = 10,
        STRING = 11,
        BYTES = 12,
        MAX = 13,
    }

    /// <summary> Represents the priority of packet, from highest to lowest. </summary>
    public enum PacketPriority
    {
        USE_DEFAULT = -1,
        EMERGENT = 0,
        HIGH = 1,
        MEDIUM = 2,
        LOW = 3,
        LOWEST = 4
    }

    static class Constants
    {
        #region Communication Defaults
        public const int WATCHDOG_WAIT = 5000;  // ms
        public const int WATCHDOG_INTERVAL = 1000; // ms
        #endregion

        #region Reserved Packet IDs
        public const int WATCHDOG_PING = 0xF0;
        #endregion

    }

}
