using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ReverseProxy.BinarySerialization;
using ReverseProxy.Common;
using ReverseProxy.Common.Model;
using ReverseProxy.Network.Server;
using ReverseProxy.Test.Fixture;
using ReverseProxy.Test.Helper;
using ReverseProxy.Test.Utils;

namespace ReverseProxy.Test
{
    [TestClass]
    public class PacketServerTest
    {
        private const int PacketSequenceLength = 10;
        private const int PacketServerPort = 65100;
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

        [TestMethod]
        public async Task ProcessAgentPackages()
        {
            var port = this.GetPortNumber(PacketServerPort);

            var serializer = new BinarySerializer(BinarySerializationMethod.UnsafeSerialization);
            var queue = new WaitableQueue<Packet>();

            using(var packetServer = new PacketServer(IPAddress.Any, port))
            {
                packetServer.OnNewPacketReceived += (sender, value) => queue.Enqueue(value);
                packetServer.Start();

                using(var client = new TcpClient("localhost", port))
                using(var stream = client.GetStream())
                {
                    var packetSequence = PacketFixture.GetPacketSequence(PacketSequenceLength);

                    foreach(var packet in packetSequence)
                    {
                        await serializer.Serialize(packet, stream).WithTimeOut(Timeout);
                        var answer = await queue.Dequeue(Timeout);

                        Assert.AreEqual<PacketEx>(packet, answer);
                    }
                }
            }
        }

        [TestMethod]
        public async Task ProcessServerPackages()
        {
            var port = this.GetPortNumber(PacketServerPort);

            var serializer = new BinarySerializer(BinarySerializationMethod.UnsafeSerialization);

            using(var packetServer = new PacketServer(IPAddress.Any, port))
            {
                packetServer.Start();

                using(var client = new TcpClient("localhost", port))
                using(var stream = client.GetStream())
                {
                    var packetSequence = PacketFixture.GetPacketSequence(PacketSequenceLength);

                    foreach(var packet in packetSequence)
                    {
                        await packetServer.SendPacket(packet).WithTimeOut(Timeout);
                        var answer = await serializer.Deserialize<Packet>(stream).WithTimeOut(Timeout);

                        Assert.AreEqual<PacketEx>(packet, answer);
                    }
                }
            }
        }

        [TestMethod]
        public async Task ProcessServerPackagesSequence()
        {
            var port = this.GetPortNumber(PacketServerPort);

            var serializer = new BinarySerializer(BinarySerializationMethod.UnsafeSerialization);
            var queue = new WaitableQueue<Packet>();

            using(var packetServer = new PacketServer(IPAddress.Any, port))
            {
                packetServer.OnNewPacketReceived += (sender, value) => queue.Enqueue(value);
                packetServer.Start();

                using(var client = new TcpClient("localhost", port))
                using(var stream = client.GetStream())
                {
                    var packetSequence = PacketFixture.GetPacketSequence(PacketSequenceLength);

                    foreach(var packet in packetSequence)
                    {
                        await serializer.Serialize(packet, stream).WithTimeOut(Timeout);
                    }

                    foreach(var packet in packetSequence)
                    {
                        var answer = await queue.Dequeue(Timeout);
                        Assert.AreEqual<PacketEx>(packet, answer);
                    }
                }
            }
        }

        [TestMethod]
        public async Task ProcessDuplexExchange()
        {
            var port = this.GetPortNumber(PacketServerPort);

            var serializer = new BinarySerializer(BinarySerializationMethod.UnsafeSerialization);
            var queue = new WaitableQueue<Packet>();

            using(var packetServer = new PacketServer(IPAddress.Any, port))
            {
                packetServer.OnNewPacketReceived += (sender, value) => queue.Enqueue(value);
                packetServer.Start();

                using(var client = new TcpClient("localhost", port))
                using(var stream = client.GetStream())
                {
                    var serverSequence = PacketFixture.GetPacketSequence(PacketSequenceLength);
                    var clientSequence = PacketFixture.GetPacketSequence(PacketSequenceLength);

                    foreach(var (serverPacket, clientPacket) in serverSequence.JoinByIndex(clientSequence))
                    {
                        await Task.WhenAll(packetServer.SendPacket(serverPacket),
                            serializer.Serialize(clientPacket, stream));

                        var results = await Task.WhenAll(queue.Dequeue(Timeout), serializer.Deserialize<Packet>(stream).WithTimeOut(Timeout));

                        Assert.AreEqual<PacketEx>(clientPacket, results[0]);
                        Assert.AreEqual<PacketEx>(serverPacket, results[1]);
                    }
                }
            }
        }

        [TestMethod]
        public async Task ProcessDuplexExchangeConcurrent()
        {
            var port = this.GetPortNumber(PacketServerPort);

            var serializer = new BinarySerializer(BinarySerializationMethod.UnsafeSerialization);
            var queue = new WaitableQueue<Packet>();

            using(var packetServer = new PacketServer(IPAddress.Any, port))
            {
                packetServer.OnNewPacketReceived += (sender, value) => queue.Enqueue(value);
                packetServer.Start();

                using(var client = new TcpClient("localhost", port))
                using(var stream = client.GetStream())
                {
                    var serverSequence = PacketFixture.GetPacketSequence(PacketSequenceLength);
                    Task serverTask = null;
                    foreach(var packet in serverSequence)
                    {
                        var task = packetServer.SendPacket(packet);
                        serverTask = serverTask?.ContinueWith(t =>
                        {
                            Trace.WriteLine("SendPacket task finished");
                            return task;
                        }).Unwrap() ?? task;
                    }

                    var clientSequence = PacketFixture.GetPacketSequence(PacketSequenceLength);
                    Task clientTask = Task.CompletedTask;
                    foreach(var packet in clientSequence)
                    {
                        // ReSharper disable once AccessToDisposedClosure
                        clientTask = clientTask.ContinueWith(t => serializer.Serialize(packet, stream)).Unwrap();
                    }

                    await Task.WhenAll(serverTask, clientTask).WithTimeOut(Timeout);
                    foreach(var (serverPacket, clientPacket) in serverSequence.JoinByIndex(clientSequence))
                    {
                        var results = await Task.WhenAll(queue.Dequeue(Timeout), serializer.Deserialize<Packet>(stream).WithTimeOut(Timeout));

                        Assert.AreEqual<PacketEx>(clientPacket, results[0]);
                        Assert.AreEqual<PacketEx>(serverPacket, results[1]);
                    }
                }
            }
        }
    }
}