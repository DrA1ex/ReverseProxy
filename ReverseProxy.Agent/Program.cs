using System;
using System.Threading;
using NLog;
using ReverseProxy.Common.Utils;
using ReverseProxy.Network.Client;

namespace ReverseProxy.Agent
{
    internal class Program
    {
        private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();

        private static int PacketServerPort { get; set; }
        private static string PacketServerHost { get; set; }

        private static int SecuredServerPort { get; set; }
        private static string SecuredServerHost { get; set; }

        private static void Main()
        {
            try
            {
                LoadConfiguration();
            }
            catch(Exception e)
            {
                Logger.Error(e, "Parameters loading failed");
                return;
            }

            var exitEvent = new ManualResetEvent(false);

            try
            {
                Logger.Info($"Starting proxy agent for {SecuredServerHost}:{SecuredServerPort}");
                Logger.Info($"Connecting to packet server {PacketServerHost}:{PacketServerPort}");

                // ReSharper disable once UnusedVariable
                var proxyClient = new ProxyClient(SecuredServerHost, SecuredServerPort)
                {
                    PacketReceiver = new PacketClient(PacketServerHost, PacketServerPort)
                };

                exitEvent.WaitOne();
            }
            catch(Exception e)
            {
                Logger.Fatal(e);
            }
        }

        private static void LoadConfiguration()
        {
            PacketServerHost = ConfigUtils.GetString("packetServerHost");
            PacketServerPort = ConfigUtils.GetInt("packetServerPort");

            SecuredServerHost = ConfigUtils.GetString("securedServerHost");
            SecuredServerPort = ConfigUtils.GetInt("securedServerPort");
        }
    }
}