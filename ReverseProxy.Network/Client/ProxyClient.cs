using System.Net.Sockets;
using System.Threading.Tasks;
using NLog;
using ReverseProxy.Common.Model;
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

        protected Logger Logger { get; } = LogManager.GetCurrentClassLogger();

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
            SessionMapping.TryGetValue(packet.SessionId, out var session);

            if(packet.Type == PacketType.Message)
            {
                if(session == null)
                {
                    try
                    {
                        session = await CreateSession(packet.SessionId);
                    }
                    catch(SocketException e)
                    {
                        Logger.Error("Unable to establish connection with target server: {0}", e);
                        await PacketReceiver.SendPacket(new Packet
                        {
                            SessionId = packet.SessionId,
                            Type = PacketType.ConnectionClosed
                        });
                        return;
                    }
                }

                session.QueuePacket(packet);
            }
            else if(packet.Type == PacketType.ConnectionClosed)
            {
                session?.Stop();
            }
        }

        private async Task<PacketSession> CreateSession(long sessionId)
        {
            var client = new TcpClient();
            await client.ConnectAsync(Host, Port);

            var session = new PacketSession(PacketReceiver);
            AcceptSession(session, sessionId, client);

            return session;
        }

        private async void AcceptSession(PacketSession session, long sessionId, TcpClient client)
        {
            Logger.Debug("New client connected #{0} {1}", sessionId, client.Client.RemoteEndPoint);

            try
            {
                SessionMapping.TryAdd(sessionId, session);
                Logger.Debug("Added object #{0} into SessionMapping", sessionId);

                await session.Start(sessionId, client);
            }
            finally
            {
                SessionMapping.TryRemove(sessionId, out var _);
                Logger.Debug("Removed object #{0} from SessionMapping", sessionId);
            }
        }
    }
}