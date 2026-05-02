using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;

namespace EFramework.Runtime.Utils
{
    /// <summary>
    /// 游戏时间服务 - 提供服务器时间同步和时间格式化功能
    /// </summary>
    public static class GameTimeService
    {
        private static readonly List<string> k_serverUrlList = new()
        {
            "https://www.microsoft.com",
            "https://www.google.com",
            "https://github.com",
            "https://www.baidu.com",
        };

        /// <summary>
        /// 服务器时间与本地时间的偏移量
        /// </summary>
        public static TimeSpan ServerTimeOffset { get; private set; } = TimeSpan.Zero;

        /// <summary>
        /// 是否已成功同步服务器时间
        /// </summary>
        public static bool IsSynced { get; private set; } = false;

        /// <summary>
        /// 最后一次同步时间
        /// </summary>
        public static DateTime LastSyncTime { get; private set; } = DateTime.MinValue;

        #region 初始化与同步

        /// <summary>
        /// 初始化并同步服务器时间
        /// </summary>
        public static async void Init()
        {
            await SyncServerTime();
        }

        /// <summary>
        /// 同步服务器时间（可手动调用重新同步）
        /// </summary>
        /// <returns>是否同步成功</returns>
        public static async Task<bool> SyncServerTime()
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

            foreach (string url in k_serverUrlList)
            {
                try
                {
                    var localTimeBefore = DateTime.UtcNow;
                    HttpResponseMessage response = await client.GetAsync(url);
                    var localTimeAfter = DateTime.UtcNow;

                    if (response.IsSuccessStatusCode && response.Headers.Date.HasValue)
                    {
                        DateTime serverTime = response.Headers.Date.Value.UtcDateTime;
                        // 使用请求前后的中间时间来减少网络延迟误差
                        var localMidTime = localTimeBefore + (localTimeAfter - localTimeBefore) / 2;
                        ServerTimeOffset = serverTime - localMidTime;
                        IsSynced = true;
                        LastSyncTime = DateTime.Now;
                        Debug.Log($"[GameTimeService] 服务器时间同步成功，偏移量: {ServerTimeOffset.TotalSeconds:F2}s");
                        return true;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[GameTimeService] 从 {url} 获取时间失败: {e.Message}");
                }
            }

            // 所有请求失败
            Debug.LogWarning("[GameTimeService] 所有服务器时间同步失败，使用本地时间");
            ServerTimeOffset = TimeSpan.Zero;
            IsSynced = false;
            return false;
        }

        /// <summary>
        /// 手动设置服务器时间偏移（用于GM调试）
        /// </summary>
        public static void SetServerTimeOffset(TimeSpan offset)
        {
            ServerTimeOffset = offset;
            Debug.Log($"[GameTimeService] GM设置时间偏移: {offset}");
        }

        /// <summary>
        /// 增加服务器时间偏移（用于GM调试）
        /// </summary>
        public static void AddServerTimeOffset(TimeSpan offset)
        {
            ServerTimeOffset += offset;
            Debug.Log($"[GameTimeService] GM增加时间偏移: {offset}, 当前总偏移: {ServerTimeOffset}");
        }

        #endregion

        #region 当前时间属性

        /// <summary>
        /// 当前服务器时间戳（Unix秒）
        /// </summary>
        public static long CurrentTime => CurrentDateTimeOffset.ToUnixTimeSeconds();

        /// <summary>
        /// 当前服务器时间（本地时区）
        /// </summary>
        public static DateTime CurrentDateTime => DateTime.Now + ServerTimeOffset;

        /// <summary>
        /// 当前服务器时间（带时区信息）
        /// </summary>
        public static DateTimeOffset CurrentDateTimeOffset => new(CurrentDateTime);

        /// <summary>
        /// 将时间戳转换为本地DateTime
        /// </summary>
        public static DateTime GetDateTimeFromTimestamp(long timestamp)
        {
            return DateTimeOffset.FromUnixTimeSeconds(timestamp).LocalDateTime;
        }

        #endregion

        #region 自然周期判断

