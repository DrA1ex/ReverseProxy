using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using ReverseProxy.Common.Model;
using ReverseProxy.Network.Misc;
using ReverseProxy.Network.Packets;

namespace ReverseProxy.Network.Sessions
{
    public class PacketSession
    {
        public const int DesiredReceiveBufferSize = 1024 * 64; //64 KiB -- max size of the one TCP packet

        public PacketSession(IPacketReceiver packetReceiver)
        {
            PacketReceiver = packetReceiver;
        }

        protected Logger Logger { get; } = LogManager.GetCurrentClassLogger();

        public bool Started { get; private set; }

        private SocketWriteQueue WritingQueue { get; } = new SocketWriteQueue();
        private CancellationTokenSource CancellationSource { get; } = new CancellationTokenSource();

        public IPacketReceiver PacketReceiver { get; }

        public async Task Start(long sessionId, TcpClient client)
        {
            if(Started)
            {
                throw new InvalidOperationException("Already started");
            }

            try
            {
                Started = true;

                using(client)
                using(var stream = client.GetStream())
                {
                    try
                    {
                        Logger.Trace("Packet session started");

                        await Task.WhenAll(
                            ProcessIncomingData(sessionId, stream),
                            WritingQueue.Start(stream));

                        if(!CancellationSource.IsCancellationRequested)
                        {
                            await PacketReceiver.SendPacket(new Packet
                            {
                                SessionId = sessionId,
                                Type = PacketType.ConnectionClosed
                            });
                        }
                    }
                    catch(OperationCanceledException)
                    {
                        Logger.Debug("Session {0} closed due to cancellation", sessionId);
                    }
                    catch(Exception e)
                    {
                        Logger.Debug("Session {0} is closed due to error: {1}", sessionId, e);
                    }
                }
            }
            finally
            {
                Started = false;

                Logger.Trace("Packet session stoped");
            }
        }

        public void QueuePacket(Packet packet)
        {
            WritingQueue.QueueData(packet.Data);
        }

        private async Task ProcessIncomingData(long sessionId, NetworkStream stream)
        {
            Logger.Trace("Session incoming data processing started");

            try
            {
                var buffer = new byte[DesiredReceiveBufferSize];
                while(true)
                {
                    var read = await stream.ReadAsync(buffer, 0, buffer.Length, CancellationSource.Token);

                    if(read > 0)
                    {
                        Logger.Debug("Read response data for session {0}. Length: {1}", sessionId, read);

                        var message = new byte[read];
                        Buffer.BlockCopy(buffer, 0, message, 0, read);

                        await PacketReceiver.SendPacket(new Packet
                        {
                            SessionId = sessionId,
                            Type = PacketType.Message,
                            Data = message
                        });
                    }
                    else
                    {
                        break; //disconnected
                    }
                }
            }
            finally
            {
                WritingQueue.Stop();
                Logger.Trace("Session incoming data processing stoped");
            }
        }

        public void Stop()
        {
            CancellationSource.Cancel(true);
            WritingQueue.Stop();
        }
    }
}