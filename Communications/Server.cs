using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Scarlet.Utilities;

namespace Scarlet.Communications
{
    public static class Server
    {
        public static bool OutputWatchdogDebug { get; set; }
        public static bool TraceLogging { get; set; }

        public static bool StorePackets { get; set; }
        public static List<Packet> PacketsReceived { get; private set; }
        public static List<Packet> PacketsSent { get; private set; }

        public static event EventHandler<EventArgs> ClientConnectionChange;

        private static Dictionary<string, Queue<Packet>> SendQueues; // Client Name -> Packet Queue
        private static Queue<Packet> ReceiveQueue;

        private static UdpClient UDPListener;
        private static TcpListener TCPListener;

        private static Dictionary<string, ScarletClient> Clients;

        private static Thread SendThread, ReceiveThreadTCP, ReceiveThreadUDP, ProcessThread;

        private static bool Initialized = false;
        private static volatile bool Stopping = false;
        private static int ReceiveBufferSize = 64;
        private static int OperationPeriod = 10;

        /*
         * The overall flow is this:
         * - Server is started
         *  - TCP wait thread is started
         *   - Waits for incoming TCP clients
         *    - Starts HandleTCPClient thread for every incoming client
         *    - Repeat
         *  - UDP wait thread is started
         *  - Packet processing (incoming packet parsing) thread is started
         *  - Packet sending thread is started
         */

        public static void Start(ushort PortTCP, ushort PortUDP)
        {
            Stopping = false;

            if (!Initialized)
            {
                Log.Output(Log.Severity.DEBUG, Log.Source.NETWORK, "Initializing Server.");
                Log.Output(Log.Severity.DEBUG, Log.Source.NETWORK, "Listening on ports " + PortTCP + " (TCP), and " + PortUDP + " (UDP).");

                Clients = new Dictionary<string, ScarletClient>();
                SendQueues = new Dictionary<string, Queue<Packet>>();
                ReceiveQueue = new Queue<Packet>();
                PacketsSent = new List<Packet>();
                PacketsReceived = new List<Packet>();

                InternalParsing.Start();
                new Thread(new ParameterizedThreadStart(StartThreads)).Start(new Tuple<int, int>(PortTCP, PortUDP));

                // TODO: Watchdogs?

                Initialized = true;
            }
            else { Log.Output(Log.Severity.WARNING, Log.Source.NETWORK, "Attempted to start Server when it was already started. Ignoring."); }
        }

        /// <summary> Starts all Server threads, then waits for them to terminate. </summary>
        /// <param name="Ports"> <see cref="Tuple{T1, T2}(int, int)"/> of ports, corresponding to (TCP, UDP). </param>
        private static void StartThreads(object Ports)
        {
            ReceiveThreadTCP = new Thread(new ParameterizedThreadStart(WaitForClientsTCP));
            ReceiveThreadTCP.Start(((Tuple<int, int>)Ports).Item1);

            ReceiveThreadUDP = new Thread(new ParameterizedThreadStart(WaitForClientsUDP));
            ReceiveThreadUDP.Start(((Tuple<int, int>)Ports).Item2);

            ProcessThread = new Thread(new ThreadStart(ProcessPackets));
            ProcessThread.Start();

            SendThread = new Thread(new ThreadStart(SendPackets));
            SendThread.Start();

            ReceiveThreadTCP.Join();
            Log.Trace(typeof(Server), "TCP receiver thread stopped.", TraceLogging);

            ReceiveThreadUDP.Join();
            Log.Trace(typeof(Server), "UDP receiver thread stopped.", TraceLogging);

            ProcessThread.Join();
            Log.Trace(typeof(Server), "Packet processing thread stopped.", TraceLogging);

            SendThread.Join();
            Log.Trace(typeof(Server), "Packet sending thread stopped.", TraceLogging);

            Log.Output(Log.Severity.INFO, Log.Source.NETWORK, "Server stopped.");
            Initialized = false;
        }

        /// <summary> Sends signal to all components of Server to stop, then waits for everything to shut down. </summary>
        public static void Stop()
        {
            if (!Initialized) { return; } // We never even started
            Log.Output(Log.Severity.DEBUG, Log.Source.NETWORK, "Stopping Server.");
            Stopping = true;

            // TODO: Stop watchdogs?

            // This is a meh solution to the WaitForClientsTCP thread not ending until the next client connects.
            TcpClient Dummy = new TcpClient();
            Dummy.Connect(new IPEndPoint(IPAddress.Loopback, ((IPEndPoint)TCPListener.LocalEndpoint).Port));
            Dummy.Close();

            while (Initialized) { Thread.Sleep(50); } // Wait for all threads to stop.
        }

