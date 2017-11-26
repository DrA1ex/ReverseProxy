using System;
using System.Diagnostics;

namespace ReverseProxy.BinarySerialization.Utils
{
    internal static class TraceUtils
    {
        [Conditional("TRACE")]
        internal static void WriteLineFormatted(string format, params object[] args)
        {
            Debug.Assert(format != null);
            Debug.Assert(args != null);

            Trace.WriteLine(String.Format(format, args));
        }
    }
}