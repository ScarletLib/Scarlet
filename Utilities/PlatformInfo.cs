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
        /// <summary> Gets the general class of device, such as <c>"RaspberryPi"</c> or <c>"PC"</c>. </summary>
        public static PlatformType Platform { get; private set; }

        /// <summary> Gets info about the hardware, such as <c>"Pi 3 Model B"</c> or <c>"Laptop"</c>. </summary>
        public static string Hardware { get; private set; }

        /// <summary> Gets info about the OS, such as <c>"Windows 10.0.17134.112"</c> or <c>"Debian 8.0"</c>. </summary>
        public static string OS { get; private set; }

        /// <summary> Gets the revision number for the OS. For example <c>"10.0.17134.112"</c> or <c>"8.0"</c>. </summary>
        public static string OSRevision { get; private set; }

        /// <summary> Gets the name of the OS as an enum value. E.G. OperatingSystems.Windows </summary>
        public static OperatingSystems OSName { get; private set; }

        /// <summary> Gets a value indicating whether or not the OS is supported by Scarlet. </summary>
        public static bool OSSupport { get; private set; }

        /// <summary> Gets the CPU information file dump from the unix kernel. Used in multiple places. </summary>
        private static readonly string[] UnixCPUInfo;

        static PlatformInfo()
        {
            try { UnixCPUInfo = File.ReadAllLines("/proc/cpuinfo"); }
            catch { } // Not a unix platform / broken unix kernel
            GetPlatformInformation();
            GetOSInformation();
            switch (Platform)
            {
                case PlatformType.RaspberryPi:
                    ReadPiVersion();
                    break;
            }
        }

        private static void GetPlatformInformation()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                case PlatformID.MacOSX:
                    Platform = PlatformType.PC;
                    break;
                case PlatformID.Unix:
                    Platform = DetermineUnixPlatform();
                    break;
            }
        }

        private static PlatformType DetermineUnixPlatform()
        {
            // Determine if this is a Beaglebone Black, Raspberry Pi, or PC
            try
            {
                IEnumerable<string> PlatformEnumerable = UnixCPUInfo.Where(x => x.StartsWith("Hardware"));
                if (!PlatformEnumerable.Any()) { return PlatformType.PC; }
                else
                {
                    string PlatformName = PlatformEnumerable.First();
                    PlatformName = PlatformName.Substring(PlatformName.IndexOf(':') + 1).ToUpper().Trim();
                    Log.ForceOutput(Log.Severity.DEBUG, Log.Source.OTHER, PlatformName);
                    switch (PlatformName)
                    {
                        case "BCM2709": return PlatformType.RaspberryPi;
                        default: return PlatformType.PC;
                    }
                }
            }
            catch { return PlatformType.Other; }
        }

        private static void GetOSInformation()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                    // TODO: Solve problem of getting Win7 version when running in Powershell
                    OSName = OperatingSystems.Windows;
                    OSRevision = Environment.OSVersion.Version.ToString();
                    OS = Environment.OSVersion.VersionString;
                    break;
                case PlatformID.MacOSX:
                    OSName = OperatingSystems.MacOS;
                    OSRevision = Environment.OSVersion.Version.ToString();
                    OS = Environment.OSVersion.VersionString;
                    break;
                case PlatformID.Unix:
                    UnixDistroInformation UnixDistro = QueryUnixDistribution();
                    OSName = UnixDistro.OSVersion;
                    OSRevision = UnixDistro.DistributionRelease;
                    OS = UnixDistro.DistributionDescription;
                    break;
                default:
                    OSName = OperatingSystems.NotSupported;
                    OSRevision = Environment.OSVersion.Version.ToString();
                    OS = Environment.OSVersion.VersionString;
                    break;
            }

            if (OSName == OperatingSystems.NotSupported)
            {
                OSSupport = false;
                string OSWarning = string.Empty;
                if (OS.Length != 0) { OSWarning = " OS = " + OS; }
                Log.Output(Log.Severity.WARNING, Log.Source.OTHER, "OPERATING UNSUPPORTED OS. SUPPORT NOT GUARUNTEED." + OSWarning);
            }
            else { OSSupport = true; }
        }

        private static UnixDistroInformation QueryUnixDistribution()
        {
            try
            {
                string[] PossibleFiles = new string[] { "/etc/lsb-release", "/etc/os-release" };
                string WorkingFile = PossibleFiles[0];
                foreach (string CurrentFile in PossibleFiles) { if (File.Exists(CurrentFile)) { WorkingFile = CurrentFile; break; } }
                string[] Lines = File.ReadAllLines(WorkingFile);
                UnixDistroInformation Result = new UnixDistroInformation();

                // Determine the distribution on the file. If it is not in the OperatingSystems enum, it is unsupported
                foreach (string Line in Lines)
                {
                    string[] LineInfo = Line.Split('=');
                    switch (LineInfo[0].ToUpper().Trim())
                    {
                        case "DISTRIB_ID":
                        case "DISTRIBUTOR ID":
                        case "ID_LIKE":
                            Result.DistributionID = LineInfo[1].Trim('"', ' ');
                            break;
                        case "DISTRIB_RELEASE":
                        case "RELEASE":
                        case "VERSION":
                            Result.DistributionRelease = LineInfo[1].Trim('"', ' ');
                            break;
                        case "DISTRIB_CODENAME":
                        case "CODENAME":
                        case "ID":
                            Result.DistributionCodeName = LineInfo[1].Trim('"', ' ');
                            break;
                        case "DISTRIB_DESCRIPTION":
                        case "DESCRIPTION":
                        case "PRETTY_NAME":
                            Result.DistributionDescription = LineInfo[1].Trim('"', ' ');
                            break;
                        default:
                            break;
                    }
                }
                if (Result.DistributionID.ToUpper() == "DEBIAN") { Result.OSVersion = OperatingSystems.Debian; }
                else if (Result.DistributionID.ToUpper() == "UBUNTU") { Result.OSVersion = OperatingSystems.Ubuntu; }
                else { Result.OSVersion = OperatingSystems.NotSupported; } // Set OS to unsupported if not a supported linux distribution
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
                    OSVersion = OperatingSystems.NotSupported,
                };
            }
        }

        // Info from: http://ozzmaker.com/check-raspberry-software-hardware-version-command-line/
        private static void ReadPiVersion()
        {
            string Revision = UnixCPUInfo.Where(x => x.StartsWith("Revision")).First();
            Revision = Revision.Substring(Revision.IndexOf(':') + 1).ToLower().Trim();
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
            NotSupported,
        }

        public enum PlatformType
        {
            RaspberryPi,
            BeagleBoneBlack,
            PC,
            Other,
        }
    }
}