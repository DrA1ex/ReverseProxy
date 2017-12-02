using System.Reflection;
using System.Runtime.CompilerServices;

namespace ReverseProxy.Test.Utils
{
    internal static class TcpUtils
    {
        public static int GetPortNumber<T>(this T instance, int startPort, [CallerMemberName] string callerName = "") where T : class
        {
            var methodIndex = instance.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .FindIndex(m => m.Name == callerName);

            if(methodIndex > 0)
            {
                return startPort + methodIndex;
            }

            return startPort;
        }
    }
}