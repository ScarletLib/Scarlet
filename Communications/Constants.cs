namespace Scarlet.Communications
{

    static class Constants
    {
        #region Communication Defaults
        public const int WATCHDOG_WAIT = 5000;  // ms
        public const int WATCHDOG_INTERVAL = 1000; // ms
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

}
