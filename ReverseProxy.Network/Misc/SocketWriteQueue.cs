using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using ReverseProxy.Common.Utils;

namespace ReverseProxy.Network.Misc
{
    public class SocketWriteQueue
    {
        private ManualResetEvent DataAvailableEvent { get; } = new ManualResetEvent(false);
        private ManualResetEvent StopEvent { get; } = new ManualResetEvent(false);

        private ConcurrentQueue<byte[]> DataQueue { get; } = new ConcurrentQueue<byte[]>();

        public bool Started { get; private set; }

        public async Task Start(NetworkStream stream)
        {
            if(Started)
            {
                throw new InvalidOperationException("Already started");
            }

            try
            {
                Started = true;
                await ProcessQueue(stream);
            }
            finally
            {
                Started = false;
            }
        }

        public void Stop()
        {
            StopEvent.Set();
        }

        public void QueueData(byte[] data)
        {
            if(!Started)
            {
                throw new InvalidOperationException("Unable to queue element: Queue processing is not running");
            }

            DataQueue.Enqueue(data);
            DataAvailableEvent.Set();
        }

        private async Task ProcessQueue(NetworkStream stream)
        {
            while(true)
            {
                await Task.WhenAny(DataAvailableEvent.WaitOneAsync(), StopEvent.WaitOneAsync());

                if(!StopEvent.WaitOne(0))
                {
                    byte[] data;
                    while(!DataQueue.TryDequeue(out data))
                    {
                    }

                    if(DataQueue.IsEmpty)
                    {
                        DataAvailableEvent.Reset();
                    }

                    try
                    {
                        await stream.WriteAsync(data, 0, data.Length);
                    }
                    catch(Exception e)
                    {
                        LogUtils.LogDebugMessage("Unable to send data: {0}", e);
                        QueueData(data);

                        break;
                    }
                }
                else
                {
                    StopEvent.Reset();
                    break;
                }
            }
        }
    }
}