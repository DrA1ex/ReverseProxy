using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using ReverseProxy.Common.Utils;

namespace ReverseProxy.Test.Helper
{
    internal class WaitableQueue<T>
    {
        private ManualResetEvent DataAvailableEvent { get; } = new ManualResetEvent(false);

        private ConcurrentQueue<T> Queue { get; } = new ConcurrentQueue<T>();

        public WaitHandle WaitHandle => DataAvailableEvent;

        public void Enqueue(T data)
        {
            Queue.Enqueue(data);
            DataAvailableEvent.Set();
        }

        public async Task<T> Dequeue(TimeSpan? timeout = null)
        {
            if(!await WaitHandle.WaitOneAsync(timeout).ConfigureAwait(false))
            {
                throw new TimeoutException("Unable to dequeue element. Timeout has reached");
            }

            T value;
            while(!Queue.TryDequeue(out value))
            {
            }

            if(Queue.IsEmpty)
            {
                DataAvailableEvent.Reset();
            }

            return value;
        }
    }
}