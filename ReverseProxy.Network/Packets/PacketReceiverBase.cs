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

            LogUtils.LogDebugMessage("Queue package Id: {0} Length: {1}", packet.Id, packet.Data.Length);
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
                Packet message = null;

                try
                {
                    var serializer = new BinarySerializer(BinarySerializationMethod.UnsafeSerialization);
                    message = await serializer.Deserialize<Packet>(stream);
                }
                catch(IOException e) when(e.InnerException is SocketException)
                {
                    // Client disconnected
                }
                catch(Exception e)
                {
                    LogUtils.LogException(e);
                }

                if(message != null)
                {
                    LogUtils.LogDebugMessage("Received packet Id: {0} Length: {1}", message.Id, message.Data.Length);
                    OnNewPacket(message);
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