        #region TCP Handling
        /// <summary> Waits for incoming TCP clients, then creates a HandleTCPClient thread to interface with each client. </summary>
        /// <param name="ReceivePort"> The port to listen for TCP clients on. Must be int. </param>
        private static void WaitForClientsTCP(object ReceivePort)
        {
            if (!Initialized) { throw new InvalidOperationException("Cannot use Server before initialization. Call Server.Start()."); }
            TCPListener = new TcpListener(new IPEndPoint(IPAddress.Any, (int)ReceivePort));
            TCPListener.Start();

            List<Thread> ClientThreads = new List<Thread>();

            while (!Stopping)
            {
                TcpClient Client = TCPListener.AcceptTcpClient();
                if (!Stopping) { Log.Output(Log.Severity.DEBUG, Log.Source.NETWORK, "Client is connecting."); }

                // Start sub-threads for every client.
                Thread ClientThread = new Thread(new ParameterizedThreadStart(HandleTCPClient));
                ClientThreads.Add(ClientThread);
                ClientThread.Start(Client);
            }
            TCPListener.Stop();
            ClientThreads.ForEach(x => x?.Abort());
            ClientThreads.ForEach(x => x?.Join());
        }

        /// <summary>
        /// Waits for, and receives data from a connected TCP client.
        /// This must be started on a thread, as it will block until <see cref="Stopping"/> is true, or the client disconnects.
        /// </summary>
        /// <param name="ClientObj"> The client to receive data from. Must be <see cref="TcpClient"/>. </param>
        private static void HandleTCPClient(object ClientObj)
        {
            TcpClient Client = (TcpClient)ClientObj;

            void SendHandshakeResponse(ClientServerConnectionState ErrorCode)
            {
                try
                {
                    byte[] PacketData = new byte[Packet.HEADER_LENGTH + sizeof(byte) + sizeof(byte)];
                    Array.Copy(UtilData.ToBytes(DateTime.Now.Ticks), 0, PacketData, 0, sizeof(long)); // 0-7 (8B)
                    PacketData[8] = Constants.HANDSHAKE_FROM_SERVER; // 8 (1B)
                    Array.Copy(UtilData.ToBytes((ushort)PacketData.Length), 0, PacketData, 9, sizeof(ushort)); // 9-10 (2B)
                    PacketData[11] = (byte)Utilities.Constants.SCARLET_VERSION; // 11 (1B)
                    PacketData[12] = (byte)ErrorCode; // 12 (1B)
                    Log.Trace(typeof(Server), "Sending handshake response: " + UtilMain.BytesToNiceString(PacketData, true), TraceLogging);
                    Client.GetStream().Write(PacketData, 0, PacketData.Length);
                }
                catch (Exception Exc)
                {
                    if (Exc is ThreadAbortException) { throw; } // This just means Server is stopping, no need to alert.
                    Log.Output(Log.Severity.WARNING, Log.Source.NETWORK, "An error occurred when sending the handshake response to the connecting TCP client.");
                    Log.Exception(Log.Source.NETWORK, Exc);
                }
            }

            NetworkStream ReceiveStream = Client.GetStream();
            if (!ReceiveStream.CanRead)
            {
                Log.Output(Log.Severity.ERROR, Log.Source.NETWORK, "Client TCP NetworkStream connection does not permit reading.");
                return;
            }

            // Receive client information.
            ScarletClient ConnectedClient;
            byte[] DataBuffer = new byte[Math.Max(ReceiveBufferSize, 64)]; // Guarantee at least 64B

            try
            {
                int ReceivedByteLength = ReceiveStream.Read(DataBuffer, 0, DataBuffer.Length);
                Log.Trace(typeof(Server), "During TCP client connection phase, received " + ReceivedByteLength + " bytes.", TraceLogging);
                Log.Trace(typeof(Server), "Handshake Data: " + UtilMain.BytesToNiceString(UtilMain.SubArray(DataBuffer, 0, ReceivedByteLength), true));
                if (ReceivedByteLength == 0)
                {
                    ReceiveStream?.Close();

                    // The if statement is here so that we don't output this when the dummy connection is sent to terminate this thread.
                    if (!Stopping) { Log.Output(Log.Severity.WARNING, Log.Source.NETWORK, "TCP Client disconnected before sending name. Terminating connection."); }
                    return;
                }
                else // We got some data from the client.
                {
                    const int HANDSHAKE_DATA_MIN_LENGTH = sizeof(byte) + sizeof(byte) + sizeof(ushort); // The amount of data we expect to see in the packet after the header, not including the variable length name.
                    if (ReceivedByteLength < Packet.HEADER_LENGTH)
                    {
                        Log.Output(Log.Severity.WARNING, Log.Source.NETWORK, "TCP Client tried to connect with incomplete handshake packet header. Terminating connection.");
                        SendHandshakeResponse(ClientServerConnectionState.CONNECTION_FAILED);
                        return;
                    }
                    else if (DataBuffer[8] != Constants.HANDSHAKE_FROM_CLIENT)
                    {
                        Log.Output(Log.Severity.WARNING, Log.Source.NETWORK, "TCP Client tried to connect with packet other than handshake. Terminating connection.");
                        SendHandshakeResponse(ClientServerConnectionState.CONNECTION_FAILED);
                        return;
                    }
                    else if (ReceivedByteLength == Packet.HEADER_LENGTH)
                    {
                        Log.Output(Log.Severity.WARNING, Log.Source.NETWORK, "TCP Client tried to connect without sending information about itself. Terminating connection.");
                        SendHandshakeResponse(ClientServerConnectionState.CONNECTION_FAILED);
                        return;
                    }
                    else if (ReceivedByteLength < (Packet.HEADER_LENGTH + HANDSHAKE_DATA_MIN_LENGTH))
                    {
                        Log.Output(Log.Severity.WARNING, Log.Source.NETWORK, "TCP Client tried to connect with incomplete handshake data. Terminating connection.");
                        SendHandshakeResponse(ClientServerConnectionState.CONNECTION_FAILED);
                        return;
                    }
                    else if (ReceivedByteLength == (Packet.HEADER_LENGTH + HANDSHAKE_DATA_MIN_LENGTH))
                    {
                        Log.Output(Log.Severity.WARNING, Log.Source.NETWORK, "TCP Client tried to connect with no name. Terminating connection.");
                        SendHandshakeResponse(ClientServerConnectionState.INVALID_NAME);
                        return;
                    }

                    // Check if we have the full packet. If not, we need to get the rest of the data.
                    ushort ExpectedLength = UtilData.ToUShort(UtilMain.SubArray(DataBuffer, 9, sizeof(ushort)));
                    if (ExpectedLength > ReceivedByteLength)
                    {
                        Log.Trace(typeof(Server), "Handshake packet larger than received data, attempting further read.", TraceLogging);
                        byte[] DataBufferExtended = new byte[ExpectedLength];
                        Array.Copy(DataBuffer, DataBufferExtended, ReceivedByteLength);
                        int ReceivedByteLengthExtended = ReceiveStream.Read(DataBufferExtended, ReceivedByteLength, (ExpectedLength - ReceivedByteLength));
                        Log.Trace(typeof(Server), "Read an additional " + ReceivedByteLengthExtended + " bytes.", TraceLogging);
                        if (ExpectedLength > (ReceivedByteLength + ReceivedByteLengthExtended))
                        {
                            Log.Output(Log.Severity.WARNING, Log.Source.NETWORK, "Failed to get full handshake packet from incoming TCP client. Terminating connection.");
                            SendHandshakeResponse(ClientServerConnectionState.CONNECTION_FAILED);
                            return;
                        }
                        else
                        {
                            DataBuffer = DataBufferExtended;
                            ReceivedByteLength += ReceivedByteLengthExtended;
                        }
                    }

                    // We are now ready to start interpreting the handshake packet.
                    LatencyMeasurementMode LatencyMode;
                    byte ScarletVersion;
                    ushort UDPPort;
                    string ClientName;

                    try
                    {
                        LatencyMode = (LatencyMeasurementMode)DataBuffer[Packet.HEADER_LENGTH + 0];
                        ScarletVersion = DataBuffer[Packet.HEADER_LENGTH + 1];
                        UDPPort = UtilData.ToUShort(DataBuffer, (Packet.HEADER_LENGTH + 2));
                        ClientName = UtilData.ToString(UtilMain.SubArray(DataBuffer, (Packet.HEADER_LENGTH + 4), (ExpectedLength - (Packet.HEADER_LENGTH + 4))));
                    }
                    catch (Exception Exc)
                    {
                        if (Exc is ThreadAbortException) { throw; } // This just means Server is stopping, no need to alert.
                        Log.Output(Log.Severity.WARNING, Log.Source.NETWORK, "Failed to interpret incoming TCP client information. Terminating connection.");
                        Log.Exception(Log.Source.NETWORK, Exc);
                        SendHandshakeResponse(ClientServerConnectionState.CONNECTION_FAILED);
                        return;
                    }

                    // To be enabled at a later time if future updates cause breaking network changes.
                    /*if (ScarletVersion < 1) // If the client is running a version incompatible with this server, disconnect it.
                    {
                        Log.Output(Log.Severity.WARNING, Log.Source.NETWORK, "Incoming TCP client is running Scarlet version " + ScarletVersion + ", which is incompatible. Update the client. Terminating connection.");
                        SendHandshakeResponse((byte)ClientServerConnectionState.INCOMPATIBLE_VERSIONS);
                        return;
                    }*/

                    if (ClientName != null && ClientName.Length > 0)
                    {
                        Log.Output(Log.Severity.INFO, Log.Source.NETWORK, "TCP Client connected with name \"" + ClientName + "\".");
                        lock (Clients)
                        {
                            if (Clients.ContainsKey(ClientName))
                            {
                                Log.Trace(typeof(Server), "Client \"" + ClientName + "\" already exists. Updating status.", TraceLogging);
                                Clients[ClientName].TCP = Client;
                                Clients[ClientName].Connected = true;
                                Clients[ClientName].LatencyMode = LatencyMode;
                                Clients[ClientName].ScarletVersion = ScarletVersion;
                                Clients[ClientName].EndpointUDP = (IPEndPoint)Client.Client.RemoteEndPoint;
                                Clients[ClientName].EndpointUDP.Port = UDPPort;
                                ConnectedClient = Clients[ClientName];
                            }
                            else
                            {
                                Log.Trace(typeof(Server), "Client \"" + ClientName + "\" is new. Adding info.", TraceLogging);
                                ScarletClient NewClient = new ScarletClient()
                                {
                                    Name = ClientName,
                                    TCP = Client,
                                    LatencyMode = LatencyMode,
                                    ScarletVersion = ScarletVersion,
                                    EndpointUDP = (IPEndPoint)Client.Client.RemoteEndPoint,
                                    Connected = true
                                };
                                NewClient.EndpointUDP.Port = UDPPort;
                                Clients.Add(ClientName, NewClient);
                                ConnectedClient = NewClient;
                            }
                        }

                        if (!SendQueues.ContainsKey(ClientName)) { SendQueues.Add(ClientName, new Queue<Packet>()); }
                    }
                    else
                    {
                        Log.Output(Log.Severity.WARNING, Log.Source.NETWORK, "Invalid TCP client name received. Dropping connection.");
                        SendHandshakeResponse(ClientServerConnectionState.INVALID_NAME);
                        return;
                    }
                }
            }
            catch (Exception Exc)
            {
                if (Exc is ThreadAbortException) { throw; } // This just means Server is stopping, no need to alert.
                Log.Output(Log.Severity.WARNING, Log.Source.NETWORK, "Something went wrong while attempting to handle incoming client. Dropping connection.");
                Log.Exception(Log.Source.NETWORK, Exc);
                SendHandshakeResponse(ClientServerConnectionState.CONNECTION_FAILED);
                ReceiveStream?.Close();
                return;
            }

            SendHandshakeResponse(ClientServerConnectionState.OKAY);

            // The client is now connected.
            ClientConnChange(new ClientConnectionChangeEvent() { ClientName = ConnectedClient.Name, IsNowConnected = true });

            // TODO: Add to watchdog dispatch

            // Receive data from client.
            DataBuffer = new byte[ReceiveBufferSize];
            byte[] LeftoverData = null;

            while (!Stopping && Clients[ConnectedClient.Name].Connected)
            {
                try
                {
                    int ReceivedByteLength;
                    if (LeftoverData != null)
                    {
                        ReceivedByteLength = LeftoverData.Length;
                        if (DataBuffer.Length < LeftoverData.Length) { DataBuffer = new byte[LeftoverData.Length]; }
                        Array.Copy(LeftoverData, DataBuffer, LeftoverData.Length);
                        LeftoverData = null;
                        Log.Trace(typeof(Server), "Using leftover data of length " + ReceivedByteLength + ".", TraceLogging);
                    }
                    else
                    {
                        ReceivedByteLength = ReceiveStream.Read(DataBuffer, 0, DataBuffer.Length);
                        Log.Trace(typeof(Server), "Received data from TCP client of length " + ReceivedByteLength + ".", TraceLogging);
                    }

                    if (ReceivedByteLength == 0)
                    {
                        Log.Output(Log.Severity.INFO, Log.Source.NETWORK, "Client \"" + ConnectedClient.Name + "\" has disconnected.");
                        lock (Clients[ConnectedClient.Name]) { Clients[ConnectedClient.Name].Connected = false; }
                        break;
                    }
                    if (ReceivedByteLength >= Packet.HEADER_LENGTH)
                    {
                        ushort ExpectedLength = UtilData.ToUShort(UtilMain.SubArray(DataBuffer, 9, sizeof(ushort)));
                        if (ExpectedLength > ReceivedByteLength) // If we haven't gotten enough data to satisfy the packet length
                        {
                            Log.Trace(typeof(Server), "Packet larger than received data, attempting further read.", TraceLogging);
                            byte[] DataBufferExtended = new byte[ExpectedLength];
                            Array.Copy(DataBuffer, DataBufferExtended, ReceivedByteLength);
                            int ReceivedByteLengthExtended = ReceiveStream.Read(DataBufferExtended, ReceivedByteLength, (ExpectedLength - ReceivedByteLength));
                            Log.Trace(typeof(Server), "Read an additional " + ReceivedByteLengthExtended + " bytes.", TraceLogging);
                            if (ExpectedLength > (ReceivedByteLength + ReceivedByteLengthExtended))
                            {
                                Log.Output(Log.Severity.WARNING, Log.Source.NETWORK, "Failed to get entire packet on extended attempt. Check that the packet's length field matches the actual amount of data. The packet was discarded.");
                                continue;
                            }
                            else
                            {
                                DataBuffer = DataBufferExtended;
                                ReceivedByteLength += ReceivedByteLengthExtended;
                            }
                        }
                        else if (ReceivedByteLength > ExpectedLength) // We got more data than expected, the remainder will be interpreted as another packet.
                        {
                            Log.Trace(typeof(Server), "Packet smaller than received data, saving data for next round.", TraceLogging);
                            LeftoverData = new byte[ReceivedByteLength - ExpectedLength];
                            Array.Copy(DataBuffer, ExpectedLength, LeftoverData, 0, (ReceivedByteLength - ExpectedLength));

                            // TODO: If multiple packets are received combined, and the last packet does not have a complete header, then it will be discarded. Is this possible, and if so, worth addressing?
                            if (LeftoverData.Length < Packet.HEADER_LENGTH) { Log.Output(Log.Severity.ERROR, Log.Source.NETWORK, "Packet splitting length created incomplete header. The next packet(s) will fail to be interpreted. Please report this issue to the developers!"); }
                        }
                        byte[] PacketData = new byte[ExpectedLength];
                        Array.Copy(DataBuffer, PacketData, ExpectedLength);
                        Packet ReceivedPack = new Packet(new Message(PacketData), false, ConnectedClient.Name);
                        ReceiveQueue.Enqueue(ReceivedPack);
                        if (StorePackets) { PacketsReceived.Add(ReceivedPack); }
                    }
                    else { Log.Output(Log.Severity.WARNING, Log.Source.NETWORK, "Data received from client was too short. Discarding."); }
                }
                catch (IOException IOExc)
                {
                    if (IOExc.InnerException is SocketException)
                    {
                        int Error = ((SocketException)IOExc.InnerException).ErrorCode;
                        Log.Output(Log.Severity.WARNING, Log.Source.NETWORK, "Failed to read data from connected client with SocketExcpetion code " + Error);
                        Log.Exception(Log.Source.NETWORK, IOExc);
                        if (Error == 10054 && Clients != null && ConnectedClient != null && Clients.ContainsKey(ConnectedClient.Name) && Clients[ConnectedClient.Name] != null) { Clients[ConnectedClient.Name].Connected = false; } // The connection was reset (the client probably terminated).
                    }
                    else
                    {
                        Log.Output(Log.Severity.WARNING, Log.Source.NETWORK, "Failed to read data from connected client because of IO exception.");
                        Log.Exception(Log.Source.NETWORK, IOExc);
                    }
                }
                catch (Exception OtherExc)
                {
                    if (OtherExc is ThreadAbortException) { throw; } // This just means Server is stopping, no need to alert.
                    Log.Output(Log.Severity.WARNING, Log.Source.NETWORK, "Failed to read data from connected client.");
                    Log.Exception(Log.Source.NETWORK, OtherExc);
                }
                if (!ReceiveStream.DataAvailable && LeftoverData == null) { Thread.Sleep(OperationPeriod); } // If we have nothing to do, wait until there is data available.
            }
            lock (Clients) { Clients.Remove(ConnectedClient.Name); }
            Client.Client.Disconnect(true);
            ReceiveStream.Close();
            Client.Close();
            ClientConnChange(new ClientConnectionChangeEvent() { ClientName = ConnectedClient.Name, IsNowConnected = false });
        }
        #endregion

