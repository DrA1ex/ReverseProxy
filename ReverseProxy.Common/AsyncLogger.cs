using System;
using System.Collections.Concurrent;
using System.Threading;

namespace ReverseProxy.Common
{
    public enum LogLevel
    {
        Debug,
        Info,
        Error,
        None
    }

    public abstract class AsyncLogger : IDisposable
    {
        private readonly ManualResetEvent _hasNewItems = new ManualResetEvent(false);
        private readonly Thread _loggingThread;
        private readonly ConcurrentQueue<Action> _queue = new ConcurrentQueue<Action>();
        private readonly ManualResetEvent _terminate = new ManualResetEvent(false);
        private readonly ManualResetEvent _waiting = new ManualResetEvent(false);
        private bool _disposed;

        protected AsyncLogger()
        {
            _loggingThread = new Thread(ProcessQueue) {IsBackground = true};

            _loggingThread.Start();
        }

        public void Dispose()
        {
            Dispose(false);
        }

        ~AsyncLogger()
        {
            if(!_disposed)
            {
                Dispose(true);
            }
        }

        protected void Dispose(bool fromFinalization)
        {
            if(!_disposed)
            {
                if(fromFinalization)
                {
                    GC.SuppressFinalize(this);
                }

                _disposed = true;
                _terminate.Set();
                _loggingThread.Join();
            }
        }


        private void ProcessQueue()
        {
            while(true)
            {
                _waiting.Set();
                var i = WaitHandle.WaitAny(new WaitHandle[] {_hasNewItems, _terminate});
                // terminate was signaled 
                if(i == 1)
                {
                    return;
                }
                _hasNewItems.Reset();
                _waiting.Reset();

                while(_queue.TryDequeue(out var log))
                {
                    log();
                }
            }
        }

        public void LogMessage(string message, LogLevel level)
        {
            _queue.Enqueue(() => AsyncLogMessage(message, level));
            _hasNewItems.Set();
        }

        protected abstract void AsyncLogMessage(string message, LogLevel level);

        public void Flush()
        {
            _waiting.WaitOne();
        }
    }
}