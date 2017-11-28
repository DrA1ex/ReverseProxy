using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using ReverseProxy.BinarySerialization;
using ReverseProxy.Common;
using ReverseProxy.Common.Utils;
using ReverseProxy.Network.Misc;

namespace ReverseProxy.Network.Packets
{
    public abstract class PacketReceiverBase : IPacketReceiver
    {
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

            LogUtils.LogDebugMessage("Queue package Id: {0} of type {1} with data length: {2}", packet.SessionId, packet.Type, packet.Data?.Length ?? 0);
        }

        protected virtual Task StartWaitQueue(NetworkStream stream)
        {
            return WriteQueue.Start(stream);
        }

        public event EventHandler<Packet> OnNewPacketReceived;

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
                    LogUtils.LogException(e);
                }

                if(packet != null)
                {
                    LogUtils.LogDebugMessage("Received package Id: {0} of type {1} with data length: {2}", packet.SessionId, packet.Type, packet.Data?.Length ?? 0);
                    OnNewPacket(packet);
                }
                else
                {
                    LogUtils.LogErrorMessage("Packet receiver lost connection");
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
                LogUtils.LogInfoMessage("New packet connection: {0}", client.Client.RemoteEndPoint);

                await Task.WhenAll(
                    StartWaitQueue(stream),
                    ProcessIncomingPackages(stream));
            }
        }
    }
}