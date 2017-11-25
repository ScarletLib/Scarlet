using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Scarlet.Utilities
{
    public static class StateStore
    {
        private static string FileName;
        private static Dictionary<string, string> Data;
        public static bool Started { get; private set; }

        //TODO: Make this thread-safe.

        /// <summary> Prepares the system for use. Creates a file if it doesn't exist, or reads the configuration if it does. </summary>
        /// <param name="SystemName"> A unique identifier for this application, used as part of the filename to prevent multiple applications on one system from interfering. </param>
        public static void Start(string SystemName)
        {
            FileName = "ScarletStore-" + SystemName + ".txt";
            if(!File.Exists(FileName)) { File.Create(FileName).Close(); }
            Data = new Dictionary<string, string>();
            foreach (string Line in File.ReadAllLines(FileName))
            {
                Data.Add(Line.Split('=')[0], string.Join("=", Line.Split('=').Skip(1).ToArray()));
            }
            Started = true;
        }

        /// <summary> Saves the configuration to disk. </summary>
        public static void Save()
        {
            List<string> Lines = new List<string>(Data.Count);
            foreach(KeyValuePair<string, string> Item in Data)
            {
                Lines.Add(Item.Key + '=' + Item.Value);
            }
            string OldFile = FileName + "old";
            File.Move(FileName, OldFile);
            File.WriteAllLines(FileName, Lines);
            File.Delete(OldFile);
        }

        /// <summary> Sets the specified proprty internally. Does not change the file, you must use Save() to save changes to file. </summary>
        public static void Set(string Key, string Value)
        {
            if (!Data.ContainsKey(Key)) { Data.Add(Key, Value); }
            else { Data[Key] = Value; }
        }

        /// <summary> Gets the specified property, or null if it doesn't exist. </summary>
        public static string Get(string Key) { return Data.ContainsKey(Key) ? Data[Key] : null; }

        /// <summary> Gets the specified property, or if it doesn't exist, sets it to the default value and then returns that default value. </summary>
        public static string GetOrCreate(string Key, string DefaultValue)
        {
            if (!Data.ContainsKey(Key)) { Data.Add(Key, DefaultValue); }
            return Data[Key];
        }
    }
}
