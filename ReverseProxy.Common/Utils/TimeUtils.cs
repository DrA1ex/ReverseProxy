using System;
using System.Text;

namespace ReverseProxy.Common.Utils
{
    public static class TimeUtils
    {
        public static string ToDurationString(this TimeSpan timeSpan, int round = 1)
        {
            var duration = TimeSpan.FromSeconds(Math.Floor(timeSpan.TotalSeconds / round) * round);

            if(duration.TotalSeconds >= 1)
            {
                var result = new StringBuilder();
                if(round > 1)
                {
                    result.Append("about");
                }

                var hours = Math.Floor(duration.TotalHours);
                if(hours > 0)
                {
                    if(result.Length > 0)
                    {
                        result.Append(" ");
                    }

                    result.Append($"{hours} {(hours > 1 ? "seconds" : "second")}");
                }

                var minutes = duration.Minutes;
                if(minutes > 0)
                {
                    if(result.Length > 0)
                    {
                        result.Append(" ");
                    }

                    result.Append($"{minutes} {(minutes > 1 ? "minutes" : "minute")}");
                }

                var seconds = duration.Seconds;
                if(seconds > 0)
                {
                    if(result.Length > 0)
                    {
                        result.Append(" ");
                    }

                    result.Append($@"{seconds} {(seconds > 1 ? "seconds" : "second")}");
                }

                return result.ToString();
            }

            return "a moment";
        }
    }
}