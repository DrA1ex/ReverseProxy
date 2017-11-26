using System;
using System.Net;
using ReverseProxy.Network.Packets;
using ReverseProxy.Network.Server.Base;

namespace ReverseProxy.Network.Server
{
    public class PacketServer : PacketReceiverBase, IServer, IDisposable
    {
        public PacketServer(IPAddress address, int port)
        {
            Server = new SimpleServer(address, port, Bind);
        }

        private SimpleServer Server { get; }

        public void Dispose()
        {
            Stop();
        }

        public bool Active => Server.Active;

        public void Start()
        {
            Server.Start();
        }

        public void Stop()
        {
            Server.Stop();
        }
    }
}