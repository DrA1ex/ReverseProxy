using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ReverseProxy.Network.Server.Base
{
    public abstract class ServerBase : IServer, IDisposable
    {
        protected ServerBase(IPAddress address, int port)
        {
            TcpListener = new TcpListener(address, port);
        }

        public bool Active { get; private set; }
        private TcpListener TcpListener { get; }

        public virtual void Dispose()
        {
            if(Active)
            {
                Stop();
            }
        }

        public virtual void Start()
        {
            if(Active)
            {
                throw new InvalidOperationException("Server is already running");
            }

            TcpListener.Start();
            Active = true;

            ProcessNewClientsConnected();
        }

        private async void ProcessNewClientsConnected()
        {
            while(Active)
            {
                var client = await TcpListener.AcceptTcpClientAsync();
                await OnNewClient(client);
            }
        }

        protected abstract Task OnNewClient(TcpClient client);

        public virtual void Stop()
        {
            if(!Active)
            {
                throw new InvalidOperationException("Server is not running");
            }

            TcpListener.Stop();
            Active = false;
        }
    }
}