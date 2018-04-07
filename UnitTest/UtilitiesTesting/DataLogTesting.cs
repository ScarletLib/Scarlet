using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Scarlet.Utilities;
using System.Collections.Generic;
using System.IO;

namespace UnitTest.UtilitiesTesting
{
    [TestClass]
    public class DataLogTesting
    {
        DataUnit DataCont = new DataUnit("DataLogTesting")
        {
            { "int", 0x811426 },
            { "float", 1.0486F },
            { "bool", false },
            { "Dictionary", new Dictionary<string, string>() },
            { "string", "Erza" },
            { "byte", (byte)19 },
            { "long", 811426811426L }
        };

        [TestMethod]
        public void DataUnitTest()
        {
            Assert.AreEqual(0x811426, this.DataCont.GetValue<int>("int"));
            Assert.AreEqual(1.0486F, this.DataCont.GetValue<float>("float"));
            Assert.AreEqual(false, this.DataCont.GetValue<bool>("bool"));
            Assert.IsInstanceOfType(this.DataCont.GetValue<Dictionary<string, string>>("Dictionary"), typeof(Dictionary<string, string>));
            Assert.AreEqual("Erza", this.DataCont.GetValue<string>("string"));
            Assert.AreEqual((byte)19, this.DataCont.GetValue<byte>("byte"));
            Assert.AreEqual(811426811426L, this.DataCont.GetValue<long>("long"));

            Assert.ThrowsException<InvalidCastException>(delegate { this.DataCont.GetValue<int>("string"); });
            Assert.ThrowsException<InvalidCastException>(delegate { this.DataCont.GetValue<bool>("string"); });
            Assert.ThrowsException<InvalidCastException>(delegate { this.DataCont.GetValue<string>("Dictionary"); });
            Assert.ThrowsException<InvalidCastException>(delegate { this.DataCont.GetValue<int>("long"); });
            Assert.ThrowsException<InvalidCastException>(delegate { this.DataCont.GetValue<byte>("int"); });
            Assert.ThrowsException<InvalidCastException>(delegate { this.DataCont.GetValue<byte>("bool"); });
            Assert.ThrowsException<InvalidCastException>(delegate { this.DataCont.GetValue<float>("string"); });

            foreach (KeyValuePair<string, object> Item in this.DataCont) // Just to check there are no errors doing this.
            {
                Console.WriteLine(String.Format("key \"{0}\" has value \"{1}\".", Item.Key, Item.Value));
            }
        }

        [TestMethod]
        public void DataLogTestDeleteDuringUse()
        {
            DataLog DUT = new DataLog("DataLogUnitTest");
            DUT.Output(DataCont);
            Assert.ThrowsException<IOException>(delegate { File.Delete(DUT.LogFilePath); });
        }

        [TestMethod]
        public void DataLogBasicUseTest()
        {
            DataLog.DeleteAll();
            DataLog DUT = new DataLog("test");
            DataUnit DataUnitWait = new DataUnit("test")
            {
                { "a", 10 },
                { "b", 20 },
                { "c", 30 }
            }.SetSystem("TestSystem");
            for (int i = 0; i < 10; i++) { DUT.Output(DataUnitWait); }
            DataLog.DeleteAll();
        }
    }
}
