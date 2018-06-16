using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Scarlet.Components.Sensors;

namespace UnitTest.ComponentTesting
{
    [TestClass]
    public class BME280Tests
    {
        [TestMethod]
        public void TestDataConvert()
        {
            BME280 Sens = new BME280(null);
            PrivateObject SensAccess = new PrivateObject(Sens);

            BME280.CompensationParameters CompParams = new BME280.CompensationParameters()
            {
                T1 = 27504,
                T2 = 26435,
                T3 = -1000,
                P1 = 36477,
                P2 = -10685,
                P3 = 3024,
                P4 = 2855,
                P5 = 140,
                P6 = -7,
                P7 = 15500,
                P8 = -14600,
                P9 = 6000
            };

            SensAccess.SetField("CompParams", CompParams);
            int IntTempOut = (int)SensAccess.Invoke("ProcessTemperatureInternal", 519888);
            Assert.AreEqual(128422, IntTempOut);

            double TempOut = (double)SensAccess.Invoke("ProcessTemperature", IntTempOut);
            Assert.AreEqual(25.08, TempOut);

            double PressOut = (double)SensAccess.Invoke("ProcessPressure", 415148, IntTempOut);
            Assert.IsTrue(Math.Abs(100653.265625 - PressOut) < 1);
        }
    }
}
