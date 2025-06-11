using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonToLLM.Extensions
{
    public static class DateTimeExtensions
    {
        private static DateTime FirstUnixTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);

        // Convert datetime to UNIX time
        public static long? ToUnixTime(this DateTime dateTime)
        {
            if (dateTime < FirstUnixTime)
                return null;
            DateTimeOffset dto = new DateTimeOffset(dateTime.ToUniversalTime());
            return dto.ToUnixTimeSeconds();
        }

        // Convert datetime to UNIX time including miliseconds
        public static long? ToUnixTimeMilliSeconds(this DateTime dateTime)
        {
            if (dateTime < FirstUnixTime)
                return null;
            DateTimeOffset dto = new DateTimeOffset(dateTime.ToUniversalTime());
            return dto.ToUnixTimeMilliseconds();
        }
    }
}
