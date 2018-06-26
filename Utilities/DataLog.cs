using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Scarlet.Utilities
{
    public class DataLog
    {
        private const string LogFilesLocation = "DataLogs";

        private string Filename;
        private StreamWriter Writer;

        private bool FileCreated = false;
        private bool HeaderCreated = false;
        private bool FileOpened = false;

        public string LogFilePath { get; private set; }
        public bool AutoFlush;

        /// <summary>
        /// Constructs a DataLog system, which is used to log given data into a CSV file.
        /// Particularly useful for applications like logging sensor data over time.
        /// </summary>
        /// <param name="Filename">Name of the file to log to (a date and timestamp will be directly appended to this).</param>
        /// <param name="AutoFlush">If true, output will be flushed on every call to Output(), otherwise use Flush() to manually flush.</param>
        public DataLog(string Filename, bool AutoFlush = true)
        {
            this.Filename = Filename;
            this.AutoFlush = AutoFlush;
            CreateLogFile();
        }

        /// <summary> Creates a log file with a unique filename determined by a current timestamp. </summary>
        private void CreateLogFile()
        {
            if (!this.FileCreated)
            {
                string FileName = this.Filename + DateTime.Now.ToString("yy-MM-dd-hh-mm-ss-tt");
                if (!Directory.Exists(@LogFilesLocation)) { Directory.CreateDirectory(@LogFilesLocation); }
                string[] Files = Directory.GetFiles(@LogFilesLocation, "*.csv");
                int Iterations = 0;
                while (Files.Contains(FileName + ".csv"))
                {
                    FileName += "_" + Iterations.ToString();
                    Iterations++;
                }
                FileName += ".csv";
                LogFilePath = Path.Combine(LogFilesLocation, FileName);
                this.Writer = new StreamWriter(@LogFilePath) { AutoFlush = this.AutoFlush };
                Log.Output(Log.Severity.INFO, Log.Source.SENSORS, "DataLog created at \"" + LogFilePath + "\".");
                this.FileCreated = true;
                this.FileOpened = true;
            }
        }

        /// <summary> Outputs these data points to CSV. If this is the first one, these structures will be used to build the header. </summary>
        /// <remarks> You need to keep the order and number of elements the same each call. There is no internal sorting or checking. Things are output to CSV in the order given, so if the order is incorrect, data will be in the wrong column. </remarks>
        /// <param name="Data"> The data to output. Provide as much as you'd like. </param>
        public void Output(params DataUnit[] Data)
        {
            CheckOpened();
            StringBuilder Line = new StringBuilder();
            if (!this.HeaderCreated) { CreateHeader(Data); }
            foreach (DataUnit Unit in Data)
            {
                foreach (object Value in Unit.Values)
                {
                    Line.Append(Value.ToString());
                    Line.Append(',');
                }
            }
            Line.Remove(Line.Length - 1, 1); // Remove the last comma
            this.Writer.WriteLine(Line);
        }

        private void CreateHeader(DataUnit[] Data)
        {
            CheckOpened();   
            StringBuilder Line = new StringBuilder();
            foreach(DataUnit Unit in Data)
            {
                foreach(string Key in Unit.Keys)
                {
                    Line.AppendFormat("{0}.{1}.{2},", (Unit.System ?? "UntitledSystem"), Unit.Origin, Key);
                }
            }
            Line.Remove(Line.Length - 1, 1); // Remove the last comma
            this.Writer.WriteLine(Line);
            this.HeaderCreated = true;
        }

        /// <summary> Manually flushes the output to the file. If AutoFlush is true, this is ignored. </summary>
        public void Flush() { if (!AutoFlush && FileOpened) { this.Writer.Flush(); } }

        private void CheckOpened() { if (!FileOpened) { throw new Exception("Attempting to write to closed file..."); } }

        /// <summary> Deletes all DataLog CSV files with the same given Filename except any that are currently in-use. </summary>
        public void DeleteAll()
        {
            Log.Output(Log.Severity.INFO, Log.Source.SENSORS, "Deleting old DataLog files...");
            try
            {
                IEnumerable<string> FileList = Directory.EnumerateFiles(LogFilesLocation);
                foreach (string FilePath in FileList)
                {
                    if (FilePath.StartsWith(LogFilesLocation + "\\" + Filename) && FilePath.EndsWith(".csv")) { File.Delete(FilePath); }
                }
            }
            catch (IOException) { } // Do Nothing if file is in use or directory doesn't exist
        }

        /// <summary> Flush the output and stop writing. </summary>
        public void CloseFile()
        {
            this.Writer.Flush();
            this.Writer.Close();
            this.FileOpened = false;
        }

    }

    public class DataUnit : IEnumerable
    {
        private Dictionary<string, object> Data = new Dictionary<string, object>();
        public Dictionary<string, object>.KeyCollection Keys { get => Data.Keys; }
        public Dictionary<string, object>.ValueCollection Values { get => Data.Values; }
        public string System;
        public string Origin;

        /// <param name="SourceType"> The source type. Something like "MAX31855" </param>
        public DataUnit(string SourceType)
        {
            if (SourceType.Contains('.') || SourceType.Contains(',') || SourceType.Contains(';') || SourceType.Contains('\n')) { throw new Exception("SourceType cannot contain these characters: {. , ; [newline]}"); }
            this.Origin = SourceType;
        }

        /// <summary> Sets the name of the system that this data source belongs to. Used in the CSV header. </summary>
        /// <remarks> This returns itself, so that you can do something like this: DataLog.Output(MySensor.GetDataUnit().SetSystem("GNDTemp")); </remarks>
        /// <param name="SystemName"> The name of the system where this data source belongs. Something like "GroundTemperature" </param>
        /// <returns> Itself, so that you can inline this call. See remarks. </returns>
        public DataUnit SetSystem(string SystemName)
        {
            if (SystemName.Contains('.') || SystemName.Contains(',') || SystemName.Contains(';') || SystemName.Contains('\n')) { throw new Exception("SourcePurpose cannot contain these characters: {. , ; [newline]}"); }
            this.System = SystemName;
            return this;
        }

        public void Add<DataType>(string key, DataType value)// where DataType : class
        {
            this.Data.Add(key, value);
        }

        public DataType GetValue<DataType>(string key)// where DataType : class
        {
            return (DataType)this.Data[key];
        }

        public IEnumerator GetEnumerator() => this.Data.GetEnumerator();
    }
}