        /// <summary>
        /// 判断两个时间是否在同一自然周（周一到周日为一周）
        /// </summary>
        public static bool IsSameNaturalWeek(DateTime dateTime, DateTime other)
        {
            // 获取两个日期所在周的周一日期（作为周的标识）
            DateTime mondayOfDateTime = GetMondayOfWeek(dateTime);
            DateTime mondayOfOther = GetMondayOfWeek(other);

            // 比较两个周一的日期是否相同
            return mondayOfDateTime.Date == mondayOfOther.Date;
        }

        public static bool IsSameNaturalWeek(long timestamp1)
        {
            DateTime dateTime1 = GetDateTimeFromTimestamp(timestamp1);
            return IsSameNaturalWeek(dateTime1, CurrentDateTime);
        }

        public static bool IsSameNaturalWeek(long timestamp1, long timestamp2)
        {
            DateTime dateTime1 = GetDateTimeFromTimestamp(timestamp1);
            DateTime dateTime2 = GetDateTimeFromTimestamp(timestamp2);
            return IsSameNaturalWeek(dateTime1, dateTime2);
        }

        /// <summary>
        /// 判断两个时间是否在同一自然日期
        /// </summary>
        /// <param name="dateTime"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool IsSameNaturalDay(DateTime dateTime, DateTime other)
        {
            return dateTime.Date == other.Date;
        }
        public static bool IsSameNaturalDay(long timestamp)
        {
            DateTime dateTime = GetDateTimeFromTimestamp(timestamp);

            //    Debug.LogWarning($"IsSameNaturalDay check: {dateTime} VS {CurrentDateTime}");
            return IsSameNaturalDay(dateTime, CurrentDateTime);
        }

        public static bool IsSameNaturalMonth(long timestamp)
        {
            DateTime dateTime = GetDateTimeFromTimestamp(timestamp);
            DateTime now = CurrentDateTime;
            return dateTime.Year == now.Year && dateTime.Month == now.Month;
        } 

        /// <summary>
        /// 获取指定日期所在周的周一日期（凌晨0点）
        /// </summary>
        public static DateTime GetMondayOfWeek(DateTime date)
        {
            // 计算距离周一的天数
            int daysFromMonday = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;

            // 返回本周周一的日期（凌晨0点）
            return date.Date.AddDays(-daysFromMonday);
        }

        public static int GetDayOfWeek()
        {
            return (int)CurrentDateTime.DayOfWeek;
        }

        #endregion

        #region 剩余时间计算

        public static string GetTodayTimeLeftHMS()
        {
            return GetTimeHMS(GetTodayTimeLeft());
        }

        public static long GetTodayTimeLeft()
        {
            DateTime now = CurrentDateTime;
            DateTime tomorrow = now.Date.AddDays(1);
            TimeSpan timeLeft = tomorrow - now;
            long sec = (long)timeLeft.TotalSeconds;
            return sec;
        }

        public static string GetWeekTimeLeftDH()
        {
            return GetTimeDH(GetWeekTimeLeft());
        }

        public static long GetWeekTimeLeft()
        {
            DateTime now = CurrentDateTime;
            // 计算到本周周日的天数（周一..周日 为一周）
            int daysUntilSunday = ((int)DayOfWeek.Sunday - (int)now.DayOfWeek + 7) % 7;
            // 本周周日晚上12点（即下周一凌晨0点）
            DateTime thisSundayMidnight = now.Date.AddDays(daysUntilSunday).AddDays(1);
            TimeSpan timeLeft = thisSundayMidnight - now;
            long sec = (long)timeLeft.TotalSeconds;
            return sec > 0 ? sec : 0;
        }

        public static string GetMonthTimeLeftDHMS()
        {
            return GetTimeDHMS(GetMonthTimeLeft());
        }

        public static long GetMonthTimeLeft()
        {
            DateTime now = CurrentDateTime;
            // 下个月的第一天凌晨0点
            DateTime firstDayOfNextMonth = new DateTime(now.Year, now.Month, 1).AddMonths(1);
            TimeSpan timeLeft = firstDayOfNextMonth - now;
            long sec = (long)timeLeft.TotalSeconds;
            return sec > 0 ? sec : 0;
        }

        public static long GetTimeLeft(long endTimestamp)
        {
            long sec = endTimestamp - CurrentTime;
            return sec > 0 ? sec : 0;
        }

