using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using ReverseProxy.Common.Utils;

namespace ReverseProxy.Network.Misc
{
    public class StreamWriteQueue
    {
        protected Logger Logger { get; } = LogManager.GetCurrentClassLogger();

        private ManualResetEvent DataAvailableEvent { get; } = new ManualResetEvent(false);
        private ManualResetEvent StopEvent { get; } = new ManualResetEvent(false);

        private ConcurrentQueue<byte[]> DataQueue { get; } = new ConcurrentQueue<byte[]>();

        public bool Started { get; private set; }

        public async Task Start(Stream stream)
        {
            if(Started)
            {
                throw new InvalidOperationException("Already started");
            }

            try
            {
                Logger.Trace("Queue  is started");
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

            Logger.Trace("Queue new element {0}", data.GetHashCode());

            DataQueue.Enqueue(data);
            DataAvailableEvent.Set();
        }

        private async Task ProcessQueue(Stream stream)
        {
            while(true)
            {
                await Task.WhenAny(DataAvailableEvent.WaitOneAsync(), StopEvent.WaitOneAsync());

                if(!StopEvent.WaitOne(0))
                {
                    byte[] data;
                    if(!DataQueue.TryDequeue(out data))
                    {
                        Logger.Trace("Unable to deque element from Queue due to concurrency access");
                    }

                    if(DataQueue.IsEmpty)
                    {
                        Logger.Trace("Writing queue is empty. Wait for new element...");
                        DataAvailableEvent.Reset();
                    }

                    try
                    {
                        Logger.Trace("Write queue element {0}", data.GetHashCode());
                        await stream.WriteAsync(data, 0, data.Length);
                    }
                    catch(Exception e)
                    {
                        Logger.Debug(e, "Unable to send data");
                        QueueData(data);

                        break;
                    }
                }
                else
                {
                    Logger.Trace("Queue  is stopped");
                    StopEvent.Reset();
                    break;
                }
            }
        }
    }
}