using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using ReverseProxy.Common.Utils;
using ReverseProxy.Network.Packets;

namespace ReverseProxy.Network.Client
{
    public class PacketClient : PacketReceiverBase
    {
        private static readonly TimeSpan MinReconnectDelayInMinutes = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan MaxReconnectDelayInMinutes = TimeSpan.FromMinutes(5);

        public PacketClient(string host, int port)
        {
            Host = host;
            Port = port;

            InitializeConnection();
        }

        private bool Reconnect { get; } = true;

        public string Host { get; }
        public int Port { get; }

        private async Task Connect()
        {
            using(var client = new TcpClient())
            {
                await client.ConnectAsync(Host, Port);
                await Bind(client);
            }

            Logger.Error("Lost connection with Packet Server");

            if(Reconnect)
            {
                InitializeConnection();
            }
        }

        private async void InitializeConnection()
        {
            var timeout = MinReconnectDelayInMinutes;

            while(true)
            {
                Logger.Info("Connecting to server...");

                try
                {
                    await Connect();
                    break;
                }
                catch(Exception e)
                {
                    Logger.Error("Failed to connect: {0}:", e.Message);
                    Logger.Info("Queue next connection try after {0}", timeout.ToDurationString(30));
                }

                await Task.Delay(timeout);

                timeout = TimeSpan.FromSeconds(Math.Min(timeout.TotalSeconds * 2, MaxReconnectDelayInMinutes.TotalSeconds));
            }
        }
    }
}