        #region UDP Handling
        /// <summary> Initially starts the UDP receiver. </summary>
        /// <param name="ReceivePort"> Port to listen for UDP packets on. Must be <see cref="int"/>. </param>
        private static void WaitForClientsUDP(object ReceivePort)
        {
            UDPListener = new UdpClient(new IPEndPoint(IPAddress.Any, (int)ReceivePort));
            UDPListener.BeginReceive(HandleUDPData, UDPListener);
        }

        /// <summary> Processes incoming UDP packet, then starts the listener again. </summary>
        private static void HandleUDPData(IAsyncResult Result)
        {
            UdpClient Listener = null;
            IPEndPoint ReceivedEndpoint;
            byte[] Data;
            string ClientName;

            try
            {
                Listener = (UdpClient)Result.AsyncState;
                ReceivedEndpoint = new IPEndPoint(IPAddress.Any, 0);
                Data = Listener.EndReceive(Result, ref ReceivedEndpoint);
                ClientName = FindClient(ReceivedEndpoint, true);
            }
            catch (Exception Exc)
            {
                Log.Output(Log.Severity.WARNING, Log.Source.NETWORK, "Something went wrong while trying to receive UDP data.");
                Log.Exception(Log.Source.NETWORK, Exc);
                if (Listener != null) { Listener.BeginReceive(HandleUDPData, Listener); }
                return;
            }

            if (Data.Length == 0) // TODO: Can this happen?
            {
                Log.Output(Log.Severity.INFO, Log.Source.NETWORK, "UDP Client \"" + ClientName + "\" has disconnected.");
                if (ClientName != null)
                {
                    lock (Clients[ClientName]) { Clients[ClientName].Connected = false; }
                }
            }
            else
            {
                Packet ReceivedPack = new Packet(new Message(Data), true, (ClientName ?? "UNKNOWN"));
                ReceiveQueue.Enqueue(ReceivedPack);
                if (StorePackets) { PacketsReceived.Add(ReceivedPack); }
            }
            Listener.BeginReceive(HandleUDPData, Listener);
        }
        #endregion

