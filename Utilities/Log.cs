using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization;

namespace Scarlet.Utilities
{
    public static class Log
    {
        // Override these in your implementation.
        // OutputType determines which system you see output from.
        // OutputLevel determines the minimum severity required for a message to be output.

        private static WriteDestination P_Destination = WriteDestination.CONSOLE;

        /// <summary> Sets where log output is directed. </summary>
        public static WriteDestination Destination 
        {
            get { return P_Destination; }
            set 
            { 
                P_Destination = value;
                if (value == WriteDestination.ALL || value == WriteDestination.FILE) { CreateLogFile(); }
            }
        }

        private static Severity[] OutputLevels = new Severity[Enum.GetNames(typeof(Source)).Length];

        /// <summary> Sets all sources to have this minimum severity for output. </summary>
        public static void SetGlobalOutputLevel(Severity OutputLevel)
        {
            for (int i = 0; i < Log.OutputLevels.Length; i++) { Log.OutputLevels[i] = OutputLevel; }
        }

        /// <summary>
        /// Sets <param name="Source" /> to have minimum output level <param name="OutputLevel" />.
        /// </summary>
        public static void SetSingleOutputLevel(Source Source, Severity OutputLevel) { Log.OutputLevels[(int)Source] = OutputLevel; }

        /// <summary> Replaces output levels with the given array. Length must be equal. </summary>
        public static void SetAllOutputLevels(Severity[] OutputLevels)
        {
            if (OutputLevels.Length == Log.OutputLevels.Length) { Log.OutputLevels = OutputLevels; }
        }

        /// <summary> The list of systems that an error can originate from, used by Log.Error(). Override this in your implementation. </summary>
        public static string[] SystemNames;

        /// <summary> A human-readable, concise description for every possible error, used by Log.Error(). Override this in your implementation. </summary>
        public static string[][] ErrorCodes;

        private static StreamWriter LogFile; // Location of the Log File. Set in Begin()
        private const string LogFilesLocation = "Logs"; // Folder to hold log files
        private static bool FileCreated;

        private static object ConsoleLock = new object();
        private static object FileLock = new object();

        private static ObjectIDGenerator IDGen = new ObjectIDGenerator();

        /// <summary> Outputs a general log message if configured to output this type of message. </summary>
        /// <param name="Severity"> How severe this message is. This partially determines if it is output. </param>
        /// <param name="Src"> The system where this log entry is originating. </param>
        /// <param name="Message"> The actual log entry to output. </param>
        public static void Output(Severity Sev, Source Src, string Message)
        {
            if(OutputLevels[(int)Src] <= Sev) { ForceOutput(Sev, Src, Message); }
        }

        /// <summary>
        /// Delegate to defer Output message creation until the message actually needs
        /// to be logged.
        /// </summary>
        /// <returns>The message to log</returns>
        public delegate string MessageProvider();

        /// <summary> Delegate version of Output. This should only be used if
        /// message creation would be more expensive than the creation of the 
        /// delegate MessageProvider.
        /// </summary>
        /// <param name="Severity"> How severe this message is. </param>
        /// <param name="Src"> The system where this log entry is originating. </param>
        /// <param name="Message"> Delegate to provide message. </param>
        public static void Output(Severity Sev, Source Src, MessageProvider message) {
            //repeat this code because the delegate shouldn't be called unless its log level is enabled
            if (OutputLevels[(int)Src] <= Sev) { ForceOutput(Sev, Src, message()); }
        }

        /// <summary> Same as Output, but ignores logging settings and always outputs. </summary>
        public static void ForceOutput(Severity Sev, Source Src, string Message)
        {
            switch (Sev)
            {
                case Severity.DEBUG:
                    Message = "[DBG] " + Message;
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case Severity.INFO:
                    Message = "[INF] " + Message;
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case Severity.WARNING:
                    Message = "[WRN] " + Message;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case Severity.ERROR:
                    Message = "[ERR] " + Message;
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case Severity.FATAL:
                    Message = "[FAT] " + Message;
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.BackgroundColor = ConsoleColor.Red;
                    break;
            }
            Message = "[" + DateTime.Now.ToLongTimeString() + "] " + Message;
            WriteLine(Message);
            Console.ResetColor();
        }

        public static void Trace(object Sender, string Message)
        {
            Message = string.Format("[{0}] [TRC] [{1}#{2}] {3}", DateTime.Now.ToLongTimeString(), Sender.GetType().Name, IDGen.GetId(Sender, out bool IgnoreMe), Message);
            Console.ForegroundColor = ConsoleColor.Gray;
            WriteLine(Message);
            Console.ResetColor();
        }

