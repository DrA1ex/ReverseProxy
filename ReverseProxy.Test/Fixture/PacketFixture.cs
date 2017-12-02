using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ReverseProxy.Common;
using ReverseProxy.Common.Model;
using ReverseProxy.Test.Utils;

namespace ReverseProxy.Test.Fixture
{
    static class PacketFixture
    {
        public static Random Random { get;  } = new Random();

        public static long PacketNumber { get; private set; }

        public static Packet[] GetPacketSequence(int length, int maxSize = 1024*1024)
        {
            var result = new Packet[length];

            for(int i = 0; i < length; i++)
            {
                result[i] = GetPacket(maxSize);
            }

            return result;
        }

        public static Packet GetPacket(int maxSize = 1024 * 1024)
        {
            var packetData = new byte[Random.Next(1, maxSize)];
            Random.NextBytes(packetData);

            return new Packet
            {
                Data = packetData,
                SessionId = PacketNumber++
            };
        }
    }
}