        /// <summary> Tries to find the client name that matches the given IPEndPoint. </summary>
        public static string FindClient(IPEndPoint Endpoint, bool IsUDP)
        {
            try
            {
                string Result;
                if (IsUDP) { Result = Clients.Where(Pair => Pair.Value.EndpointUDP.Equals(Endpoint)).Single().Key; }
                else { Result = Clients.Where(Pair => Pair.Value.EndpointTCP.Equals(Endpoint)).Single().Key; }
                return Clients[Result].Connected ? Result : null;
            }
            catch { return null; }
        }

        public class ClientConnectionChangeEvent : EventArgs
        {
            public string ClientName { get; set; }
            public bool IsNowConnected { get; set; }
        }

        private static void ClientConnChange(ClientConnectionChangeEvent Event) { ClientConnectionChange?.Invoke("Server", Event); }

        #region Processing Incoming Packets
        /// <summary>
        /// Pushes received packets through to Parse for processing.
        /// This must be started on a thread, as it will block until <see cref="Stopping"/> is true.
        /// Assumes that packets will not be removed from <see cref="ReceiveQueue"/> anywhere but inside this method.
        /// </summary>
        private static void ProcessPackets()
        {
            while (!Stopping)
            {
                while (ReceiveQueue.Count != 0)
                {
                    Packet CurrentPacket = ReceiveQueue.Dequeue();

                    if (CurrentPacket != null)
                    {
                        CurrentPacket = (Packet)CurrentPacket.Clone();
                        ProcessOnePacket(CurrentPacket);
                    }
                }
                Thread.Sleep(OperationPeriod);
            }
        }

