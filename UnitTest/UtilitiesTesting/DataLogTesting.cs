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
            DUT.CloseFile();
            DUT.DeleteAll();
        }

        [TestMethod]
        public void DataLogBasicUseTest()
        {
            DataLog DUT = new DataLog("TestFile");
            DUT.DeleteAll();
            for (int i = 0; i < 10; i++)
            {
                DUT.Output(new DataUnit("TestDataUnit")
                {
                    { "a", "∫" },
                    { "b", 20 },
                    { "c", i }
                }.SetSystem("TestSystem"));
            }
            DUT.DeleteAll(); // Test deleting all the log files
            DUT.CloseFile(); // Close the file to regain access permission
            Assert.IsTrue(File.Exists(DUT.LogFilePath));
            string[] Lines = File.ReadAllLines(DUT.LogFilePath);
            int LineCnt = Lines.Length;
            string[] ExpectedLines = new string[] 
            {
                "TestSystem.TestDataUnit.a,TestSystem.TestDataUnit.b,TestSystem.TestDataUnit.c",
                "∫,20,0", "∫,20,1", "∫,20,2", "∫,20,3", "∫,20,4",
                "∫,20,5", "∫,20,6", "∫,20,7", "∫,20,8", "∫,20,9"
            };
            for (int i = 0; i < Lines.Length; i++)
            {
                Assert.AreEqual(Lines[i], ExpectedLines[i]); // Test contents are correct
            }
            Assert.ThrowsException<Exception>(delegate { DUT.Output(DataCont); }); // Try to output to the file (which was closed)
            Assert.AreEqual(File.ReadAllLines(DUT.LogFilePath).Length, LineCnt); // Ensure it didn't write
            DUT.DeleteAll(); // Delete the file
            Assert.IsFalse(File.Exists(DUT.LogFilePath)); // Make sure it was deleted
        }
    }
}
