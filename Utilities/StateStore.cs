using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Scarlet.Utilities
{
    public static class StateStore
    {
        // Extension for file
        private const string FileExtension = ".txt";

        private static FileInfo FileInfo;
        private static Dictionary<string, string> Data;
        public static bool Started { get; private set; }

        /// <summary> Prepares the system for use. Creates a file if it doesn't exist, or reads the configuration if it does. </summary>
        /// <param name="SystemName"> A unique identifier for this application, used as part of the filename to prevent multiple applications on one system from interfering. </param>
        public static void Start(string SystemName)
        {
            if (Started) { return; }
            FileInfo = new FileInfo( "ScarletStore-" + SystemName + FileExtension);
            CheckForBackups();
            if(!File.Exists(FileInfo.FullName)) { File.Create(FileInfo.FullName).Close(); }
            Data = new Dictionary<string, string>();
            foreach (string Line in File.ReadAllLines(FileInfo.FullName))
            {
                Data.Add(Line.Split('=')[0], string.Join("=", Line.Split('=').Skip(1).ToArray()));
            }
            Started = true;
        }

        private static void CheckForBackups()
        {
            FileInfo OldFile = new FileInfo(FileInfo.Name + "-old" + FileExtension);
            if(File.Exists(OldFile.FullName))
            {
                Log.Output(Log.Severity.WARNING, Log.Source.OTHER, "StateStore found a backup file in place, meaning that something went wrong while saving last time. This backup file will now be used.");
                Log.Output(Log.Severity.WARNING, Log.Source.OTHER, "If this keeps happening, there may be a corruption problem. Check the *.txt-corrupt file to see why this may be.");
                if (File.Exists(FileInfo.FullName)) // New file exists
                {
                    FileInfo CorrFile = new FileInfo(FileInfo.Name + "-corrupt" + FileExtension);
                    if(File.Exists(CorrFile.FullName)) { File.Delete(CorrFile.FullName); } // If we already have an old new one, get rid of it.
                    File.Move(FileInfo.FullName, CorrFile.FullName);
                }
                File.Move(OldFile.FullName, FileInfo.FullName); // Replace the potentially corrupt file with the backup
            }
        }

        /// <summary> Saves the configuration to disk. </summary>
        public static void Save()
        {
            List<string> Lines = new List<string>(Data.Count);
            foreach(KeyValuePair<string, string> Item in Data)
            {
                Lines.Add(Item.Key + '=' + Item.Value);
            }
            FileInfo OldFile = new FileInfo(FileInfo.Name + "-old" + FileExtension);
            File.Move(FileInfo.FullName, OldFile.FullName); // Move the current saved file into a backup
            File.WriteAllLines(FileInfo.FullName, Lines); // Save the new version
            File.Delete(OldFile.FullName); // Delete the backup if saving went OK
        }

        /// <summary> Sets the specified proprty internally. Does not change the file, you must use Save() to save changes to file. </summary>
        public static void Set(string Key, string Value)
        {
            lock (Data)
            {
                if (!Data.ContainsKey(Key)) { Data.Add(Key, Value); }
                else { Data[Key] = Value; }
            }
        }

        /// <summary> Gets the specified property, or null if it doesn't exist. </summary>
        public static string Get(string Key)
        {
            lock (Data) { return Data.ContainsKey(Key) ? Data[Key] : null; }
        }

        /// <summary> Gets the specified property, or if it doesn't exist, sets it to the default value and then returns that default value. </summary>
        public static string GetOrCreate(string Key, string DefaultValue)
        {
            lock (Data)
            {
                if (!Data.ContainsKey(Key)) { Data.Add(Key, DefaultValue); }
                return Data[Key];
            }
        }
    }
}