        /// <summary> Attempts to process a packet. Outputs to log and discards if processing fails. </summary>
        /// <param name="Packet"> The <see cref="Packet"/> to attempt to parse and handle. </param>
        /// <returns> Whether processing was successful. </returns>
        private static bool ProcessOnePacket(Packet Packet)
        {
            try { return Parse.ParseMessage(Packet); }
            catch (Exception Exc)
            {
                Log.Output(Log.Severity.WARNING, Log.Source.NETWORK, "Failed to process packet. Discarding.");
                Log.Exception(Log.Source.NETWORK, Exc);
                return false;
            }
        }
        #endregion

        public class PacketSendResult
        {
            public Packet Packet { get; set; }
            public Status Result { get; set; }

            public PacketSendResult(Packet Packet, Status Result)
            {
                this.Packet = Packet;
                this.Result = Result;
            }

            public enum Status
            {
                /// <summary> Returned when the packet was too far back in the queue, and sending was never attempted. </summary>
                TIMED_OUT,

                /// <summary> Returned when something went wrong while sending the packet. </summary>
                SENDING_FAILED,

                /// <summary>
                /// TCP: Returned when the destination client did not connect in time to receive the packet.
                /// UDP: Returned if the client was not connected at the time of send attempt.
                /// </summary>
                CLIENT_NOT_CONNECTED,

