using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scarlet.Utilities
{
    public class PlatformInfo
    {
        /// <summary> The general class of device, such as <c>"RaspberryPi"</c> or <c>"PC"</c>. </summary>
        public static string Platform { get; private set; }

        /// <summary> Info about the hardware, such as <c>"Pi 3 Model B"</c> or <c>"Laptop"</c>. </summary>
        public static string Hardware { get; private set; }

        /// <summary> Info about the OS, such as <c>"Windows 10.0.17134.112"</c> or <c>"Debian 8.0"</c>. </summary>
        public static string OS { get; private set; }

        /// <summary> Gets the version number for the OS. For example <c>"10.0.17134.112"</c> or <c>"8.0"</c> </summary>
        public static string OSVersionNumber { get; private set; }

        /// <summary> Gets the version of the OS. </summary>
        public static OperatingSystems OSVersion { get; private set; }

        private const string UnsupportedDistroName = "UnsupportedUnix";

        static PlatformInfo()
        {
            
        }

        private static void GetOSInformation()
        {
            OperatingSystem OS = Environment.OSVersion;

            // Determine OS Version number
            OSVersionNumber = OS.VersionString;

            // Determine OS Version
            string Distro = ""; // Only used if platform is unix-based

            if (OS.Platform == PlatformID.Win32NT) { OSVersion = OperatingSystems.Windows; }
            else if (OS.Platform == PlatformID.MacOSX) { OSVersion = OperatingSystems.MacOS; }
            else if (OS.Platform == PlatformID.Unix)
            {
                Distro = QueryUnixDistribution();
                if (Distro == "Debian") { OSVersion = OperatingSystems.Debian; }
                else if (Distro == "Ubuntu") { OSVersion = OperatingSystems.Ubuntu; }
                else { OSVersion = OperatingSystems.Unsupported; }
            }
            else { OSVersion = OperatingSystems.Unsupported; }

            // Determine OS property
            if (OSVersion == OperatingSystems.Unsupported && OS.Platform == PlatformID.Unix) { PlatformInfo.OS = "[Unsupported] " + QueryUnixDistribution(); }
            else if (OSVersion == OperatingSystems.Unsupported) { PlatformInfo.OS = "[Unsupported] " + Enum.GetName(typeof(PlatformID), OS.Platform); }
            else { PlatformInfo.OS = Enum.GetName(typeof(OperatingSystems), OSVersion); }
            PlatformInfo.OS += " " + OSVersionNumber;
            if (OSVersion == OperatingSystems.Unsupported) { Log.Output(Log.Severity.WARNING, Log.Source.OTHER, "PLATFORM NOT SUPPORTED: " + PlatformInfo.OS); }
        }

        private static string QueryUnixDistribution()
        {
            try
            {
                IEnumerable<string> Stream = File.ReadLines("/etc/lsb-release");

                // Determine the distribution on the file. If it is not in the OperatingSystems enum, it is unsupported
                return Stream.First().Split('=')[1];
            } catch { return UnsupportedDistroName; }
        }

        // Info from: http://ozzmaker.com/check-raspberry-software-hardware-version-command-line/
        private static void ReadPiVersion()
        {
            string[] Lines = File.ReadAllLines("/proc/cpuinfo");
            string Revision = Lines.Where(x => x.StartsWith("Revision")).First();
            Revision = Revision.Substring(Revision.IndexOf(':') + 1).ToLower();
            switch (Revision)
            {
                case "0002": Hardware = "Pi 1; Model B; Revision 1"; break;
                case "0003": Hardware = "Pi 1; Model B; Revision 1 + ECN0001"; break;

                case "0004":
                case "0005":
                case "0006": Hardware = "Pi 1; Model B; Revision 2"; break;

                case "0007":
                case "0008":
                case "0009": Hardware = "Pi 1; Model A"; break;

                case "000d":
                case "000e":
                case "000f": Hardware = "Pi 1; Model B; Revision 2"; break; // This one has 512MB of RAM, the one above has 256MB.

                case "0010": Hardware = "Pi 1; Model B+"; break;
                case "0011": Hardware = "Compute Module"; break;
                case "0012": Hardware = "Pi 1; Model A+"; break;

                case "a01041":
                case "a21041": Hardware = "Pi 2; Model B"; break;

                case "900092": 
                case "900093": Hardware = "Pi Zero"; break;

                case "a02082":
                case "a22082": Hardware = "Pi 3; Model B"; break;

                case "9000c1": Hardware = "Pi Zero W"; break;
                default: Hardware = "Unknown; Revision " + Revision; break;
            }
        }

        public enum OperatingSystems
        {
            Windows,
            Debian,
            Ubuntu,
            MacOS,
            Unsupported,
        }
    }
}
