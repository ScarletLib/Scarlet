using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Scarlet.Communications;
using Scarlet.Utilities;

namespace UnitTest
{
    [TestClass]
    public class UnitTestClientAndServer
    {
        private static String ReceivedMessage;

        private void InitializeConnection()
        {
            Server.Start(2000, 2001);
            System.Threading.Thread.Sleep(500);
            Client.Start("127.0.0.1", 2000, 2001, "MyClient");
            System.Threading.Thread.Sleep(500);
        }

        public void MessageHandler(Packet Packet)
        {
            ReceivedMessage = UtilData.ToString(Packet.Data.Payload);
        }

        [TestMethod]
        public void BasicTest()
        {
            String TestText = "Hello, World!";
            Byte ChannelID = 0xcd;

            InitializeConnection();
            Parse.SetParseHandler(ChannelID, MessageHandler);
            Packet MyPack = new Packet(new Message(ChannelID, UtilData.ToBytes(TestText)), false);
            Client.Send(MyPack);
            System.Threading.Thread.Sleep(200);
            Assert.AreEqual(TestText, ReceivedMessage);
        }
    }
}
