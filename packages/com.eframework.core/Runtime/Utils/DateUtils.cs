using System;

namespace EFrame.Runtime.Utils
{
    public static class DateUtils
    {
        // 获取当前UTC秒级时间戳
        public static long GetUtcSeconds()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        // 获取当前UTC毫秒级时间戳
        public static long GetUtcMilliseconds()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        // 时间戳（秒）转DateTime（UTC）
        public static DateTime FromUnixSeconds(long seconds)
        {
            return DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
        }

        // 时间戳（毫秒）转DateTime（UTC）
        public static DateTime FromUnixMilliseconds(long ms)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;
        }

        // DateTime转秒级时间戳（UTC）
        public static long ToUnixSeconds(DateTime dateTime)
        {
            return new DateTimeOffset(dateTime.ToUniversalTime()).ToUnixTimeSeconds();
        }

        // DateTime转毫秒级时间戳（UTC）
        public static long ToUnixMilliseconds(DateTime dateTime)
        {
            return new DateTimeOffset(dateTime.ToUniversalTime()).ToUnixTimeMilliseconds();
        }

        // 计算两个时间戳（秒）之间的天数
        public static int DaysBetween(long startSeconds, long endSeconds)
        {
            return (int)((endSeconds - startSeconds) / 86400);
        }

        // 计算两个DateTime之间的天数
        public static int DaysBetween(DateTime start, DateTime end)
        {
            return (int)(end.Date - start.Date).TotalDays;
        }

        // 格式化DateTime为字符串
        public static string Format(DateTime dateTime, string format = "yyyy-MM-dd HH:mm:ss")
        {
            return dateTime.ToString(format);
        }

        // 时间戳（秒）格式化为字符串
        public static string FormatUnixSeconds(long seconds, string format = "yyyy-MM-dd HH:mm:ss")
        {
            return FromUnixSeconds(seconds).ToString(format);
        }

        // 获取今天零点的UTC时间戳（秒）
        public static long GetTodayZeroUnixSeconds()
        {
            DateTime now = DateTime.UtcNow.Date;
            return ToUnixSeconds(now);
        }
    }
}
