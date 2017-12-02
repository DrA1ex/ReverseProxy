namespace ReverseProxy.Common.Model
{
    public enum PacketType : byte
    {
        Message,
        ConnectionClosed
    }

    public class Packet
    {
        public long SessionId { get; set; }
        public PacketType Type { get; set; }
        public byte[] Data { get; set; }
    }
}