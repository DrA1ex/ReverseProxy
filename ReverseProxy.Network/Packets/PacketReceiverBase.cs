using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using NLog;
using ReverseProxy.BinarySerialization;
using ReverseProxy.Common.Model;
using ReverseProxy.Network.Misc;

namespace ReverseProxy.Network.Packets
{
    public abstract class PacketReceiverBase : IPacketReceiver
    {
        protected Logger Logger { get; } = LogManager.GetCurrentClassLogger();

        private SocketWriteQueue WriteQueue { get; } = new SocketWriteQueue();

        public virtual async Task SendPacket(Packet packet)
        {
            using(var stream = new MemoryStream())
            {
                var serializer = new BinarySerializer(BinarySerializationMethod.UnsafeSerialization);
                await serializer.Serialize(packet, stream);

                var data = new byte[stream.Length];
                Array.Copy(stream.GetBuffer(), data, data.Length);

                WriteQueue.QueueData(data);
            }

            Logger.Debug("Queue package Id: {0} of type {1} with data length: {2}", packet.SessionId, packet.Type, packet.Data?.Length ?? 0);
        }

        public event EventHandler<Packet> OnNewPacketReceived;

        protected virtual Task StartWaitQueue(NetworkStream stream)
        {
            return WriteQueue.Start(stream);
        }

        protected virtual void OnNewPacket(Packet packet)
        {
            OnNewPacketReceived?.Invoke(this, packet);
        }

        protected virtual async Task ProcessIncomingPackages(NetworkStream stream)
        {
            while(true)
            {
                Packet packet = null;

                try
                {
                    var serializer = new BinarySerializer(BinarySerializationMethod.UnsafeSerialization);
                    packet = await serializer.Deserialize<Packet>(stream);
                }
                catch(IOException e) when(e.InnerException is SocketException)
                {
                    // Client disconnected
                }
                catch(Exception e)
                {
                    Logger.Error(e);
                }

                if(packet != null)
                {
                    Logger.Debug("Received package Id: {0} of type {1} with data length: {2}", packet.SessionId, packet.Type, packet.Data?.Length ?? 0);
                    OnNewPacket(packet);
                }
                else
                {
                    Logger.Debug("Packet receiver lost connection");
                    WriteQueue.Stop();
                    break; //disconnected or network error
                }
            }
        }

        internal async Task Bind(TcpClient client)
        {
            using(client)
            using(var stream = client.GetStream())
            {
                Logger.Info("New packet connection: {0}", client.Client.RemoteEndPoint);

                await Task.WhenAll(
                    StartWaitQueue(stream),
                    ProcessIncomingPackages(stream));
            }
        }
    }
}