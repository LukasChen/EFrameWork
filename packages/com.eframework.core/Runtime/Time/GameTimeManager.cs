using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;

namespace EFramework.Runtime.Time
{
    public sealed class GameTimeManager : ITimeService
    {
        private readonly List<string> m_serverUrls = new();
        private bool m_disposed;

        public GameTimeManager(IEnumerable<string> serverUrls = null)
        {
            if (serverUrls == null)
            {
                return;
            }

            foreach (string url in serverUrls)
            {
                AddServerTimeUrl(url);
            }
        }

        public IReadOnlyList<string> ServerTimeUrls => m_serverUrls;
        public TimeSpan ServerTimeOffset { get; private set; } = TimeSpan.Zero;
        public bool IsSynced { get; private set; }
        public DateTime LastSyncTime { get; private set; } = DateTime.MinValue;
        public long CurrentUnixSeconds => CurrentDateTimeOffset.ToUnixTimeSeconds();
        public DateTime CurrentLocalDateTime => CurrentDateTimeOffset.LocalDateTime;
        public DateTimeOffset CurrentDateTimeOffset => DateTimeOffset.UtcNow + ServerTimeOffset;

        public void AddServerTimeUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url) || m_serverUrls.Contains(url))
            {
                return;
            }

            m_serverUrls.Add(url);
        }

        public void ClearServerTimeUrls()
        {
            m_serverUrls.Clear();
        }

        public async Task<bool> SyncAsync()
        {
            if (m_serverUrls.Count == 0)
            {
                IsSynced = false;
                return false;
            }

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

            foreach (string url in m_serverUrls)
            {
                try
                {
                    DateTimeOffset localTimeBefore = DateTimeOffset.UtcNow;
                    using HttpResponseMessage response = await client.GetAsync(url);
                    DateTimeOffset localTimeAfter = DateTimeOffset.UtcNow;

                    if (response.IsSuccessStatusCode && response.Headers.Date.HasValue)
                    {
                        TimeSpan roundTripHalf = TimeSpan.FromTicks((localTimeAfter - localTimeBefore).Ticks / 2);
                        DateTimeOffset localMidTime = localTimeBefore + roundTripHalf;
                        ServerTimeOffset = response.Headers.Date.Value.ToUniversalTime() - localMidTime;
                        IsSynced = true;
                        LastSyncTime = DateTime.Now;
                        Debug.Log($"[GameTimeManager] Server time synced. Offset: {ServerTimeOffset.TotalSeconds:F2}s");
                        return true;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[GameTimeManager] Failed to sync from {url}: {e.Message}");
                }
            }

            ServerTimeOffset = TimeSpan.Zero;
            IsSynced = false;
            Debug.LogWarning("[GameTimeManager] Server time sync failed. Falling back to local time.");
            return false;
        }

        public void SetServerTimeOffset(TimeSpan offset)
        {
            ServerTimeOffset = offset;
            Debug.Log($"[GameTimeManager] Time offset set: {offset}");
        }

        public void AddServerTimeOffset(TimeSpan offset)
        {
            ServerTimeOffset += offset;
            Debug.Log($"[GameTimeManager] Time offset added: {offset}, Current offset: {ServerTimeOffset}");
        }

        public DateTime GetDateTimeFromTimestamp(long timestamp)
        {
            return DateTimeOffset.FromUnixTimeSeconds(timestamp).LocalDateTime;
        }

        public bool IsSameNaturalWeek(DateTime dateTime, DateTime other)
        {
            return GetMondayOfWeek(dateTime).Date == GetMondayOfWeek(other).Date;
        }

        public bool IsSameNaturalWeek(long timestamp)
        {
            return IsSameNaturalWeek(GetDateTimeFromTimestamp(timestamp), CurrentLocalDateTime);
        }

        public bool IsSameNaturalWeek(long timestamp1, long timestamp2)
        {
            return IsSameNaturalWeek(GetDateTimeFromTimestamp(timestamp1), GetDateTimeFromTimestamp(timestamp2));
        }

        public bool IsSameNaturalDay(DateTime dateTime, DateTime other)
        {
            return dateTime.Date == other.Date;
        }

        public bool IsSameNaturalDay(long timestamp)
        {
            return IsSameNaturalDay(GetDateTimeFromTimestamp(timestamp), CurrentLocalDateTime);
        }

        public bool IsSameNaturalMonth(long timestamp)
        {
            DateTime dateTime = GetDateTimeFromTimestamp(timestamp);
            DateTime now = CurrentLocalDateTime;
            return dateTime.Year == now.Year && dateTime.Month == now.Month;
        }

        public DateTime GetMondayOfWeek(DateTime date)
        {
            int daysFromMonday = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            return date.Date.AddDays(-daysFromMonday);
        }

        public int GetDayOfWeek()
        {
            return (int)CurrentLocalDateTime.DayOfWeek;
        }

        public string GetTodayTimeLeftHMS()
        {
            return GetTimeHMS(GetTodayTimeLeft());
        }

        public long GetTodayTimeLeft()
        {
            DateTime now = CurrentLocalDateTime;
            DateTime tomorrow = now.Date.AddDays(1);
            return System.Math.Max(0, (long)(tomorrow - now).TotalSeconds);
        }

        public string GetWeekTimeLeftDH()
        {
            return GetTimeDH(GetWeekTimeLeft());
        }

        public long GetWeekTimeLeft()
        {
            DateTime now = CurrentLocalDateTime;
            int daysUntilSunday = ((int)DayOfWeek.Sunday - (int)now.DayOfWeek + 7) % 7;
            DateTime nextMonday = now.Date.AddDays(daysUntilSunday).AddDays(1);
            return System.Math.Max(0, (long)(nextMonday - now).TotalSeconds);
        }

        public string GetMonthTimeLeftDHMS()
        {
            return GetTimeDHMS(GetMonthTimeLeft());
        }

        public long GetMonthTimeLeft()
        {
            DateTime now = CurrentLocalDateTime;
            DateTime firstDayOfNextMonth = new DateTime(now.Year, now.Month, 1).AddMonths(1);
            return System.Math.Max(0, (long)(firstDayOfNextMonth - now).TotalSeconds);
        }

        public long GetTimeLeft(long endTimestamp)
        {
            return System.Math.Max(0, endTimestamp - CurrentUnixSeconds);
        }

        public string FormatTimeRemaining(long endTimestamp)
        {
            return FormatDuration(endTimestamp - CurrentUnixSeconds);
        }

        public string FormatTimeRemaining(long endTimestamp, string finishedText)
        {
            long remaining = endTimestamp - CurrentUnixSeconds;
            return remaining <= 0 ? finishedText : FormatDuration(remaining);
        }

        public string FormatDuration(long seconds)
        {
            if (seconds <= 0)
            {
                return "Finished";
            }

            const int secondsPerDay = 86400;
            const int secondsPerHour = 3600;
            const int secondsPerMinute = 60;

            if (seconds >= secondsPerDay)
            {
                int days = (int)(seconds / secondsPerDay);
                int hours = (int)((seconds % secondsPerDay) / secondsPerHour);
                return hours > 0 ? $"{days}d {hours}h" : $"{days}d";
            }

            int h = (int)(seconds / secondsPerHour);
            int m = (int)((seconds % secondsPerHour) / secondsPerMinute);
            int s = (int)(seconds % secondsPerMinute);
            return h > 0 ? $"{h:00}:{m:00}:{s:00}" : $"{m:00}:{s:00}";
        }

        public string GetTimeHMS(long seconds)
        {
            if (seconds <= 0)
            {
                return "00:00";
            }

            int h = Mathf.FloorToInt(seconds / 3600);
            int m = Mathf.FloorToInt((seconds % 3600) / 60);
            int s = Mathf.FloorToInt(seconds % 60);
            return seconds >= 3600 ? $"{h:00}:{m:00}:{s:00}" : $"{m:00}:{s:00}";
        }

        public string GetTimeDHMS(long seconds)
        {
            return FormatDuration(seconds);
        }

        public string GetTimeDH(long seconds)
        {
            long day = System.Math.Max(0, seconds) / 86400;
            long hour = System.Math.Max(0, seconds) % 86400 / 3600;
            return hour > 0 ? $"{day}d {hour}h" : $"{day}d";
        }

        public void Dispose()
        {
            if (m_disposed)
            {
                return;
            }

            m_disposed = true;
            m_serverUrls.Clear();
            ServerTimeOffset = TimeSpan.Zero;
            IsSynced = false;
            LastSyncTime = DateTime.MinValue;
        }
    }
}
