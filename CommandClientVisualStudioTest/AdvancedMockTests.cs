using System;
using System.Net;
using System.Reflection;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Proshot.CommandClient;
using Rhino.Mocks;
using System.Linq;

namespace CommandClientVisualStudioTest
{
    [TestClass]
    public class AdvancedMockTests
    {
        private MockRepository mocks;

        [TestMethod]
        public void VerySimpleTest()
        {
            CMDClient client = new CMDClient(null, "Bogus network name");
            Assert.AreEqual("Bogus network name", client.NetworkName);
        }

        [TestInitialize()]
        public void Initialize()
        {
            mocks = new MockRepository();
        }

        [TestMethod]
        public void TestUserExitCommand()
        {
            IPAddress ipaddress = IPAddress.Parse("127.0.0.1");
            Command command = new Command(CommandType.UserExit, ipaddress, null);
            System.IO.Stream fakeStream = mocks.DynamicMock<System.IO.Stream>();
            byte[] commandBytes = { 0, 0, 0, 0 };
            byte[] ipLength = { 9, 0, 0, 0 };
            byte[] ip = { 49, 50, 55, 46, 48, 46, 48, 46, 49 };
            byte[] metaDataLength = { 2, 0, 0, 0 };
            byte[] metaData = { 10, 0 };

            using (mocks.Ordered())
            {
                fakeStream.Write(commandBytes, 0, 4);
                fakeStream.Flush();
                fakeStream.Write(ipLength, 0, 4);
                fakeStream.Flush();
                fakeStream.Write(ip, 0, 9);
                fakeStream.Flush();
                fakeStream.Write(metaDataLength, 0, 4);
                fakeStream.Flush();
                fakeStream.Write(metaData, 0, 2);
                fakeStream.Flush();
            }
            mocks.ReplayAll();
            CMDClient client = new CMDClient(null, "Bogus network name");
            
            // we need to set the private variable here
            typeof(CMDClient).GetField("networkStream", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(client, fakeStream);

            client.SendCommandToServerUnthreaded(command);
            mocks.VerifyAll();
            
        }

        [TestMethod]
        public void TestUserExitCommandWithoutMocks()
        {
            IPAddress ipaddress = IPAddress.Parse("127.0.0.1");
            Command command = new Command(CommandType.UserExit, ipaddress, null);
            MemoryStream mockStream = new MemoryStream();
            byte[] commandBytes = { 0, 0, 0, 0 };
            byte[] ipLength = { 9, 0, 0, 0 };
            byte[] ip = { 49, 50, 55, 46, 48, 46, 48, 46, 49 };
            byte[] metaDataLength = { 2, 0, 0, 0 };
            byte[] metaData = { 10, 0 };

            String commandBytesString = "0000";
            String ipLengthString = "9000";
            String ipString = "495055464846484649";
            String metaDataLengthString = "2000";
            String metaDataString = "100";

            CMDClient client = new CMDClient(null, "Bogus network name");
            typeof(CMDClient).GetField("networkStream", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(client, mockStream);
            client.SendCommandToServerUnthreaded(command);
            byte[] data = mockStream.ToArray();
            String dataString = "";
            for (int i = 0; i < data.Length; i++)
            {
                dataString += data[i].ToString();
            }

            Assert.IsTrue(dataString.Contains(commandBytesString));
            Assert.IsTrue(dataString.Contains(ipLengthString));
            Assert.IsTrue(dataString.Contains(ipString));
            Assert.IsTrue(dataString.Contains(metaDataLengthString));
            Assert.IsTrue(dataString.Contains(metaDataString));

        }

        [TestMethod]
        public void TestSemaphoreReleaseOnNormalOperation()
        {
            System.IO.Stream mockStream = mocks.DynamicMock<System.IO.Stream>();
            System.Threading.Semaphore mockSemaphore =  mocks.DynamicMock<System.Threading.Semaphore>();

            using (mocks.Ordered())
            {
                Expect.Call(mockSemaphore.WaitOne()).Return(true);
                Expect.Call(mockSemaphore.Release()).Return(1);
            }
            mocks.ReplayAll();
            CMDClient client = new CMDClient(null, "Bogus network name");
            typeof(CMDClient).GetField("networkStream", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(client, mockStream);
            typeof(CMDClient).GetField("semaphore", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(client, mockSemaphore);

            client.SendCommandToServerUnthreaded(new Command(CommandType.UserExit, IPAddress.Parse("127.0.0.1"), null));
            mocks.VerifyAll();
        }
        
        [TestMethod]
        public void TestSemaphoreReleaseOnExceptionalOperation()
        {
            System.IO.Stream mockStream = mocks.DynamicMock<System.IO.Stream>();
            System.Threading.Semaphore mockSemaphore = null;
            mockSemaphore = mocks.DynamicMock<System.Threading.Semaphore>();

            using (mocks.Ordered())
            {
                Expect.Call(mockSemaphore.WaitOne()).Return(true);               
                mockStream.Flush();
                Expect.Call(mockSemaphore.Release()).Return(1);
                LastCall.On(mockStream).Throw(new ArgumentException());
            }
            mocks.ReplayAll();
            CMDClient client = new CMDClient(null, "Bogus network name");
            typeof(CMDClient).GetField("networkStream", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(client, mockStream);
            typeof(CMDClient).GetField("semaphore", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(client, mockSemaphore);
            try
            {
                client.SendCommandToServerUnthreaded(new Command(CommandType.UserExit, IPAddress.Parse("127.0.0.1"), null));
            }
            catch { }
            mocks.VerifyAll();

        }
    }
}
