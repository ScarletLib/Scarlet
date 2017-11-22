namespace Scarlet.Communications
{
    /// <summary>
    /// Represents the priority of packet, from highest to lowest.
    /// </summary>
    public enum PacketPriority
    {
        USE_DEFAULT = -1,
        EMERGENT = 0,
        HIGH,
        MEDIUM,
        LOW,
        LOWEST
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
