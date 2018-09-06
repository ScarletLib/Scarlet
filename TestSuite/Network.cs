using Scarlet.Communications;
using Scarlet.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Scarlet.TestSuite
{
    public class Network
    {
        public static void Start(string[] args)
        {
            if (args.Length < 2) { TestMain.ErrorExit("network command requires functionality to test."); }
            switch (args[1].ToLower())
            {
                case "client":
                    {
                        Log.SetSingleOutputLevel(Log.Source.NETWORK, Log.Severity.DEBUG);
                        Client.TraceLogging = true;
                        string IP = (args.Length > 2) ? args[2] : "127.0.0.1";
                        ushort PortTCP = (ushort)(args.Length > 3 && ushort.TryParse(args[3], out _) ? ushort.Parse(args[3]) : 1765);
                        ushort PortUDP = (ushort)(args.Length > 4 && ushort.TryParse(args[4], out _) ? ushort.Parse(args[4]) : 2765);
                        Client.Start("TestSuite", IP, PortTCP, PortUDP);
                        while (!Console.KeyAvailable) { Thread.Sleep(50); }
                        Client.Stop();
                        return;
                    }
                case "server":
                    {
                        Log.SetSingleOutputLevel(Log.Source.NETWORK, Log.Severity.DEBUG);
                        Server.TraceLogging = true;
                        ushort PortTCP = (ushort)(args.Length > 2 && ushort.TryParse(args[2], out _) ? ushort.Parse(args[2]) : 1765);
                        ushort PortUDP = (ushort)(args.Length > 3 && ushort.TryParse(args[3], out _) ? ushort.Parse(args[3]) : 2765);
                        Server.Start(PortTCP, PortUDP);
                        while (!Console.KeyAvailable) { Thread.Sleep(50); }
                        Server.Stop();
                        return;
                    }
                default:
                    {
                        Log.Output(Log.Severity.ERROR, Log.Source.GUI, "Invalid input.");
                        return;
                    }
            }
        }
    }
}
