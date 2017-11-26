using System;
using System.Threading;
using System.Threading.Tasks;

namespace ReverseProxy.Common.Utils
{
    public static class WaitHandleUtils
    {
        public static Task<bool> WaitOneAsync(this WaitHandle waitHandle, TimeSpan? timeout = null)
        {
            if(waitHandle == null)
            {
                throw new ArgumentNullException(nameof(waitHandle));
            }

            var tcs = new TaskCompletionSource<bool>();
            var rwh = ThreadPool.RegisterWaitForSingleObject(waitHandle,
                (state, expired) => tcs.TrySetResult(!expired),
                null,
                timeout.HasValue ? (long)timeout.Value.TotalMilliseconds : -1,
                true);
            var t = tcs.Task;
            t.ContinueWith(antecedent => rwh.Unregister(null));
            return t;
        }
    }
}