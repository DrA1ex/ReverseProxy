using System;
using NLog;

namespace ReverseProxy.Common.Utils
{
    public class LogUtils : AsyncLogger
    {
        private static readonly Logger Logger = LogManager.GetLogger(typeof(LogUtils).Name);
        private static readonly LogUtils Instance = new LogUtils();

        public static LogLevel Level { get; set; } = LogLevel.Debug;

        public static void LogDebugMessage(string format, params object[] objs)
        {
            if(Level <= LogLevel.Debug)
            {
                LogMessageInternal(format, LogLevel.Debug, objs);
            }
        }

        public static void LogInfoMessage(string format, params object[] objs)
        {
            if(Level <= LogLevel.Info)
            {
                LogMessageInternal(format, LogLevel.Info, objs);
            }
        }

        public static void LogErrorMessage(string format, params object[] objs)
        {
            if(Level <= LogLevel.Error)
            {
                LogMessageInternal(format, LogLevel.Error, objs);
            }
        }

        public static void LogException(Exception e)
        {
            if(Level <= LogLevel.Error)
            {
                LogMessageInternal(e.ToString(), LogLevel.Error);
            }
        }

        private static void LogMessageInternal(string format, LogLevel category, params object[] objs)
        {
            Instance.LogMessage(string.Format(format, objs), category);
        }

        protected override void AsyncLogMessage(string message, LogLevel level)
        {
            switch(level)
            {
                case LogLevel.Debug:
                    Logger.Debug(message);
                    break;
                case LogLevel.Info:
                    Logger.Info(message);
                    break;
                case LogLevel.Error:
                    Logger.Error(message);
                    break;
            }
        }
    }
}