        public static string GetTimeHMS(long sec)
        {
            if (sec > 0)
            {
                int hours = Mathf.FloorToInt(sec / 3600);
                int minutes = Mathf.FloorToInt((sec % 3600) / 60);
                int seconds = Mathf.FloorToInt(sec % 60);

                if (sec >= 3600) // 大于等于1小时
                {
                    return $"{hours:00}:{minutes:00}:{seconds:00}";
                }
                else // 小于1小时
                {
                    return $"{minutes:00}:{seconds:00}";
                }
            }
            else
            {
                return $"00:00";
            }
        }
        
        public static string GetTimeDHMS(long sec)
        {
            if (sec > 0)
            {
                int days = Mathf.FloorToInt(sec / 86400);
                int hours = Mathf.FloorToInt((sec % 86400) / 3600);
                int minutes = Mathf.FloorToInt((sec % 3600) / 60);
                int seconds = Mathf.FloorToInt(sec % 60);

                if (days > 0) // 大于等于1天
                {
                    return $"{days}d {hours:0}h";
                }
                else if (hours > 0) // 大于等于1小时
                {
                    return $"{hours:00}:{minutes:00}:{seconds:00}";
                }
                else // 小于1小时
                {
                    return $"{minutes:00}:{seconds:00}";
                }
            }
            return "Finished";
        }

        public static string GetTimeDH(long sec)
        {
            long day = sec / 86400;
            long hour = sec % 86400 / 3600;
            return hour > 0 ? $"{day}d {hour}h" : $"{day}d";
        }

        #endregion

        #region 智能格式化接口

        /// <summary>
        /// 智能格式化剩余时间
        /// - 已过期：返回 "Finished"
        /// - >= 1天：返回 "Xd Xh" 格式
        /// - < 1天：返回 "HH:MM:SS" 或 "MM:SS" 格式
        /// </summary>
        /// <param name="endTimestamp">目标时间戳（Unix秒）</param>
        /// <returns>格式化后的时间字符串</returns>
        public static string FormatTimeRemaining(long endTimestamp)
        {
            long remaining = endTimestamp - CurrentTime;
            return FormatDuration(remaining);
        }

        /// <summary>
        /// 智能格式化时间间隔
        /// - <= 0：返回 "Finished"
        /// - >= 1天：返回 "Xd Xh" 格式
        /// - < 1天：返回 "HH:MM:SS" 或 "MM:SS" 格式
        /// </summary>
        /// <param name="seconds">时间间隔（秒）</param>
        /// <returns>格式化后的时间字符串</returns>
        public static string FormatDuration(long seconds)
        {
            if (seconds <= 0)
                return "Finished";

            const int SecondsPerDay = 86400;
            const int SecondsPerHour = 3600;
            const int SecondsPerMinute = 60;

            if (seconds >= SecondsPerDay)
            {
                // >= 1天：显示 Xd Xh
                int days = (int)(seconds / SecondsPerDay);
                int hours = (int)((seconds % SecondsPerDay) / SecondsPerHour);
                return hours > 0 ? $"{days}d {hours}h" : $"{days}d";
            }
            else
            {
                // < 1天：显示 HH:MM:SS 或 MM:SS
                int hours = (int)(seconds / SecondsPerHour);
                int minutes = (int)((seconds % SecondsPerHour) / SecondsPerMinute);
                int secs = (int)(seconds % SecondsPerMinute);

                return hours > 0
                    ? $"{hours:00}:{minutes:00}:{secs:00}"
                    : $"{minutes:00}:{secs:00}";
            }
        }

        /// <summary>
        /// 格式化时间，支持自定义过期文本
        /// </summary>
        /// <param name="endTimestamp">目标时间戳（Unix秒）</param>
        /// <param name="finishedText">过期时显示的文本</param>
        /// <returns>格式化后的时间字符串</returns>
        public static string FormatTimeRemaining(long endTimestamp, string finishedText)
        {
            long remaining = endTimestamp - CurrentTime;
            if (remaining <= 0)
                return finishedText;
            return FormatDuration(remaining);
        }

        #endregion
    }
}
