using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Scarlet.Components.Sensors;

namespace UnitTest.ComponentTesting
{
    [TestClass]
    public class MAX31855Tests
    {
        [TestMethod]
        public void TestDataConvert()
        {
            // External (Thermocouple) temperature
            //                                             |   Thermocouple  |R|F|   Internal   |R|Flt|
            Assert.AreEqual(MAX31855.ConvertExternalFromRaw(0b1111_0000_0110_00_0_0_0000_0000_0000_0_000), -250F);
            Assert.AreEqual(MAX31855.ConvertExternalFromRaw(0b1111_1111_1111_00_0_0_0000_0000_0000_0_000), -1F);
            Assert.AreEqual(MAX31855.ConvertExternalFromRaw(0b1111_1111_1111_11_0_0_0000_0000_0000_0_000), -0.25F);
            Assert.AreEqual(MAX31855.ConvertExternalFromRaw(0b0000_0000_0000_00_0_0_0000_0000_0000_0_000), 0F);
            Assert.AreEqual(MAX31855.ConvertExternalFromRaw(0b0000_0001_1001_00_0_0_0000_0000_0000_0_000), 25F);
            Assert.AreEqual(MAX31855.ConvertExternalFromRaw(0b0000_0110_0100_11_0_0_0000_0000_0000_0_000), 100.75F);
            Assert.AreEqual(MAX31855.ConvertExternalFromRaw(0b0011_1110_1000_00_0_0_0000_0000_0000_0_000), 1000F);
            Assert.AreEqual(MAX31855.ConvertExternalFromRaw(0b0110_0100_0000_00_0_0_0000_0000_0000_0_000), 1600F);

            // Internal (calibration) temperature
            //                                             |   Thermocouple  |R|F|   Internal   |R|Flt|
            Assert.AreEqual(MAX31855.ConvertInternalFromRaw(0b0000_0000_0000_00_0_0_1100_1001_0000_0_000), -55F);
            Assert.AreEqual(MAX31855.ConvertInternalFromRaw(0b0000_0000_0000_00_0_0_1110_1100_0000_0_000), -20F);
            Assert.AreEqual(MAX31855.ConvertInternalFromRaw(0b0000_0000_0000_00_0_0_1111_1111_0000_0_000), -1F);
            Assert.AreEqual(MAX31855.ConvertInternalFromRaw(0b0000_0000_0000_00_0_0_1111_1111_1111_0_000), -0.0625F);
            Assert.AreEqual(MAX31855.ConvertInternalFromRaw(0b0000_0000_0000_00_0_0_0000_0000_0000_0_000), 0F);
            Assert.AreEqual(MAX31855.ConvertInternalFromRaw(0b0000_0000_0000_00_0_0_0001_1001_0000_0_000), 25F);
            Assert.AreEqual(MAX31855.ConvertInternalFromRaw(0b0000_0000_0000_00_0_0_0110_0100_1001_0_000), 100.5625F);
            Assert.AreEqual(MAX31855.ConvertInternalFromRaw(0b0000_0000_0000_00_0_0_0111_1111_0000_0_000), 127F);

            // Fault detection
            //                                          |   Thermocouple  |R|F|   Internal   |R|Flt|
            Assert.AreEqual(MAX31855.ConvertFaultFromRaw(0b0000_0000_0000_00_0_0_0000_0000_0000_0_000), MAX31855.Fault.NONE);
            Assert.AreEqual(MAX31855.ConvertFaultFromRaw(0b0000_0000_0000_00_0_1_0000_0000_0000_0_100), MAX31855.Fault.SHORT_VCC);
            Assert.AreEqual(MAX31855.ConvertFaultFromRaw(0b0000_0000_0000_00_0_1_0000_0000_0000_0_010), MAX31855.Fault.SHORT_GND);
            Assert.AreEqual(MAX31855.ConvertFaultFromRaw(0b0000_0000_0000_00_0_1_0000_0000_0000_0_001), MAX31855.Fault.NO_THERMOCOUPLE);
            Assert.AreEqual(MAX31855.ConvertFaultFromRaw(0b0000_0000_0000_00_0_1_0000_0000_0000_0_111), (MAX31855.Fault.SHORT_VCC | MAX31855.Fault.SHORT_GND | MAX31855.Fault.NO_THERMOCOUPLE));
            Assert.AreEqual(MAX31855.ConvertFaultFromRaw(0b0000_0000_0000_00_0_1_0000_0000_0000_0_101), (MAX31855.Fault.SHORT_VCC | MAX31855.Fault.NO_THERMOCOUPLE));
            Assert.AreEqual(MAX31855.ConvertFaultFromRaw(0b0000_0000_0000_00_0_0_0000_0000_0000_0_111), MAX31855.Fault.NONE); // The fault bit is not set, so any faults should be ignored.

            // Real data test
            //           |   Thermocouple  |R|F|   Internal   |R|Flt|
            uint Data = 0b0000_0001_1010_10_0_0_0001_0111_0111_0_000;
            Assert.AreEqual(MAX31855.ConvertExternalFromRaw(Data), 26.50F);
            Assert.AreEqual(MAX31855.ConvertInternalFromRaw(Data), 23.4375F);
        }
    }
}
