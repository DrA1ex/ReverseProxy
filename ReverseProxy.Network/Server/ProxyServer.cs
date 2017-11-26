using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ReverseProxy.Common;
using ReverseProxy.Common.Utils;
using ReverseProxy.Network.Packets;
using ReverseProxy.Network.Server.Base;
using ReverseProxy.Network.Sessions;
using SessionMapping = System.Collections.Concurrent.ConcurrentDictionary<long, ReverseProxy.Network.Sessions.PacketSession>;

namespace ReverseProxy.Network.Server
{
    public class ProxyServer : ServerBase
    {
        private long _nextClientId;

        public ProxyServer(IPAddress address, int port)
            : base(address, port)
        {
        }

        public IPacketReceiver PacketReceiver { get; set; }
        private SessionMapping SessionMapping { get; } = new SessionMapping();

        private long GetNextClientId()
        {
            return Interlocked.Increment(ref _nextClientId);
        }

        public override void Start()
        {
            if(PacketReceiver != null)
            {
                PacketReceiver.OnNewPacketReceived += PacketReceiverOnNewPacketReceived;
                base.Start();
            }
            else
            {
                throw new InvalidOperationException("PacketReceiver should be specified");
            }
        }

        protected override Task OnNewClient(TcpClient client)
        {
            AcceptSession(GetNextClientId(), client);
            return Task.CompletedTask;
        }

        private async void AcceptSession(long clientId, TcpClient client)
        {
            LogUtils.LogDebugMessage("New client connected #{0} {1}", clientId, client.Client.RemoteEndPoint);

            var session = new PacketSession(PacketReceiver);

            try
            {
                SessionMapping.TryAdd(clientId, session);
                LogUtils.LogDebugMessage("Added object #{0} into SessionMapping", clientId);

                await session.Start(clientId, client);
            }
            finally
            {
                SessionMapping.TryRemove(clientId, out var _);
                LogUtils.LogDebugMessage("Removed object #{0} from SessionMapping", clientId);
            }
        }

        private void PacketReceiverOnNewPacketReceived(object sender, Packet packet)
        {
            if(SessionMapping.TryGetValue(packet.Id, out var session))
            {
                session.QueuePacket(packet);
            }
            else
            {
                // Received packet for already disconnected client
                LogUtils.LogDebugMessage("Received message with undefined client Id: {0}", packet.Id);
            }
        }

        public override void Stop()
        {
            base.Stop();
            PacketReceiver.OnNewPacketReceived -= PacketReceiverOnNewPacketReceived;
        }
    }
}