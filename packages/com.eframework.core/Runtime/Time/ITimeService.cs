using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EFramework.Runtime.Time
{
    public interface ITimeService : IDisposable
    {
        IReadOnlyList<string> ServerTimeUrls { get; }
        TimeSpan ServerTimeOffset { get; }
        bool IsSynced { get; }
        DateTime LastSyncTime { get; }
        long CurrentUnixSeconds { get; }
        DateTime CurrentLocalDateTime { get; }
        DateTimeOffset CurrentDateTimeOffset { get; }

        void AddServerTimeUrl(string url);
        void ClearServerTimeUrls();
        Task<bool> SyncAsync();
        void SetServerTimeOffset(TimeSpan offset);
        void AddServerTimeOffset(TimeSpan offset);
        DateTime GetDateTimeFromTimestamp(long timestamp);
        bool IsSameNaturalWeek(DateTime dateTime, DateTime other);
        bool IsSameNaturalWeek(long timestamp);
        bool IsSameNaturalWeek(long timestamp1, long timestamp2);
        bool IsSameNaturalDay(DateTime dateTime, DateTime other);
        bool IsSameNaturalDay(long timestamp);
        bool IsSameNaturalMonth(long timestamp);
        DateTime GetMondayOfWeek(DateTime date);
        int GetDayOfWeek();
        string GetTodayTimeLeftHMS();
        long GetTodayTimeLeft();
        string GetWeekTimeLeftDH();
        long GetWeekTimeLeft();
        string GetMonthTimeLeftDHMS();
        long GetMonthTimeLeft();
        long GetTimeLeft(long endTimestamp);
        string FormatTimeRemaining(long endTimestamp);
        string FormatTimeRemaining(long endTimestamp, string finishedText);
        string FormatDuration(long seconds);
        string GetTimeHMS(long seconds);
        string GetTimeDHMS(long seconds);
        string GetTimeDH(long seconds);
    }
}