                /// <summary>
                /// TCP: Returned when the connection quality did not achieve a sufficient level in time to send the packet.
                /// UDP: Returned if the connection quality was insufficient at the time of send attempt.
                /// </summary>
                CONNECTION_QUALITY_INSUFFICIENT,

                /// <summary> Returned when the client received the packet (TCP), or it was sent at the client (UDP). </summary>
                SUCCESS
            }
        }

        #region Sending Packets
        /// <summary> TCP: Adds a packet to the queue of packets to be sent. / UDP: Sends the packet in the background. </summary>
        /// <remarks> Returns quickly regardless of result. Use <paramref name="SendResult"/> if you want to know what happens to the packet. </remarks>
        /// <param name="Packet"> The packet to be sent. </param>
        /// <param name="SendResult"> Add a delegate here if you want to know what happens to your <see cref="Packet"/>. </param>
        public static void Send(Packet Packet, Action<PacketSendResult> SendResult = null)
        {
            if (!Initialized) { throw new InvalidOperationException("Cannot use Server before initialization. Call Server.Start()."); }

            if (Packet.IsUDP)
            {
                // Lets the runtime handle UDP sending in the threadpool, which means this method returns quickly, but we don't need to start a thread (very expensive) for every packet.
                // This will get run in the background, and if the user requested it, they will get notified when something happens.
                Task UDPSend = Task.Factory.StartNew(SendUDPTask, new Tuple<Packet, Action<PacketSendResult>>(Packet, SendResult));
            }
            else
            {
                // TODO: Do Checks
                SendQueues[Packet.Endpoint].Enqueue(Packet);
            }
        }

