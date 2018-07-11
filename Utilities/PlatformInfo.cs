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

        /// <summary> The revision number for the OS. For example <c>"10.0.17134.112"</c> or <c>"8.0"</c> </summary>
        public static string OSRevision { get; private set; }

        /// <summary> The name of the OS. E.G. "Windows" </summary>
        public static OperatingSystems OSName { get; private set; }

        static PlatformInfo()
        {
            GetOSInformation();
        }

        private static void GetOSInformation()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                // TODO: Solve problem of getting Win7 version when running in Powershell
                OSName = OperatingSystems.Windows;
                OSRevision = Environment.OSVersion.Version.ToString();
                OS = Environment.OSVersion.VersionString;
            }
            else if (Environment.OSVersion.Platform == PlatformID.MacOSX)
            {
                OSName = OperatingSystems.MacOS;
                OSRevision = Environment.OSVersion.Version.ToString();
                OS = Environment.OSVersion.VersionString;
            }
            else if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                UnixDistroInformation UnixDistro = QueryUnixDistribution();
                OSName = UnixDistro.OSVersion;
                OSRevision = UnixDistro.DistributionRelease;
                OS = UnixDistro.DistributionDescription;
            }
            else
            {
                OSName = OperatingSystems.Unsupported;
                OSRevision = Environment.OSVersion.Version.ToString();
                OS = Environment.OSVersion.VersionString;
            }

            if (OSName == OperatingSystems.Unsupported)
            {
                OS = "[Unsupported] " + OS;
                Log.Output(Log.Severity.WARNING, Log.Source.OTHER, "OPERATING UNSUPPORTED OS. SUPPORT NOT GUARUNTEED. OS = " + OS);
            }
        }

        private static UnixDistroInformation QueryUnixDistribution()
        {
            try
            {
                string[] Lines = File.ReadAllLines("/etc/lsb-release");
                UnixDistroInformation Result = new UnixDistroInformation();

                // Determine the distribution on the file. If it is not in the OperatingSystems enum, it is unsupported
                foreach (string Line in Lines)
                {
                    string[] LineInfo = Line.Split('=');
                    switch (LineInfo[0])
                    {
                        case "DISTRIB_ID":
                            Result.DistributionID = LineInfo[1];
                            break;
                        case "DISTRIB_RELEASE":
                            Result.DistributionRelease = LineInfo[1];
                            break;
                        case "DISTRIB_CODENAME":
                            Result.DistributionCodeName = LineInfo[1];
                            break;
                        case "DISTRIB_DESCRIPTION":
                            Result.DistributionDescription = LineInfo[1];
                            break;
                        default:
                            break;
                    }
                }
                if (Result.DistributionID.ToUpper() == "DEBIAN") { Result.OSVersion = OperatingSystems.Debian; }
                if (Result.DistributionID.ToUpper() == "UBUNTU") { Result.OSVersion = OperatingSystems.Ubuntu; }
                else { Result.OSVersion = OperatingSystems.Unsupported; }
                return Result;
            }
            catch
            {
                // Return empty information
                return new UnixDistroInformation()
                {
                    DistributionID = "Unknown Unix",
                    DistributionDescription = string.Empty,
                    DistributionRelease = string.Empty,
                    DistributionCodeName = string.Empty,
                    OSVersion = OperatingSystems.Unsupported,
                };
            }
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

        private struct UnixDistroInformation
        {
            public string DistributionID;
            public string DistributionRelease;
            public string DistributionCodeName;
            public string DistributionDescription;
            public OperatingSystems OSVersion;
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
