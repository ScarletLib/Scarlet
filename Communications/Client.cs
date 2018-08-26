using Scarlet.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Scarlet.Communications
{
    /// <summary>
    /// 
    /// </summary>
    public static class Client
    {
        public static bool TraceLogging { get; set; }
        public static bool WatchdogLogging { get; set; }
        public static bool StorePackets { get; set; }
        public static bool Initialized { get; private set; }
        public static bool IsConnected { get; private set; }

        public static string ClientName { get; private set; }

        public static int ReceiveBufferSize { get; private set; }
        public static int OperationPeriod { get; private set; }
        public static int PortUDP { get; private set; }
        public static int PortTCP { get; private set; }
        public static int ConnectionQuality { get; private set; }

        public static IPAddress ServerIP { get; private set; }

        public static event EventHandler<ConnectionStatusChanged> ConnectionStatusChanged;
        public static event EventHandler<BufferLengthChanged> BufferLengthChanged;
        public static event EventHandler<TimeSynchronizationOccurred> TimeSynchronizationOccurred;
        public static event EventHandler<ConnectionQualityChanged> ConnectionQualityChanged;

        public static List<Packet> ReceivedPackets { get; private set; }
        public static List<Packet> SendPackets { get; private set; }

        public static LatencyMeasurementMode LatencyMeasurement;

        private static Queue<Packet> PacketProcessQueue;
        private static Queue<Packet> PacketSendQueue;

        private static TcpClient ServerTCP;
        private static UdpClient ServerUDP;

        private static volatile bool StopThreads;
        private static volatile bool WatchdogFoundOnInterval;

        private static bool ConnectionThreadRunning;

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
        public static void Start(string ClientName, string ServerIP, int PortTCP, int PortUDP, int ReceiveBufferSize = 64,
            int OperationPeriod = 20, LatencyMeasurementMode LatencyMode = LatencyMeasurementMode.FULL)
        {
            IPAddress ServerIPAsType = IPAddress.None;
            bool Valid = IPAddress.TryParse(ServerIP, out ServerIPAsType);
            if (!Valid) { throw new Exception("Failed to parse ServerIP as an IPAddress."); }
            Start(ClientName, IPAddress.Parse(ServerIP), PortTCP, PortUDP, ReceiveBufferSize, OperationPeriod, LatencyMode);
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
        public static void Start(string ClientName, IPAddress ServerIP, int PortTCP, int PortUDP, int ReceiveBufferSize = 64,
            int OperationPeriod = 20, LatencyMeasurementMode LatencyMode = LatencyMeasurementMode.FULL)
        {
            if (!Initialized)
            {
                Log.Output(Log.Severity.DEBUG, Log.Source.NETWORK, "Initializing Client.");

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

                // Add connection change trigger
                ConnectionStatusChanged += StartConnectOnConnFailure;

                // Initialize Data structures
                PacketProcessQueue = new Queue<Packet>();
                PacketSendQueue = new Queue<Packet>();

                // Try to open the connection
                if (!ConnectionThreadRunning) { ConnectThreadFactory().Start(); }
            }
            else { Trace("Client.Start() called when Client is already initialized."); }
        }

        #region Connect

        private static void Connect()
        {
            ConnectionThreadRunning = true;
            Trace("Attempting Server Connection.");
            while (!StopThreads && !IsConnected)
            {
                if (TryOpenConnection()) { SendHandshake(); }
                Thread.Sleep(Constants.CONNECTION_RETRY_DELAY);
            }
            ConnectionThreadRunning = false;
        }

        private static bool TryOpenConnection()
        {
            // Open TCP Connection
            ServerTCP = new TcpClient();
            IAsyncResult TCPConnResult = ServerTCP.BeginConnect(ServerIP, PortTCP, null, null);
            bool Success = TCPConnResult.AsyncWaitHandle.WaitOne(Constants.CONNECTION_RETRY_DELAY);
            if (!Success)
            {
                CloseConnection();
                Trace("Unable to connect to Server TCP port at " + ServerIP.ToString() + ":" + PortTCP.ToString() + " within " + Constants.CONNECTION_RETRY_DELAY.ToString() + " ms. Retrying.");
            }
            else
            {
                ServerTCP.NoDelay = true;
                ServerTCP.ReceiveBufferSize = ReceiveBufferSize;
            }

            // Open UDP Connection
            ServerUDP = new UdpClient();
            try { ServerUDP.Connect(new IPEndPoint(ServerIP, PortUDP)); }
            catch (SocketException)
            {
                Trace("Unable to connect to Server UDP socket at " + ServerIP.ToString() + ":" + PortUDP.ToString() + ". Retrying.");
                Success = false;
            }
            return Success;
        }

        private static void SendHandshake()
        {
            // Form handshake packet
            Packet Handshake = new Packet(Constants.HANDSHAKE_FROM_CLIENT, false);
            Handshake.AppendData(new byte[] { (byte)LatencyMeasurement, (byte)Utilities.Constants.SCARLET_VERSION });
            Handshake.AppendData(UtilData.ToBytes(ClientName));

            // Send handshake packet via TCP immediately
            try { ServerTCP.Client.Send(Handshake.GetForSend()); }
            catch (SocketException) { Trace("Unable send handshake to Server TCP port at " + ServerIP.ToString() + ":" + PortTCP.ToString() + ". Retrying."); }
        }

        internal static void ReceiveHandshake(Packet Handshake)
        {
            // TODO: Actually handle this
            IsConnected = true;
            ConnectionStatusChanged?.Invoke(Handshake, new ConnectionStatusChanged() { StatusEndpoint = "Server", StatusConnected = true });
        }

        private static void CloseConnection()
        {
            ServerTCP?.Close();
            ServerUDP?.Close();
            if (IsConnected) { ConnectionStatusChanged?.Invoke("Client", new ConnectionStatusChanged() { StatusEndpoint = "Server", StatusConnected = false }); }
            IsConnected = false;
        }

        private static void StartConnectOnConnFailure(object Sender, ConnectionStatusChanged Event)
        {
            if (!Event.StatusConnected && !ConnectionThreadRunning) { ConnectThreadFactory().Start(); }
        }

        #endregion

        #region Send

        /// <summary>
        /// 
        /// </summary>
        /// <param name="SendPacket"></param>
        public static void Send(Packet SendPacket)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="SendPacket"></param>
        public static void SendNow(Packet SendPacket)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="SendPacket"></param>
        internal static void SendRegardless(Packet SendPacket)
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

        #region Watchdogs

        internal static void HandleWatchdog(Packet Watchdog)
        {
            WatchdogFoundOnInterval = true;
            // Set telemetry based on watchdog packet
        }

        private static void WatchdogLoop()
        {
            while (!StopThreads)
            {

                Thread.Sleep(Constants.WATCHDOG_WAIT);
            }
        }

        #endregion

        #region Thread Factories

        private static Thread ConnectThreadFactory() { return new Thread(new ThreadStart(Connect)); }
        private static Thread SendThreadFactory() { return null; }
        private static Thread ReceiveUDPThreadFactory() { return null; }
        private static Thread ReceiveTCPThreadFactory() { return null; }
        private static Thread ProcessIncomingThreadFactory() { return null; }

        #endregion

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

        #region Info

        public static int GetProcessQueueLength() { return PacketProcessQueue?.Count ?? 0; }
        public static int GetSendQueueLength() { return PacketSendQueue?.Count ?? 0; }

        #endregion

    }
}
