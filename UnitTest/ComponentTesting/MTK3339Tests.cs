using Microsoft.VisualStudio.TestTools.UnitTesting;
using Scarlet.Components.Sensors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTest.ComponentTesting
{
    [TestClass]
    public class MTK3339Tests
    {
        [TestMethod]
        public void TestDataConvert()
        {
            MTK3339 Sens = new MTK3339(null);
            PrivateObject SensAccess = new PrivateObject(Sens);

            byte? Checksum = (byte?)SensAccess.Invoke("GetChecksum", "$GPRMC,012600.089,V,,,,,0.00,0.00,110618,,,N*46", false);
            Assert.IsNotNull(Checksum);
            Assert.AreEqual((byte?)0x46, Checksum);

            Checksum = (byte?)SensAccess.Invoke("GetChecksum", "$GPGSA,A,1,,,,,,,,,,,,,,,*1E", false);
            Assert.IsNotNull(Checksum);
            Assert.AreEqual((byte?)0x1E, Checksum);

            Checksum = (byte?)SensAccess.Invoke("GetChecksum", "GPVTG,0.00,T,,M,0.00,N,0.00,K,N", true);
            Assert.IsNotNull(Checksum);
            Assert.AreEqual((byte?)0x32, Checksum);

            Checksum = (byte?)SensAccess.Invoke("GetChecksum", "NotAValidPacket*1E", false);
            Assert.IsNull(Checksum);

            Checksum = (byte?)SensAccess.Invoke("GetChecksum", "$NotAValidPacket", false);
            Assert.IsNull(Checksum);

            Checksum = (byte?)SensAccess.Invoke("GetChecksum", null, false);
            Assert.IsNull(Checksum);

            Checksum = (byte?)SensAccess.Invoke("GetChecksum", "", false);
            Assert.IsNull(Checksum);
        }
    }
}
