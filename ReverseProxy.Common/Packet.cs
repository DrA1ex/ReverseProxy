using System;

namespace ReverseProxy.Common
{
    [Serializable]
    public class Packet
    {
        public Packet()
        {
        }

        public Packet(long id, byte[] data)
        {
            Id = id;
            Data = data;
        }

        public long Id { get; set; }

        public byte[] Data { get; set; }

        internal int MessageSize => sizeof(long) + sizeof(int) + Data.Length;
    }
}