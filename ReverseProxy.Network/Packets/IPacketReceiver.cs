using System;
using System.Threading.Tasks;
using ReverseProxy.Common;

namespace ReverseProxy.Network.Packets
{
    public interface IPacketReceiver
    {
        Task SendPacket(Packet packet);

        event EventHandler<Packet> OnNewPacketReceived;
    }
}