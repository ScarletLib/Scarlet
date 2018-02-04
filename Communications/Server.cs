﻿using Scarlet.Utilities;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Scarlet.Communications
{
    public static class Server
    {
        private static Dictionary<string, ScarletClient> Clients;

        // Buffer
        private static Dictionary<string, PacketBuffer> SendQueues;
        private static QueueBuffer ReceiveQueue;

        // Port listener
        private static UdpClient UDPListener;
        private static TcpListener TCPListener;

        // Threads
        private static Thread SendThread;
        private static Thread ReceiveThreadTCP, ReceiveThreadUDP, ProcessThread;

        // Running status
        private static bool Initialized = false;
        private static volatile bool Stopping = false;
        private static int ReceiveBufferSize, OperationPeriod;

        // Watchdog
        public static event EventHandler<EventArgs> ClientConnectionChange;
        public static bool OutputWatchdogDebug = false;

        // Packet dumping
        public static bool StorePackets = false;
        public static List<Packet> PacketsReceived, PacketsSent;

        // Packet Priority
        public static bool PriorityQueueEnabled { get; private set; }
        public static PacketPriority DefaultPriority { get; private set; }

        /// <summary> Prepares the server for use, and starts listening for clients. </summary>
        /// <param name="PortTCP"> The port to listen on for clients communicating via TCP. </param>
        /// <param name="PortUDP"> The port to listen on for clients communicating via UDP. </param>
        /// <param name="ReceiveBufferSize"> The size, in bytes, of the receive data buffers. Increase this if your packets are longer than the default. </param>
        /// <param name="OperationPeriod"> The time, in ms, between network operations. If you are sending/receiving a lot of packets, and notice delays, lower this. </param>
        /// <param name="UsePriorityQueue"> If it is ture, packet priority control will be enabled. </param>
        public static void Start(int PortTCP, int PortUDP, int ReceiveBufferSize = 64, int OperationPeriod = 20, bool UsePriorityQueue = false)
        {
            Server.ReceiveBufferSize = ReceiveBufferSize;
            Server.OperationPeriod = OperationPeriod;
            Stopping = false;

            if (!Initialized)
            {
                Log.Output(Log.Severity.DEBUG, Log.Source.NETWORK, "Initializing Server.");
                Log.Output(Log.Severity.DEBUG, Log.Source.NETWORK, "Listening on ports " + PortTCP + " (TCP), and " + PortUDP + " (UDP).");

                Clients = new Dictionary<string, ScarletClient>();
                SendQueues = new Dictionary<string, PacketBuffer>();
                ReceiveQueue = new QueueBuffer();
                PacketsSent = new List<Packet>();
                PacketsReceived = new List<Packet>();
                Initialized = true;
                PriorityQueueEnabled = UsePriorityQueue;

                // Initialize default priority
                if (PriorityQueueEnabled) { DefaultPriority = PacketPriority.MEDIUM; }
                else { DefaultPriority = 0; }

                // Start Handler and listener
                PacketHandler.Start();
                new Thread(new ParameterizedThreadStart(StartThreads)).Start(new Tuple<int, int>(PortTCP, PortUDP));

                // Start watchdog
                WatchdogManager.Start(false);
                WatchdogManager.ConnectionChanged += WatchdogStatusUpdate;
            }
            else { Log.Output(Log.Severity.WARNING, Log.Source.NETWORK, "Attempted to start Server when already started."); }
        }

        /// <summary> Starts all Server threads, then waits for them to terminate. </summary>
        /// <param name="Ports"> Tuple<int, int> of ports, corresponding to (TCP, UDP). </param>
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
            ReceiveThreadUDP.Join();
            ProcessThread.Join();
            SendThread.Join();
            Initialized = false;
        }

        /// <summary> Sends signal to all components of Server to stop, then waits for everything to shut down. </summary>
        public static void Stop()
        {
            Log.Output(Log.Severity.DEBUG, Log.Source.NETWORK, "Stopping Server.");
            Stopping = true;
            WatchdogManager.Stop();

            // This is a meh solution to the WaitForClientsTCP thread not ending until the next client connects.
            TcpClient Dummy = new TcpClient();
            Dummy.Connect(new IPEndPoint(IPAddress.Loopback, ((IPEndPoint)TCPListener.LocalEndpoint).Port));
            Dummy.Close();

            while (Initialized) { Thread.Sleep(50); } // Wait for all threads to stop.
        }

        #region Client Handling
        /// <summary> Waits for incoming TCP clients, then creates a HandleTCPClient thread to interface with each client. </summary>
        /// <param name="ReceivePort"> The port to listen for TCP clients on. Must be int. </param>
        private static void WaitForClientsTCP(object ReceivePort)
        {
            if (!Initialized) { throw new InvalidOperationException("Cannot use Server before initialization. Call Server.Start()."); }
            TCPListener = new TcpListener(new IPEndPoint(IPAddress.Any, (int)ReceivePort));
            TCPListener.Start();
            while (!Stopping)
            {
                TcpClient Client = TCPListener.AcceptTcpClient();
                if (!Stopping) { Log.Output(Log.Severity.DEBUG, Log.Source.NETWORK, "Client is connecting."); }
                // Start sub-threads for every client.
                Thread ClientThread = new Thread(new ParameterizedThreadStart(HandleTCPClient));
                ClientThread.Start(Client);
            }
            TCPListener.Stop();
        }

        /// <summary> Create a new sending buffer for client if the client is new. </summary>
        /// <param name="ClientName"> Name of the client. </param>
        private static void CreateBufferIfClientIsNew(string ClientName)
        {
            lock (SendQueues)
            {
                if (!SendQueues.ContainsKey(ClientName))
                {
                    if (PriorityQueueEnabled)
                        SendQueues.Add(ClientName, new GenericController());
                    else
                        SendQueues.Add(ClientName, new QueueBuffer());
                }
            }
        }

        /// <summary>
        /// Waits for, and receives data from a connected TCP client.
        /// This must be started on a thread, as it will block until CommHandler.Stopping is true, or the client disconnects.
        /// </summary>
        /// <param name="ClientObj"> The client to receive data from. Must be TcpClient. </param>
        private static void HandleTCPClient(object ClientObj)
        {
            TcpClient Client = (TcpClient)ClientObj;
            NetworkStream Receive = Client.GetStream();
            if (!Receive.CanRead)
            {
                Log.Output(Log.Severity.ERROR, Log.Source.NETWORK, "Client connection does not permit reading.");
                throw new Exception("NetworkStream does not support reading");
            }
            // Receive client name.
            String ClientName;
            byte[] DataBuffer = new byte[Math.Max(ReceiveBufferSize, 64)];
            try
            {
                int DataSize = Receive.Read(DataBuffer, 0, DataBuffer.Length);
                if(DataSize == 0)
                {
                    Receive?.Close();
                    if (!Stopping) { Log.Output(Log.Severity.WARNING, Log.Source.NETWORK, "Client disconnected before sending name."); }
                    return;
                }
                else
                {
                    ClientName = null;
                    try { ClientName = UtilData.ToString(DataBuffer.Take(DataSize).ToArray()); } catch { }
                    if (ClientName != null && ClientName.Length > 0)
                    {
                        Log.Output(Log.Severity.INFO, Log.Source.NETWORK, "TCP Client connected with name \"" + ClientName + "\".");
                        lock (Clients)
                        {
                            if (Clients.ContainsKey(ClientName))
                            {
                                Clients[ClientName].TCP = Client;
                                Clients[ClientName].Connected = true;
                            }
                            else
                            {
                                ScarletClient NewClient = new ScarletClient()
                                {
                                    TCP = Client,
                                    Name = ClientName,
                                    Connected = true
                                };
                                Clients.Add(ClientName, NewClient);
                            }
                        }

                        // Create buffer for the client
                        CreateBufferIfClientIsNew(ClientName);
                    }
                    else { Log.Output(Log.Severity.WARNING, Log.Source.NETWORK, "Invalid TCP client name received. Dropping connection."); }
                }
            }
            catch(Exception Exc)
            {
                Log.Output(Log.Severity.WARNING, Log.Source.NETWORK, "Failed to read name from incoming client. Dropping connection.");
                Log.Exception(Log.Source.NETWORK, Exc);
                Receive?.Close();
                return;
            }
            ClientConnChange(new EventArgs());
            WatchdogManager.AddWatchdog(ClientName);

            // Receive data from client.
            DataBuffer = new byte[ReceiveBufferSize];
            while (!Stopping && Clients[ClientName].Connected)
            {
                try
                {
                    int DataSize = Receive.Read(DataBuffer, 0, DataBuffer.Length);
                    Log.Output(Log.Severity.DEBUG, Log.Source.NETWORK, "Received data from client (TCP).");
                    if (DataSize == 0)
                    {
                        Log.Output(Log.Severity.INFO, Log.Source.NETWORK, "Client has disconnected.");
                        lock (Clients[ClientName]) { Clients[ClientName].Connected = false; }
                        break;
                    }
                    if (DataSize >= 5)
                    {
                        byte[] Data = DataBuffer.Take(DataSize).ToArray();
                        IPEndPoint ClientEndpoint = (IPEndPoint)Client.Client.RemoteEndPoint;
                        Packet ReceivedPack = new Packet(new Message(Data), false, ClientName);
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
                        if(Error == 10054) { Clients[ClientName].Connected = false; }
                    }
                    else
                    {
                        Log.Output(Log.Severity.WARNING, Log.Source.NETWORK, "Failed to read data from connected client because of IO exception.");
                        Log.Exception(Log.Source.NETWORK, IOExc);
                    }
                }
                catch (Exception OtherExc)
                {
                    Log.Output(Log.Severity.WARNING, Log.Source.NETWORK, "Failed to read data from connected client.");
                    Log.Exception(Log.Source.NETWORK, OtherExc);
                }
                Thread.Sleep(OperationPeriod);
            }
            lock (Clients) { Clients.Remove(ClientName); }
            Client.Client.Disconnect(true);
            Receive.Close();
            Client.Close();
            ClientConnChange(new EventArgs());
        }

        /// <summary> Initially starts the UDP receiver. </summary>
        /// <param name="ReceivePort"> Port to listen for UDP packets on. Must be int. </param>
        private static void WaitForClientsUDP(object ReceivePort)
        {
            UDPListener = new UdpClient(new IPEndPoint(IPAddress.Any, (int)ReceivePort));
            UDPListener.BeginReceive(HandleUDPData, UDPListener);
        }

        /// <summary> Processes incoming UDP packet, then starts the listener again. </summary>
        private static void HandleUDPData(IAsyncResult Result)
        {
            UdpClient Listener;
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
            catch(Exception Exc)
            {
                Log.Output(Log.Severity.WARNING, Log.Source.NETWORK, "Failed to receive UDP data.");
                Log.Exception(Log.Source.NETWORK, Exc);
                return;
            }
            if (Data.Length == 0) // TODO: Can this happen?
            {
                Log.Output(Log.Severity.INFO, Log.Source.NETWORK, "Client has disconnected.");
                if (ClientName != null)
                {
                    lock (Clients[ClientName]) { Clients[ClientName].Connected = false; }
                }
            }
            else
            {
                if (ClientName == null) // New client
                {
                    try { ClientName = UtilData.ToString(Data); } catch { }
                    if (ClientName != null && ClientName.Length > 0)
                    {
                        Log.Output(Log.Severity.INFO, Log.Source.NETWORK, "UDP Client connected with name \"" + ClientName + "\".");
                        lock (Clients)
                        {
                            if (Clients.ContainsKey(ClientName))
                            {
                                Clients[ClientName].EndpointUDP = ReceivedEndpoint;
                                Clients[ClientName].Connected = true;
                            }
                            else
                            {
                                ScarletClient NewClient = new ScarletClient()
                                {
                                    EndpointUDP = ReceivedEndpoint,
                                    Name = ClientName,
                                    Connected = true
                                };
                                Clients.Add(ClientName, NewClient);
                            }
                        }

                        // Create buffer for the client
                        CreateBufferIfClientIsNew(ClientName);
                    }
                    else { Log.Output(Log.Severity.WARNING, Log.Source.NETWORK, "UDP Client sent invalid name upon connecting."); }
                }
                else // Existing client
                {
                    Packet ReceivedPack = new Packet(new Message(Data), false, ClientName);
                    ReceiveQueue.Enqueue(ReceivedPack);

                    if (StorePackets) { PacketsReceived.Add(ReceivedPack); }
                }
            }
            Listener.BeginReceive(HandleUDPData, Listener);
        }

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

        /// <summary> Gets the TCP and UDP endpoints of the specified client by client name. </summary>
        public static IPEndPoint[] GetEndpoints(string ClientName)
        {
            if (Clients.ContainsKey(ClientName))
            {
                return new IPEndPoint[] { Clients[ClientName].EndpointTCP , Clients[ClientName].EndpointUDP };
            }
            else { return null; }
        }

        private static void ClientConnChange(EventArgs Event) { ClientConnectionChange?.Invoke("Server", Event); }

        /// <summary> Receives watchdog events for clients disconnecting/reconnecting. </summary>
        public static void WatchdogStatusUpdate(object Sender, EventArgs Event)
        {
            ConnectionStatusChanged WatchdogEvent = (ConnectionStatusChanged)Event;
            if(Clients.ContainsKey(WatchdogEvent.StatusEndpoint)) { Clients[WatchdogEvent.StatusEndpoint].Connected = WatchdogEvent.StatusConnected; }
        }

        /// <summary> Gets a list of clients. Clients may not be connected, or partially connected. </summary>
        public static List<string> GetClients() { return Clients.Keys.ToList(); }
        #endregion

        #region Processing
        /// <summary>
        /// Pushes received packets through to Parse for processing.
        /// This must be started on a thread, as it will block until CommHandler.Stopping is true.
        /// Assumes that packets will not be removed from ReceiveQueue anywhere but inside this method.
        /// </summary>
        private static void ProcessPackets()
        {
            if (!Initialized) { throw new InvalidOperationException("Cannot use Server before initialization. Call Server.Start()."); }
            while (!Stopping)
            {
                Packet CurrentPacket = ReceiveQueue.Dequeue();

                if (CurrentPacket != null)
                {
                    CurrentPacket = (Packet)CurrentPacket.Clone();
                    ProcessOnePacket(CurrentPacket);
                }
                Thread.Sleep(OperationPeriod);
            }
        }

        /// <summary> Attempts to process a packet. Outputs to log and discards if processing fails. </summary>
        /// <returns> Whether processing was successful. </returns>
        private static bool ProcessOnePacket(Packet Packet)
        {
            try
            {
                return Parse.ParseMessage(Packet);
            }
            catch (Exception Exc)
            {
                Log.Output(Log.Severity.WARNING, Log.Source.NETWORK, "Failed to process packet. Discarding.");
                Log.Exception(Log.Source.NETWORK, Exc);
                return false;
            }
        }
        #endregion

        #region Sending
        /// <summary> Adds a packet to the queue of packets to be sent. </summary>
        /// <param name="Packet"> The packet to be sent. </param>
        /// <param name="Priority"> Priority of packet. </param>
        public static void Send(Packet Packet, PacketPriority Priority = PacketPriority.USE_DEFAULT)
        {
            if (!Initialized) { throw new InvalidOperationException("Cannot use Server before initialization. Call Server.Start()."); }

            if (Packet.IsUDP)
            {
                // Send UDP in new thread
                Thread PacketSender = new Thread(new ParameterizedThreadStart(SendNowGen));
                PacketSender.Start(Packet);
            }
            else
            {
                // Use default priority if needed
                if (Priority == PacketPriority.USE_DEFAULT) { Priority = DefaultPriority; }

                // Add to queue
                SendQueues[Packet.Endpoint].Enqueue(Packet, (int) Priority);
            }
        }

        /// <summary> Used for threaded applications. </summary>
        /// <param name="Packet"> A Packet, which will be passed to SendNow(Packet). </param>
        private static void SendNowGen(object Packet)
        {
            SendNow((Packet)Packet);
        }

        /// <summary> Immediately sends a packet. Blocks until sending is complete, regardless of protocol. </summary>
        public static void SendNow(Packet ToSend)
        {
            if (!Initialized) { throw new InvalidOperationException("Cannot use Server before initialization. Call Server.Start()."); }
            if (ToSend.Data.ID != Constants.WATCHDOG_PING || OutputWatchdogDebug)
            {
                Log.Output(Log.Severity.DEBUG, Log.Source.NETWORK, "Sending packet: " + ToSend);
            }
            if (!Clients.ContainsKey(ToSend.Endpoint))
            {
                WatchdogManager.RemoveWatchdog(ToSend.Endpoint);
                Log.Output(Log.Severity.WARNING, Log.Source.NETWORK, "Tried to send packet to unknown client. Dropping.");
                return;
            }
            if(!Clients[ToSend.Endpoint].Connected)
            {
                Log.Output(Log.Severity.WARNING, Log.Source.NETWORK, "Cannot send packet to client that is not connected.");
                return;
            }
            try
            {
                if (ToSend.IsUDP)
                {
                    if (!Clients.ContainsKey(ToSend.Endpoint)) { throw new InvalidOperationException("Cannot send packet to client that is not connected."); }
                    lock (Clients[ToSend.Endpoint])
                    {
                        byte[] Data = ToSend.GetForSend();
                        UDPListener.Send(Data, Data.Length, Clients[ToSend.Endpoint].EndpointUDP);
                        if (StorePackets) { PacketsSent.Add(ToSend); }
                    }
                }
                else
                {
                    if (!Clients.ContainsKey(ToSend.Endpoint)) { throw new InvalidOperationException("Cannot send packet to client that is not connected."); }
                    lock (Clients[ToSend.Endpoint])
                    {
                        byte[] Data = ToSend.GetForSend();
                        Clients[ToSend.Endpoint].TCP.GetStream().Write(Data, 0, Data.Length);
                        if (StorePackets) { PacketsSent.Add(ToSend); }
                    }
                }
            }
            catch(Exception Exc)
            {
                Log.Output(Log.Severity.WARNING, Log.Source.NETWORK, "Failed to send packet.");
                Log.Exception(Log.Source.NETWORK, Exc);
            }
        }

        /// <summary>
        /// Sends an error packet to the given 
        /// endpoint via TCP.
        /// Adds Error packet to packet send queue
        /// </summary>
        /// <param name="ErrorPacketID">ID for the Error Packet</param>
        /// <param name="Endpoint">Endpoint to send the packet</param>
        /// <param name="ErrorCode">Error code for the indicated error</param>
        public static void SendError(byte ErrorPacketID, string Endpoint, int ErrorCode)
        {
            Packet ErrorPacket = new Packet(ErrorPacketID, false, Endpoint);
            ErrorPacket.AppendData(UtilData.ToBytes(ErrorCode));
            Send(ErrorPacket);
        }

        /// <summary>
        /// Sends packets from the queue.
        /// This must be started on a thread, as it will block until CommHandler.Stopping is true.
        /// Assumes that packets will not be removed from SendQueue anywhere but inside this method.
        /// </summary>
        private static void SendPackets()
        {
            if (!Initialized) { throw new InvalidOperationException("Cannot use CommHandler before initialization. Call CommHandler.Start()."); }
            while (!Stopping)
            {
                foreach (PacketBuffer SendQueue in SendQueues.Values)
                {
                    Packet ToSend = SendQueue.Dequeue();
                    if (ToSend != null)
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

                // Wait for operation period
                Thread.Sleep(OperationPeriod);
            }
        }
        #endregion

        private class ScarletClient
        {
            public TcpClient TCP;
            public IPEndPoint EndpointTCP { get { return (IPEndPoint)TCP.Client.RemoteEndPoint; } }
            public IPEndPoint EndpointUDP;
            public string Name;
            public bool Connected;
        }
    }
}