        /// <summary>
        /// Outputs an Exception objet in the form of a stack trace to the console.
        /// Exceptions are always output.
        /// </summary>
        /// <param name="Src"> The system that this Exception is originating from. </param>
        /// <param name="Ex"> The Exception object. </param>
        public static void Exception(Source Src, Exception Ex)
        {
            string Prefix = "[" + DateTime.Now.ToLongTimeString() + "] [EXC] ";
            string[] Lines = Ex.ToString().Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            Lines = Array.ConvertAll(Lines, Line => (Prefix + Line));
            Lines.ToList().ForEach(WriteLine);
        }

        /// <summary> Outputs a defined error code to the console. </summary>
        /// <param name="Error"> The error code, in standard form. E.g. 0x0000 for all OK. </param>
        public static void Error(short Error)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            byte System = (byte)(Error >> 8);
            byte Code = (byte)(Error & 0x00FF);
            WriteLine("[" + DateTime.Now.ToLongTimeString() + "] [ERR] [0x" + Error.ToString("X4") + "] [" + SystemNames[System] + "] " + ErrorCodes[System][Code]);
            Console.ResetColor();
        }

        /// <summary> Outputs a line to the console specifying the logging settings. </summary>
        public static void Begin()
        {
            StringBuilder Str = new StringBuilder();
            Str.Append('[');
            Str.Append(DateTime.Now.ToLongTimeString());
            Str.Append("] [DBG] Logging started.");
            Str.Append(". It is ");
            Str.Append(DateTime.Now.ToLongDateString());
            Str.Append(' ');
            Str.Append(DateTime.Now.ToLongTimeString());
            Str.Append('.');

            WriteLine(Str.ToString());
        }

        /// <summary> Creates a log file with a unique filename determined by a current timestamp </summary>
        private static void CreateLogFile()
        {
            if (!FileCreated)
            {
                string FileName = "ScarletLog-" + DateTime.Now.ToString("yy-MM-dd-hh-mm-ss-tt");
                if (!Directory.Exists(@LogFilesLocation)) { Directory.CreateDirectory(@LogFilesLocation); }
                string[] Files = Directory.GetFiles(@LogFilesLocation, "*.log");
                int Iterations = 0;
                while (Files.Contains(FileName + ".log"))
                {
                    FileName += "_" + Iterations.ToString();
                    Iterations++;
                }
                FileName += ".log";
                string FileLocation = Path.Combine(LogFilesLocation, FileName);
                LogFile = new StreamWriter(@FileLocation);
                FileCreated = true;
            }
        }

        /// <summary> Stops logging to file (future output directed to console), then saves and closes the log file. </summary>
        public static void StopFileLogging() 
        {
            Destination = WriteDestination.CONSOLE; // If anything else left to print, write it to the console
            LogFile.Flush();
            LogFile.Close();
        }

        /// <summary>
        /// Writes a message to the console.
        /// Does not carriage return/line feed.
        /// </summary>
        /// <param name="Message"> Message to write. </param>
        private static void Write(string Message)
        {
            if (Log.Destination == WriteDestination.ALL || Log.Destination == WriteDestination.CONSOLE) 
            {
                lock (ConsoleLock) { Console.Write(Message); }
            }
            if (Log.Destination == WriteDestination.ALL || Log.Destination == WriteDestination.FILE)
            {
                lock (FileLock) { LogFile.Write(Message); }
            }
        }

        /// <summary> Writes a string and applies a new line in the console with a carriage return and line feed. </summary>
        /// <param name="Message"> Message to write. </param>
        private static void WriteLine(string Message) { Write(Message + "\r\n"); }

        /// <summary> Write destination. </summary>
        public enum WriteDestination { CONSOLE, FILE, ALL }

        /// <summary> The subsystem where the error occured, for use in output filtering. </summary>
        public enum Source { ALL, MOTORS, NETWORK, GUI, SENSORS, CAMERAS, SUBSYSTEM, HARDWAREIO, OTHER }

        /// <summary> How important the log entry is. </summary>
        /// Debug = Used for program flow debugging and light troubleshooting (e.g. "Starting distance sensor handler")
        /// Information = Used for troubleshooting (e.g. "Distance sensor detected successfully")
        /// Warning = When an issue arises, but functionality sees little to no impact (e.g. "Distance sensor took longer than expected to find value")
        /// Error = When an issue arises that causes significant loss of functionality to one system (e.g. "Distance sensor unreachable")
        /// Fatal = When an issue arises that causes loss of functionality to multiple systems or complete shutdown (e.g. Current limit reached, shutting down all motors)
        public enum Severity { DEBUG, INFO, WARNING, ERROR, FATAL }

    }
}
