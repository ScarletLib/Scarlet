using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Scarlet.Communications;
using Scarlet.Utilities;

namespace UnitTest
{
    [TestClass]
    public class UnitTestClientAndServer
    {
        private static String ReceivedMessage, ReceivedMessage2;

        private void InitializeConnection()
        {
            Server.Start(2000, 2001);
            System.Threading.Thread.Sleep(500);
            Client.Start("127.0.0.1", 2000, 2001, "TestClient");
            System.Threading.Thread.Sleep(500);
        }

        public void MessageHandler(Packet Packet)
        {
            ReceivedMessage = UtilData.ToString(Packet.Data.Payload);
        }

        public void MessageHandler2(Packet Packet)
        {
            ReceivedMessage2 = UtilData.ToString(Packet.Data.Payload);
        }

        [TestMethod]
        public void BasicTest()
        {
            String TestText1 = "Hello, World!";
            String TestText2 = "hello, world";
            Byte ChannelID1 = 0xcd;
            Byte ChannelID2 = 0xcf;

            InitializeConnection();
            Parse.SetParseHandler(ChannelID1, MessageHandler);
            Parse.SetParseHandler(ChannelID2, MessageHandler2);

            Packet MyPack = new Packet(new Message(ChannelID1, UtilData.ToBytes(TestText1)), false);
            Client.Send(MyPack, PacketPriority.LOWEST);
            System.Threading.Thread.Sleep(200);
            Assert.AreEqual(TestText1, ReceivedMessage);

            Packet MyPack2 = new Packet(new Message(ChannelID2, UtilData.ToBytes(TestText2)), false, "TestClient");
            Server.Send(MyPack2);
            System.Threading.Thread.Sleep(200);
            Assert.AreEqual(TestText2, ReceivedMessage2);
        }
    }
}