        /// <summary> Used to send UDP packets in a <see cref="Task"/>. </summary>
        /// <remarks> T must be of type <see cref="Tuple"/>, containing <see cref="Packet"/> and <see cref="Action(PacketSendResult)"/>. </remarks>
        private static Action<object> SendUDPTask = (object DataObj) =>
        {
            Tuple<Packet, Action<PacketSendResult>> Data = (Tuple<Packet, Action<PacketSendResult>>)DataObj;
            PacketSendResult Result = SendNow(Data.Item1);
            Data.Item2?.Invoke(Result);
        };

        /// <summary> Immediately sends a packet. Blocks until sending is complete/fails, regardless of protocol. </summary>
        /// <param name="ToSend"> The <see cref="Packet"/> to send now. </param>
        public static PacketSendResult SendNow(Packet ToSend)
        {
            if (!Initialized) { throw new InvalidOperationException("Cannot use Server before initialization. Call Server.Start()."); }
            if (string.IsNullOrEmpty(ToSend.Endpoint)) { throw new InvalidOperationException("Cannot send packet to empty Endpoint."); }
            if (ToSend.ID != Constants.WATCHDOG_FROM_SERVER || OutputWatchdogDebug) { Log.Output(Log.Severity.DEBUG, Log.Source.NETWORK, "Sending packet: " + ToSend); }

            if (!Clients.ContainsKey(ToSend.Endpoint))
            {
                Log.Output(Log.Severity.DEBUG, Log.Source.NETWORK, "Tried to send packet to \"" + ToSend.Endpoint + "\", who has not connected.");
                return new PacketSendResult(ToSend, PacketSendResult.Status.CLIENT_NOT_CONNECTED);
            }
            if (!Clients[ToSend.Endpoint].Connected)
            {
                Log.Output(Log.Severity.DEBUG, Log.Source.NETWORK, "Tried to send packet to \"" + ToSend.Endpoint + "\", who is no longer connected.");
                return new PacketSendResult(ToSend, PacketSendResult.Status.CLIENT_NOT_CONNECTED);
            }
            try
            {
                if (Clients[ToSend.Endpoint].ConnectionQuality < ToSend.MinimumConnectionQuality)
                {
                    Log.Output(Log.Severity.DEBUG, Log.Source.NETWORK, "Tried to send packet to \"" + ToSend.Endpoint + "\", which does not meet the required minimum connection quality (Packet requires " + ToSend.MinimumConnectionQuality + ", client is " + Clients[ToSend.Endpoint].ConnectionQuality + ").");
                    return new PacketSendResult(ToSend, PacketSendResult.Status.CONNECTION_QUALITY_INSUFFICIENT);
                }
                if (ToSend.IsUDP)
                {
                    if (!Clients.ContainsKey(ToSend.Endpoint)) { return new PacketSendResult(ToSend, PacketSendResult.Status.CLIENT_NOT_CONNECTED); }
                    lock (Clients[ToSend.Endpoint])
                    {
                        if (!Clients[ToSend.Endpoint].Connected) { return new PacketSendResult(ToSend, PacketSendResult.Status.CLIENT_NOT_CONNECTED); }
                        byte[] Data = ToSend.GetForSend();
                        UDPListener.Send(Data, Data.Length, Clients[ToSend.Endpoint].EndpointUDP);
                    }
                    if (StorePackets) { PacketsSent.Add(ToSend); }
                }
                else
                {
                    if (!Clients.ContainsKey(ToSend.Endpoint)) { return new PacketSendResult(ToSend, PacketSendResult.Status.CLIENT_NOT_CONNECTED); }
                    lock (Clients[ToSend.Endpoint])
                    {
                        if (!Clients[ToSend.Endpoint].Connected) { return new PacketSendResult(ToSend, PacketSendResult.Status.CLIENT_NOT_CONNECTED); }
                        byte[] Data = ToSend.GetForSend();
                        Clients[ToSend.Endpoint].TCP.GetStream().Write(Data, 0, Data.Length);
                    }
                    if (StorePackets) { PacketsSent.Add(ToSend); }
                }
            }
            catch (Exception Exc)
            {
                Log.Output(Log.Severity.WARNING, Log.Source.NETWORK, "Failed to send packet.");
                Log.Exception(Log.Source.NETWORK, Exc);
                return new PacketSendResult(ToSend, PacketSendResult.Status.SENDING_FAILED);
            }
            return new PacketSendResult(ToSend, PacketSendResult.Status.SUCCESS);
        }

