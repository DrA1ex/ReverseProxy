using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using ReverseProxy.Common.Model;
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

        protected Logger Logger { get; } = LogManager.GetCurrentClassLogger();

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
                PacketReceiver.OnNewPacketReceived += OnNewPacket;
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
            Logger.Debug("New client connected #{0} {1}", clientId, client.Client.RemoteEndPoint);

            var session = new PacketSession(PacketReceiver);

            try
            {
                SessionMapping.TryAdd(clientId, session);
                Logger.Trace("Added object #{0} into SessionMapping", clientId);

                await session.Start(clientId, client);

                Logger.Debug("Connection with client ${0} is closed", clientId);
            }
            finally
            {
                SessionMapping.TryRemove(clientId, out var _);
                Logger.Trace("Removed object #{0} from SessionMapping", clientId);
            }
        }

        private void OnNewPacket(object sender, Packet packet)
        {
            if(SessionMapping.TryGetValue(packet.SessionId, out var session))
            {
                switch(packet.Type)
                {
                    case PacketType.Message:
                        session.QueuePacket(packet);
                        break;
                    case PacketType.ConnectionClosed:
                        session.Stop();
                        break;
                }
            }
            else
            {
                // Received packet for already disconnected client
                Logger.Debug("Received '{0}' packet with undefined client Id: {1}", packet.Type, packet.SessionId);
            }
        }

        public override void Stop()
        {
            base.Stop();
            PacketReceiver.OnNewPacketReceived -= OnNewPacket;
        }
    }
}