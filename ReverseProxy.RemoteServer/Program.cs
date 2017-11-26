using System;
using System.Net;
using System.Threading;
using ReverseProxy.Common;
using ReverseProxy.Common.Utils;
using ReverseProxy.Network.Server;

namespace ReverseProxy.RemoteServer
{
    internal class Program
    {
        private static int PacketServerPort { get; set; }
        private static IPAddress PacketServerIpAddress { get; set; }

        private static int ExternalServerPort { get; set; }
        private static IPAddress ExternalServerIpAddress { get; set; }

        private static void Main()
        {
            try
            {
                LoadConfiguration();
            }
            catch(FormatException e)
            {
                LogUtils.LogErrorMessage("Unable to parse IPAddress: {0}", e.Message);
                return;
            }
            catch(Exception e)
            {
                LogUtils.LogErrorMessage("Parameters loading failed: {0}", e.Message);
                return;
            }

            try
            {
                var exitEvent = new ManualResetEvent(false);

                var packetServer = new PacketServer(PacketServerIpAddress, PacketServerPort);
                var proxyServer = new ProxyServer(ExternalServerIpAddress, ExternalServerPort)
                {
                    PacketReceiver = packetServer
                };

                packetServer.Start();
                proxyServer.Start();

                LogUtils.LogInfoMessage($"Started packet server at {PacketServerIpAddress}:{PacketServerPort}");
                LogUtils.LogInfoMessage($"Started proxy server at {ExternalServerIpAddress}:{ExternalServerPort}");
                LogUtils.LogInfoMessage("Waiting for agent...");

                exitEvent.WaitOne();
            }
            catch(Exception e)
            {
                LogUtils.LogException(e);
            }
        }

        private static void LoadConfiguration()
        {
            PacketServerPort = ConfigUtils.GetInt("packetServerPort");
            PacketServerIpAddress = IPAddress.Parse(ConfigUtils.GetString("packetServerIpAddress"));

            ExternalServerPort = ConfigUtils.GetInt("externalServerPort");
            ExternalServerIpAddress = IPAddress.Parse(ConfigUtils.GetString("externalServerIpAddress"));

            var logLevel = ConfigUtils.TryGetString("logLevel");
            Enum.TryParse(logLevel, true, out LogLevel level);

            LogUtils.Level = level;
        }
    }
}