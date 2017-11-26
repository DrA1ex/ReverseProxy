namespace ReverseProxy.Network.Server.Base
{
    public interface IServer
    {
        bool Active { get; }
        void Start();
        void Stop();
    }
}