using System;
using System.Threading.Tasks;

namespace ReverseProxy.Test.Utils
{
    internal static class TaskUtils
    {
        internal static async Task WithTimeOut(this Task task, TimeSpan timeout)
        {
            var timeoutTask = Task.Delay(timeout);
            var finishedTask = await Task.WhenAny(task, timeoutTask);

            if(finishedTask != task)
            {
                throw new TimeoutException();
            }
        }

        internal static async Task<T> WithTimeOut<T>(this Task<T> task, TimeSpan timeout)
        {
            var timeoutTask = Task.Delay(timeout);
            var finishedTask = await Task.WhenAny(task, timeoutTask);

            if(finishedTask == task)
            {
                return await task;
            }

            throw new TimeoutException();
        }
    }
}