using Scarlet.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Scarlet.Communications
{
    /// <summary>
    /// 
    /// </summary>
    public static class Client
    {
        public static bool TraceLogging { get; set; }
        public static bool Initialized { get; private set; }

        public static string ClientName { get; private set; }

        public static int ReceiveBufferSize { get; private set; }
        public static int OperationPeriod { get; private set; }
        public static int PortUDP { get; private set; }
        public static int PortTCP { get; private set; }

        public static IPAddress ServerIP { get; private set; }

        public static event EventHandler<ConnectionStatusChanged> ConnectionStatusChanged;
        public static event EventHandler<BufferLengthChanged> BufferLengthChanged;
        public static event EventHandler<TimeSynchronizationOccurred> TimeSynchronizationOccurred;
        public static event EventHandler<ConnectionQualityChanged> ConnectionQualityChanged;

        private static volatile bool StopThreads;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ClientName"></param>
        /// <param name="ServerIP"></param>
        /// <param name="PortTCP"></param>
        /// <param name="PortUDP"></param>
        /// <param name="ReceiveBufferSize"></param>
        /// <param name="OperationPeriod"></param>
        /// <exception cref="Exception"> If ServerIP is unable to be parsed as an IPAddress object. </exception>
        public static void Start(string ClientName, string ServerIP, int PortTCP, int PortUDP, int ReceiveBufferSize = 64, int OperationPeriod = 20)
        {
            IPAddress ServerIPAsType = IPAddress.None;
            bool Valid = IPAddress.TryParse(ServerIP, out ServerIPAsType);
            if (!Valid) { throw new Exception("Failed to parse ServerIP as an IPAddress."); }
            Start(ClientName, IPAddress.Parse(ServerIP), PortTCP, PortUDP, ReceiveBufferSize, OperationPeriod);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ServerIP"></param>
        /// <param name="PortTCP"></param>
        /// <param name="PortUDP"></param>
        /// <param name="ClientName"></param>
        /// <param name="ReceiveBufferSize"></param>
        /// <param name="OperationPeriod"></param>
        public static void Start(string ClientName, IPAddress ServerIP, int PortTCP, int PortUDP, int ReceiveBufferSize = 64, int OperationPeriod = 20)
        {
            if (!Initialized)
            {
                Trace("Initializing Client.");

                // Initialize constructor variables into Client
                Client.ClientName = ClientName;
                Client.ReceiveBufferSize = ReceiveBufferSize;
                Client.OperationPeriod = OperationPeriod;
                Client.PortTCP = PortTCP;
                Client.PortUDP = PortUDP;
                Client.ServerIP = ServerIP;

                // Add traces and prints to events
                ConnectionStatusChanged += PrintConnectionStatus;
                BufferLengthChanged += TraceBufferChange;
                TimeSynchronizationOccurred += TraceTimeSynchronization;
                ConnectionQualityChanged += TraceConnectionQualityChanged;
            }
        }

        #region Connect
        
        /// <summary>
        /// 
        /// </summary>
        private static void Connect()
        {

        }

        #endregion

        #region Send

        /// <summary>
        /// 
        /// </summary>
        public static void Send(Packet SendPacket)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        public static void SendNow(Packet SendPacket)
        {

        }

        #endregion

        #region Receive

        /// <summary>
        /// 
        /// </summary>
        private static void Receive()
        {

        }

        #endregion

        public static void Stop()
        {
            StopThreads = true;
        }

        #region Trace and Logging

        private static void Trace(string Message) { Log.Trace(typeof(Client), Message, TraceLogging); }

        private static void TraceTimeSynchronization(object Sender, TimeSynchronizationOccurred Event) { Trace("System time synchronized with server."); }

        private static void TraceBufferChange(object Sender, BufferLengthChanged Event)
        {
            Trace("Buffer length automatically changed from " + Event.PreviousLength.ToString() + " to " + Event.Length.ToString() + ".");
        }

        private static void TraceConnectionQualityChanged(object Sender, ConnectionQualityChanged Event)
        {
            Trace("Connection quality changed from " + Event.PreviousQuality.ToString() + " to " + Event.Quality.ToString() + ".");
        }

        private static void PrintConnectionStatus(object Sender, ConnectionStatusChanged Event)
        {
            // Print regardless of trace logging setting
            if (Event.StatusConnected) { Log.Output(Log.Severity.DEBUG, Log.Source.NETWORK, "Server Connected."); }
            else { Log.Output(Log.Severity.WARNING, Log.Source.NETWORK, "Server Disconnected."); }
        }

        #endregion

    }
}
