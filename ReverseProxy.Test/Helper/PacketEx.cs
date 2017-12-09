using System;
using System.Linq;
using ReverseProxy.Common.Model;

namespace ReverseProxy.Test.Helper
{
#pragma warning disable 659
    internal class PacketEx : IEquatable<PacketEx>
    {
        public long SessionId { get; set; }
        public PacketType Type { get; set; }
        public byte[] Data { get; set; }

        public bool Equals(PacketEx other)
        {
            return SessionId == other.SessionId
                   && Type == other.Type
                   && (Data?.SequenceEqual(other.Data) ?? false);
        }

        public override bool Equals(object obj)
        {
            if(obj is PacketEx packet)
            {
                return Equals(packet);
            }

            return false;
        }

        public static implicit operator PacketEx(Packet packet)
        {
            return new PacketEx
            {
                SessionId = packet.SessionId,
                Type = packet.Type,
                Data = packet.Data
            };
        }

        public override string ToString()
        {
            return $"#{SessionId} of type {Type} with data ({Data?.Length ?? 0}) {(Data != null ? BitConverter.ToString(Data.Take(10).ToArray()) : "")}";
        }
    }
}
#pragma warning restore 659