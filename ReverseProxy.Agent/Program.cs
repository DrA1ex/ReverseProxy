using System;
using System.Diagnostics;
using System.Threading;
using ReverseProxy.Common;
using ReverseProxy.Common.Utils;
using ReverseProxy.Network.Client;

namespace ReverseProxy.Agent
{
    internal class Program
    {
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
                LogUtils.LogErrorMessage("Parameters loading failed: {0}", e);
                return;
            }

            var exitEvent = new ManualResetEvent(false);

            try
            {
                LogUtils.LogInfoMessage($"Starting proxy agent for {SecuredServerHost}:{SecuredServerPort}");
                LogUtils.LogInfoMessage($"Connecting to packet server {PacketServerHost}:{PacketServerPort}");

                // ReSharper disable once UnusedVariable
                var proxyClient = new ProxyClient(SecuredServerHost, SecuredServerPort)
                {
                    PacketReceiver = new PacketClient(PacketServerHost, PacketServerPort)
                };

                exitEvent.WaitOne();
            }
            catch(Exception e)
            {
                LogUtils.LogErrorMessage("Unable to connect to Packet server: {0}", e);
                Trace.WriteLine("Unable to connect to Packet server: {0}");
            }
        }

        private static void LoadConfiguration()
        {
            PacketServerHost = ConfigUtils.GetString("packetServerHost");
            PacketServerPort = ConfigUtils.GetInt("packetServerPort");

            SecuredServerHost = ConfigUtils.GetString("securedServerHost");
            SecuredServerPort = ConfigUtils.GetInt("securedServerPort");

            var logLevel = ConfigUtils.TryGetString("logLevel");
            Enum.TryParse(logLevel, true, out LogLevel level);

            LogUtils.Level = level;
        }
    }
}