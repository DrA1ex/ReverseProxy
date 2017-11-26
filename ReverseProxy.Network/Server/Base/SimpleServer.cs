using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ReverseProxy.Network.Server.Base
{
    public class SimpleServer : ServerBase
    {
        public SimpleServer(IPAddress address, int port, Func<TcpClient, Task> newClientHandler) : base(address, port)
        {
            NewClientHandler = newClientHandler;
        }

        public Func<TcpClient, Task> NewClientHandler { get; }

        protected override Task OnNewClient(TcpClient client)
        {
            return NewClientHandler(client);
        }
    }
}