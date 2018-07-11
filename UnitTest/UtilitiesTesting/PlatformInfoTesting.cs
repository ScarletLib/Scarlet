using Microsoft.VisualStudio.TestTools.UnitTesting;
using Scarlet.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTest.UtilitiesTesting
{
    [TestClass]
    public class PlatformInfoTesting
    {
        [TestMethod]
        public void BasicOutputTest()
        {
            Log.ForceOutput(Log.Severity.INFO, Log.Source.OTHER, "HARDWARE: " + PlatformInfo.Hardware);
            Log.ForceOutput(Log.Severity.INFO, Log.Source.OTHER, "OS: " + PlatformInfo.OS);
            Log.ForceOutput(Log.Severity.INFO, Log.Source.OTHER, "OS REVISION: " + PlatformInfo.OSRevision);
            Log.ForceOutput(Log.Severity.INFO, Log.Source.OTHER, "OS NAME: " + Enum.GetName(typeof(PlatformInfo.OperatingSystems), PlatformInfo.OSName));
            Log.ForceOutput(Log.Severity.INFO, Log.Source.OTHER, "PLATFORM: " + PlatformInfo.Platform);
        }
    }
}
