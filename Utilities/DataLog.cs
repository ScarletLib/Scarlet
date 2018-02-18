using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Scarlet.Utilities
{
    public class DataLog
    {
        private const string LogFilesLocation = "DataLogs";

        private string Filename;
        private StreamWriter Writer;

        private bool FileCreated = false;
        private bool HeaderCreated = false;

        public DataLog(string Filename)
        {
            this.Filename = Filename;
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
                string FileLocation = Path.Combine(LogFilesLocation, FileName);
                this.Writer = new StreamWriter(@FileLocation);
                this.FileCreated = true;
            }
        }

        /// <summary> Outputs these data points to CSV. If this is the first one, these structures will be used to build the header. </summary>
        /// <remarks> You need to keep the order and number of elements the same each call. There is no internal sorting or checking. Things are output to CSV in the order given, so if the order is incorrect, data will be in the wrong column. </remarks>
        /// <param name="Data"> The data to output. Provide as much as you'd like. </param>
        public void Output(params DataUnit[] Data)
        {
            if(!this.HeaderCreated) { CreateHeader(Data); }
            foreach (DataUnit Unit in Data)
            {
                foreach (object Value in Unit.Values)
                {
                    this.Writer.Write(Value.ToString() + ",");
                }
            }
            this.Writer.WriteLine(';');
        }

        private void CreateHeader(DataUnit[] Data)
        {
            foreach(DataUnit Unit in Data)
            {
                foreach(string Key in Unit.Keys)
                {
                    this.Writer.Write(String.Format("{0}.{1}.{2},", Unit.Name, Unit.Origin, Key));
                }
            }
            this.Writer.WriteLine(';');
            this.HeaderCreated = true;
        }
    }

    public class DataUnit : IEnumerable
    {
        private Dictionary<string, object> Data = new Dictionary<string, object>();
        public Dictionary<string, object>.KeyCollection Keys { get => Data.Keys; }
        public Dictionary<string, object>.ValueCollection Values { get => Data.Values; }
        public string Name;
        public string Origin;

        /// <param name="SourcePurpose"> The application of the source. Something like "Ground Temperature Sensor". Used to differentiate two of the same data sources. </param>
        /// <param name="SourceType"> The source type. Something like "MAX31855" </param>
        public DataUnit(string SourcePurpose, string SourceType)
        {
            if (SourcePurpose.Contains('.') || SourcePurpose.Contains(',') || SourcePurpose.Contains(';') || SourcePurpose.Contains('\n') { throw new Exception("SourcePurpose cannot contain these characters: {. , ; [newline]}"); }
            if (SourceType.Contains('.') || SourceType.Contains(',') || SourceType.Contains(';') || SourceType.Contains('\n') { throw new Exception("SourceType cannot contain these characters: {. , ; [newline]}"); }
            this.Name = SourcePurpose;
            this.Origin = SourceType;
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
