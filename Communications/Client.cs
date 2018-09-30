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
    /// Client handles TCP and UDP send and receive to a remote Scarlet Server. Includes connection handling and connection quality reporting. 
    /// Also handles conditional packet sending for latent networks, automatic buffer length adjustment, and remote time synchronization.
    /// More information about Client and Scarlet networking available on the Scarlet Wiki: https://github.com/huskyroboticsteam/Scarlet/wiki/Networking
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
        public static ushort PortUDP { get; private set; }
        public static ushort PortTCP { get; private set; }
        public static int ConnectionQuality { get; private set; }

        public static IPAddress ServerIP { get; private set; }

        public static event EventHandler<ConnectionStatusChanged> ConnectionStatusChanged;
        public static event EventHandler<BufferLengthChanged> BufferLengthChanged;
        public static event EventHandler<TimeSynchronizationOccurred> TimeSynchronizationOccurred;
        public static event EventHandler<ConnectionQualityChanged> ConnectionQualityChanged;

        public static List<Packet> ReceivedPackets { get; private set; }
        public static List<Packet> SendPackets { get; private set; }

        public static LatencyMeasurementMode LatencyMeasurement { get; set; }
        public static ScarletVersion RemoteVersion { get; private set; }
        public static ClientServerConnectionState ClientServerConnectionState { get; private set; }

        public static string ServerName { get; private set; }

        #region Private Variables

        private static Queue<Packet> PacketProcessQueue;
        private static Queue<Packet> PacketSendQueue;

        private static Thread ConnectionThread;
        private static Thread ReceiveUDPThread;
        private static Thread ReceiveTCPThread;
        private static Thread SendThread;
        private static Thread PacketProcessThread;

        private static TcpClient ServerTCP;
        private static UdpClient ServerUDP;

        private static volatile bool StopThreads;
        private static volatile bool SendReceiveThreadsRunning;
        private static volatile bool WatchdogFoundOnInterval;

        #endregion

        /// <summary> Starts a Client with the given parameters. Call this method before using any functionality of Client. </summary>
        /// <param name="ClientName"> Arbitrary name of the Client. </param>
        /// <param name="ServerIP"> IP of remote server. </param>
        /// <param name="PortTCP"> TCP Connection port of remote server. </param>
        /// <param name="PortUDP"> UDP Connection port of remote server. </param>
        /// <param name="OperationPeriod"> Cyclic send / receive period. (Lower = faster, more CPU/Network usage) </param>
        /// <exception cref="Exception"> If ServerIP is unable to be parsed as an IPAddress object. </exception>
        public static void Start(string ClientName, string ServerIP, ushort PortTCP, ushort PortUDP, int OperationPeriod = 20, LatencyMeasurementMode LatencyMode = LatencyMeasurementMode.FULL)
        {
            IPAddress ServerIPAsType = IPAddress.None;
            bool Valid = IPAddress.TryParse(ServerIP, out ServerIPAsType);
            if (!Valid) { throw new Exception("Failed to parse ServerIP as an IPAddress."); }
            Start(ClientName, IPAddress.Parse(ServerIP), PortTCP, PortUDP, OperationPeriod, LatencyMode);
        }

        /// <summary> Starts a Client with the given parameters. Call this method before using any functionality of Client. </summary>
        /// <param name="ClientName"> Arbitrary name of the Client. </param>
        /// <param name="ServerIP"> IP of remote server. </param>
        /// <param name="PortTCP"> TCP Connection port of remote server. </param>
        /// <param name="PortUDP"> UDP Connection port of remote server. </param>
        /// <param name="OperationPeriod"> Cyclic send / receive period. (Lower = faster, more CPU/Network usage) </param>
        /// <exception cref="Exception"> If ServerIP is unable to be parsed as an IPAddress object. </exception>
        public static void Start(string ClientName, IPAddress ServerIP, ushort PortTCP, ushort PortUDP, int OperationPeriod = 20, LatencyMeasurementMode LatencyMode = LatencyMeasurementMode.FULL)
        {
            if (!Initialized)
            {
                Log.Output(Log.Severity.DEBUG, Log.Source.NETWORK, "Initializing Client.");

                // Start parsing
                InternalParsing.Start();

                // Initialize constructor variables into Client
                Client.ClientName = ClientName;
                Client.OperationPeriod = OperationPeriod;
                Client.PortTCP = PortTCP; // TODO: Ensure ports are in valid range
                Client.PortUDP = PortUDP;
                Client.ServerIP = ServerIP;

                ReceiveBufferSize = Constants.DEFAULT_RECEIVE_BUFFER_SIZE;

                // Add traces and prints to events
                ConnectionStatusChanged += PrintConnectionStatus;
                BufferLengthChanged += TraceBufferChange;
                TimeSynchronizationOccurred += TraceTimeSynchronization;
                ConnectionQualityChanged += TraceConnectionQualityChanged;

                // Add connection change trigger
                ConnectionStatusChanged += StartConnectOnConnFailure;
                ConnectionStatusChanged += SetIsConnected;

                // Initialize Data structures
                PacketProcessQueue = new Queue<Packet>();
                PacketSendQueue = new Queue<Packet>();

                StopThreads = false;
                SendReceiveThreadsRunning = false;

                // Try to open the connection
                if (!ThreadIsRunning(ConnectionThread))
                {
                    ConnectionThread = ConnectThreadFactory();
                    ConnectionThread.Start();
                }
            }
            else { Trace("Client.Start() called when Client is already initialized."); }
        }

        #region Connect

        private static void Connect()
        {
            Trace("Client attempting Server Connection.");
            while (!StopThreads && !IsConnected)
            {
                if (TryOpenConnection()) { SendHandshake(); }
                Thread.Sleep(Constants.CONNECTION_RETRY_DELAY);
            }
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
                Trace("Client unable to connect to Server TCP port at " + ServerIP.ToString() + ":" + PortTCP.ToString() + " within " + Constants.CONNECTION_RETRY_DELAY.ToString() + " ms. Retrying.");
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
                Trace("Client unable to connect to Server UDP socket at " + ServerIP.ToString() + ":" + PortUDP.ToString() + ". Retrying.");
                Success = false;
            }
            if (!Success) { return false; }
            Success = StartSendReceiveThreads();
            if (!Success) { Trace("Client unable to startup Send / Receive threads. Retrying."); }
            else
            {
                Trace("Client listening on TCP Port: " + ((IPEndPoint)ServerTCP.Client.LocalEndPoint).Port.ToString() + " UDP Port: " + ((IPEndPoint)ServerUDP.Client.LocalEndPoint).Port);
            }
            return Success;
        }

        private static void SendHandshake()
        {
            // Form handshake packet
            Packet Handshake = new Packet(Constants.HANDSHAKE_FROM_CLIENT, false);
            Handshake.AppendData(new byte[] { (byte)LatencyMeasurement, (byte)Utilities.Constants.SCARLET_VERSION });
            Handshake.AppendData(UtilData.ToBytes((ushort)((IPEndPoint)ServerUDP.Client.LocalEndPoint).Port));
            Handshake.AppendData(UtilData.ToBytes(ClientName));

            // Send handshake packet via TCP immediately
            try { ServerTCP.Client.Send(Handshake.GetForSend()); }
            catch (SocketException) { Trace("Client unable send handshake to Server TCP port at " + ServerIP.ToString() + ":" + PortTCP.ToString() + ". Retrying."); }
        }

        internal static void ReceiveHandshake(Packet Handshake)
        {
            byte[] Payload = Handshake.Payload;
            RemoteVersion = (ScarletVersion)Payload[0];
            ClientServerConnectionState = (ClientServerConnectionState)Payload[1];
            string ErrorMsg;
            Trace("Connecting to server resulted in state " + ClientServerConnectionState + ". Server is on version " + RemoteVersion + ".");
            switch (ClientServerConnectionState)
            {
                case ClientServerConnectionState.OKAY:
                    ConnectionStatusChanged?.Invoke("Client", new ConnectionStatusChanged() { StatusEndpoint = ServerName, StatusConnected = true });
                    break;
                case ClientServerConnectionState.INCOMPATIBLE_VERSIONS:
                    string ServerVersion = Enum.GetName(typeof(ScarletVersion), RemoteVersion);
                    string ClientVersion = Enum.GetName(typeof(ScarletVersion), Utilities.Constants.SCARLET_VERSION);
                    ErrorMsg = "Incompatible versions detected. Stopping Client. Please resolve between Client on " + ClientVersion + " and Server on " + ServerVersion + ".";
                    Log.Output(Log.Severity.ERROR, Log.Source.NETWORK, ErrorMsg);
                    Stop();
                    break;
                case ClientServerConnectionState.INVALID_NAME:
                    ErrorMsg = "Invalid name, " + ClientName + ", on Server [ServerName: " + ServerName + "]" + ". ";
                    ErrorMsg += "Name either already in use/reserved or has invalid characters. Stopping Client. Please try again with a different Client name.";
                    Log.Output(Log.Severity.ERROR, Log.Source.NETWORK, ErrorMsg);
                    Stop();
                    break;
            }

        }

        private static void CloseConnection()
        {
            ServerTCP?.Close();
            ServerUDP?.Close();
            if (IsConnected) { ConnectionStatusChanged?.Invoke("Client", new ConnectionStatusChanged() { StatusEndpoint = ServerName ?? "Server", StatusConnected = false }); }
            IsConnected = false;
        }

        private static void StartConnectOnConnFailure(object Sender, ConnectionStatusChanged Event)
        {
            if (!Event.StatusConnected && !ThreadIsRunning(ConnectionThread))
            {
                ConnectionThread = ConnectThreadFactory();
                ConnectionThread.Start();
            }
        }

        private static void SetIsConnected(object Sender, ConnectionStatusChanged Event) { IsConnected = Event.StatusConnected; }

        #endregion

        #region Send

        private static void SendQueue()
        {
            while (!StopThreads)
            {
                while (PacketSendQueue.Count != 0)
                {
                    Packet SendPacket;
                    lock (PacketSendQueue) { SendPacket = PacketSendQueue.Dequeue(); }
                    SendNow(SendPacket);
                }
                Thread.Sleep(OperationPeriod);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="SendPacket"></param>
        public static void Send(Packet SendPacket)
        {
            if (!Initialized) { Log.Output(Log.Severity.WARNING, Log.Source.NETWORK, "CANNOT SEND WITHOUT INITIALIZING CLIENT."); }
            lock (PacketSendQueue) { PacketSendQueue.Enqueue(SendPacket); }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="SendPacket"></param>
        public static bool SendNow(Packet SendPacket)
        {
            if (!Initialized)
            {
                Log.Output(Log.Severity.WARNING, Log.Source.NETWORK, "CANNOT SEND WITHOUT INITIALIZING CLIENT.");
                return false;
            }
            if (IsConnected) { return SendRegardless(SendPacket); }
            Trace("Client attempted packet send in disconnected state.");
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="SendPacket"></param>
        internal static bool SendRegardless(Packet SendPacket)
        {
            if (!Initialized) { Trace("Scarlet internally attempted sending without Client initialization."); }
            SendPacket.UpdateTimestamp();
            byte[] Data = SendPacket.GetForSend();
            if (SendPacket.IsUDP)
            {
                try
                {
                    ServerUDP.Send(Data, Data.Length);
                }
                catch (SocketException)
                {
                    Trace("Client failed to send packet via UDP because it could not access the UDP socket. Try again.");
                    return false;
                }
                catch (ObjectDisposedException)
                {
                    Trace("Client failed to send packet via UDP because the UDP client was prematurely closed. Try again or restart Client.");
                    return false;
                }
                catch (InvalidOperationException)
                {
                    Trace("Client failed to send packet via UDP because the UDP client already established a default remote host. Try again or restart Client.");
                    return false;
                }
                return true;
            }
            else
            {
                try
                {
                    ServerTCP.Client.Send(Data);
                }
                catch (SocketException)
                {
                    Trace("Client failed to send packet via TCP because it could not access the TCP socket. Try again.");
                    return false;
                }
                catch (ObjectDisposedException)
                {
                    Trace("Client failed to send packet via TCP because the TCP socket was prematurely closed. Try again or restart Client.");
                    return false;
                }
            }
            return true;
        }

        #endregion

        #region Receive

        private static void ReceiveOnSocket(object ReceiveSocket)
        {
            Socket Socket = (Socket)ReceiveSocket;
            while (!StopThreads && Socket != null)
            {
                bool ReadFailureAlertSent = false;
                Trace("Available bytes on socket: " + Socket.Available);
                if (Socket.Available >= Constants.PACKET_HEADER_SIZE)
                {
                    byte[] ReceiveBuffer = new byte[ReceiveBufferSize];
                    try
                    {
                        int Size;

                        // Peek at incoming packets
                        lock (Socket) { Size = Socket.Receive(ReceiveBuffer, ReceiveBuffer.Length, SocketFlags.Peek); }
                        Trace("Incoming packet, will read " + Size + " bytes.");

                        // Find maximum number of packets in this set
                        int MaxPackets = Size / Constants.PACKET_HEADER_SIZE;
                        byte[] Message = ReceiveBuffer.Take(Size).ToArray();
                        int Offset = 0;
                        int BytesRead = 0;
                        for (int i = 0; i < MaxPackets; i++)
                        {
                            // Find length of this packet
                            int Index = i * Constants.PACKET_HEADER_SIZE + Offset;
                            int PayloadLength = ((Message[Index + 5] << 8) | Message[Index + 6]) & 0x00FF;
                            Offset += PayloadLength;
                            int FullLength = Constants.PACKET_HEADER_SIZE + PayloadLength;
                            BytesRead += FullLength;
                            // Take the packet, form it, and put it in the process queue
                            byte[] ThisPacket = Message.Skip(Index).Take(FullLength).ToArray();
                            Trace("Received packet consisting of data: " + UtilMain.BytesToNiceString(ThisPacket, true));
                            lock (PacketProcessQueue) { PacketProcessQueue.Enqueue(Packet.FromBytes(ThisPacket, Socket.ProtocolType)); }
                        }

                        // Flush the complete packets
                        ReceiveBuffer = new byte[BytesRead];
                        lock (Socket) { Socket.Receive(ReceiveBuffer, BytesRead, SocketFlags.None); }
                    }
                    catch (SocketException)
                    {
                        if (!ReadFailureAlertSent)
                        {
                            Trace("Failing to read from " + Enum.GetName(typeof(SocketType), Socket.SocketType).ToUpper() + " socket. Check Connection.");
                            ReadFailureAlertSent = true;
                        }
                    }
                }
                Thread.Sleep(OperationPeriod);
            }
            Trace("Stopping receive process on Socket. " + ReceiveSocket == null ? "" : "Socket is null.");
        }

        private static void ProcessPackets()
        {
            while (!StopThreads)
            {
                while (PacketProcessQueue?.Count != 0)
                {
                    Packet CurrentPacket = PacketProcessQueue?.Dequeue();
                    if (CurrentPacket != null)
                    {
                        CurrentPacket.Endpoint = CurrentPacket.Endpoint ?? "Server";

                        Parse.ParseMessage(CurrentPacket);
                    }
                }
                Thread.Sleep(OperationPeriod);
            }
        }

        #endregion

        #region Control

        public static void Stop()
        {
            new Thread(() => 
            {
                StopThreads = true;
                CloseConnection();
                Initialized = false;
            }).Start();
            Log.Output(Log.Severity.DEBUG, Log.Source.NETWORK, "Stopping Client.");
        }

        #endregion

        #region Watchdogs

        internal static void HandleWatchdog(Packet Watchdog)
        {
            WatchdogFoundOnInterval = true;

            // Set telemetry based on watchdog packet

            // Send new Watchdog
            Packet WatchdogPacket = new Packet(Constants.WATCHDOG_FROM_CLIENT, true);
            short? LatencyInfo = null;
            switch (LatencyMeasurement)
            {
                // TODO: Implement non-meaningless values here
                case LatencyMeasurementMode.FULL:
                    LatencyInfo = 0;
                    break;
                case LatencyMeasurementMode.BASIC:
                    LatencyInfo = 0;
                    break;
            }
            if (LatencyInfo != null) { WatchdogPacket.AppendData(UtilData.ToBytes((short)LatencyInfo)); }
            WatchdogPacket.AppendData(UtilData.ToBytes(ClientName));
            SendNow(WatchdogPacket);
        }

        private static void WatchdogLoop()
        {
            while (!StopThreads)
            {
                // If Watchdog and IsConnected don't match, there is a change in state
                if (WatchdogFoundOnInterval ^ IsConnected)
                {
                    ConnectionStatusChanged NewStatus = new ConnectionStatusChanged()
                    {
                        StatusConnected = WatchdogFoundOnInterval,
                        StatusEndpoint = "Server",
                    };
                    ConnectionStatusChanged?.Invoke("Client", NewStatus);
                }
                WatchdogFoundOnInterval = false;
                Thread.Sleep(Constants.WATCHDOG_WAIT);
            }
        }

        #endregion

        #region Thread Starts & Thread Factories

        private static Thread ConnectThreadFactory() { return new Thread(new ThreadStart(Connect)); }
        private static Thread SendThreadFactory() { return new Thread(new ThreadStart(SendQueue)); }
        private static Thread ReceiveThreadFactory() { return new Thread(new ParameterizedThreadStart(ReceiveOnSocket)); }
        private static Thread ProcessIncomingThreadFactory() { return new Thread(new ThreadStart(ProcessPackets)); }
        private static Thread WatchdogThreadFactory() { return new Thread(new ThreadStart(WatchdogLoop)); }

        private static bool StartSendReceiveThreads()
        {
            if (!SendReceiveThreadsRunning)
            {
                SendThread = SendThreadFactory();
                ReceiveUDPThread = ReceiveThreadFactory();
                ReceiveTCPThread = ReceiveThreadFactory();
                PacketProcessThread = ProcessIncomingThreadFactory();
                SendThread.Start();
                ReceiveUDPThread.Start(ServerUDP.Client);
                ReceiveTCPThread.Start(ServerTCP.Client);
                PacketProcessThread.Start();
                bool Success = SendThread.IsAlive;
                Success &= ReceiveUDPThread.IsAlive;
                Success &= ReceiveTCPThread.IsAlive;
                Success &= PacketProcessThread.IsAlive;
                SendReceiveThreadsRunning = Success;
                if (Success) { Trace("Client successfully started send and receive threads."); }
                else { Trace("Client failed starting send and receive threads."); }
            }
            return SendReceiveThreadsRunning;
        }

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

        #region Info & Utility

        public static int GetProcessQueueLength() { return PacketProcessQueue?.Count ?? 0; }
        public static int GetSendQueueLength() { return PacketSendQueue?.Count ?? 0; }

        private static bool ThreadIsRunning(Thread CheckThread) { return !(CheckThread == null || CheckThread?.ThreadState == ThreadState.Stopped); }

        #endregion

    }
}
