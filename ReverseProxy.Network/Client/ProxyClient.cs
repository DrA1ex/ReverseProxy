using System.Net.Sockets;
using ReverseProxy.Common;
using ReverseProxy.Common.Utils;
using ReverseProxy.Network.Packets;
using ReverseProxy.Network.Sessions;
using SessionMapping = System.Collections.Concurrent.ConcurrentDictionary<long, ReverseProxy.Network.Sessions.PacketSession>;

namespace ReverseProxy.Network.Client
{
    public class ProxyClient
    {
        private IPacketReceiver _packetReceiver;

        public ProxyClient(string host, int port)
        {
            Host = host;
            Port = port;
        }

        private SessionMapping SessionMapping { get; } = new SessionMapping();

        public string Host { get; }
        public int Port { get; }

        public IPacketReceiver PacketReceiver
        {
            get => _packetReceiver;
            set
            {
                _packetReceiver = value;
                value.OnNewPacketReceived += OnNewPacket;
            }
        }

        private async void OnNewPacket(object sender, Packet packet)
        {
            SessionMapping.TryGetValue(packet.Id, out var session);

            if(session == null)
            {
                var client = new TcpClient();
                await client.ConnectAsync(Host, Port);

                session = new PacketSession(PacketReceiver);
                AcceptSession(session, packet.Id, client);
            }

            session.QueuePacket(packet);
        }

        private async void AcceptSession(PacketSession session, long sessionId, TcpClient client)
        {
            LogUtils.LogDebugMessage("New client connected #{0} {1}", sessionId, client.Client.RemoteEndPoint);

            try
            {
                SessionMapping.TryAdd(sessionId, session);
                LogUtils.LogDebugMessage("Added object #{0} into SessionMapping", sessionId);

                await session.Start(sessionId, client);
            }
            finally
            {
                SessionMapping.TryRemove(sessionId, out var _);
                LogUtils.LogDebugMessage("Removed object #{0} from SessionMapping", sessionId);
            }
        }
    }
}