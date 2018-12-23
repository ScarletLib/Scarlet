using System;
using System.Collections.Generic;
using Scarlet.Utilities;

namespace Scarlet.Communications
{
    /// <summary> Handles packet parsing, using handlers of incoming message IDs. </summary>
    public static class Parse
    {
        // Delegate method type for parsing specific packet IDs
        public delegate void ParseMethod(Packet Packet);
        // Stored parsing Handlers for all possible message IDs
        static readonly Dictionary<byte, Delegate> ParsingHandlers = new Dictionary<byte, Delegate>();

        /// <summary> Sets the handler for parsing of the appropriate Message ID. </summary>
        /// <param name="MessageID"> Message ID for parsing. </param>
        /// <param name="ParseMethod"> Method used when incoming packet of <c>MessageID</c> is received. </param>
        public static void SetParseHandler(byte MessageID, ParseMethod ParseMethod)
        {
            if (Client.TraceLogging || Server.TraceLogging) { Log.Trace(typeof(Parse), "Adding parse handler for ID 0x" + MessageID.ToString("X2")); }
            if (ParsingHandlers.ContainsKey(MessageID))
            {
                Log.Output(Log.Severity.WARNING, Log.Source.NETWORK, "Parse Method for Packet ID 0x" + MessageID.ToString("X4") + " overridden.");
            }
            ParsingHandlers[MessageID] = ParseMethod;
        }

        /// <summary> Appropriately parses incoming message. </summary>
        /// <param name="NewMessage"> Message to parse. </param>
        /// <returns> Whether or not parsing was successful. </returns>
        internal static bool ParseMessage(Packet Packet)
        {
            try
            {
                // if(Packet.Data.ID != Constants.WATCHDOG_PING || Server.OutputWatchdogDebug) { Log.Output(Log.Severity.DEBUG, Log.Source.NETWORK, "Parsing packet: " + Packet.Data.ToString()); }
                if (!ParsingHandlers.ContainsKey(Packet.ID))
                {
                    Log.Output(Log.Severity.ERROR, Log.Source.NETWORK, "No handler is registered for parsing packet ID " + Packet.ID + "!");
                    return false;
                }
                ParsingHandlers[Packet.ID].DynamicInvoke(Packet);
                return true;
            }
            catch (Exception Except)
            {
                Log.Output(Log.Severity.ERROR, Log.Source.NETWORK, "Failed to invoke handler for incoming message.");
                Log.Exception(Log.Source.NETWORK, Except);
                return false;
            }
        }

    }

    internal static class InternalParsing
    {
        public static void Start()
        {
            Parse.SetParseHandler(Constants.WATCHDOG_FROM_CLIENT, ParseWatchdogFromClient);
            Parse.SetParseHandler(Constants.WATCHDOG_FROM_SERVER, ParseWatchdogFromServer);
            Parse.SetParseHandler(Constants.HANDSHAKE_FROM_CLIENT, ParseClientHandshake);
            Parse.SetParseHandler(Constants.HANDSHAKE_FROM_SERVER, ParseServerHandshake);
            Parse.SetParseHandler(Constants.TIME_SYNCHRONIZATION, ParseTimeSynchronization);
            Parse.SetParseHandler(Constants.BUFFER_LENGTH_CHANGE, ParseBufferLengthAdjustment);
        }

        #region Parse Handler Delegates

        public static void ParseWatchdogFromServer(Packet Watchdog) { Client.HandleWatchdog(Watchdog); }

        public static void ParseWatchdogFromClient(Packet Watchdog) { Server.ReceiveWatchdog(Watchdog); }

        public static void ParseClientHandshake(Packet ClientHandshake) { } // TODO: Handle unexpected handshakes.

        public static void ParseServerHandshake(Packet ServerHandshake) { Client.ReceiveHandshake(ServerHandshake); }

        public static void ParseTimeSynchronization(Packet TimeSync) { } // TODO: Implement time synchronization.

        public static void ParseBufferLengthAdjustment(Packet BufferAdjustment) { } // TODO: Implement dynamic buffer resizing.

        #endregion
    }
}