        /// <summary>
        /// Sends packets from the queue.
        /// This must be started on a thread, as it will block until <see cref="Stopping"/> is true.
        /// Assumes that packets will not be removed from <see cref="SendQueues"/> anywhere but inside this method.
        /// </summary>
        private static void SendPackets()
        {
            while (!Stopping)
            {
                foreach (Queue<Packet> SendQueue in SendQueues.Values)
                {
                    while (SendQueue.Count > 0)
                    {
                        Packet ToSend = SendQueue.Dequeue();
                        while (ToSend != null)
                        {
                            try
                            {
                                SendNow(ToSend);
                            }
                            catch (Exception Exc)
                            {
                                Log.Output(Log.Severity.WARNING, Log.Source.NETWORK, "Failed to send packet.");
                                Log.Exception(Log.Source.NETWORK, Exc);
                            }
                        }
                    }
                }
                Thread.Sleep(OperationPeriod);
            }
        }
        #endregion

        private class ScarletClient
        {
            public TcpClient TCP { get; set; }
            public IPEndPoint EndpointTCP { get => (IPEndPoint)this.TCP?.Client.RemoteEndPoint; }
            public IPEndPoint EndpointUDP { get; set; }
            public string Name { get; set; }
            public bool Connected { get; set; }
            public LatencyMeasurementMode LatencyMode { get; set; }
            public byte ScarletVersion { get; set; }
            public byte ConnectionQuality { get; set; }
        }
    }
}
