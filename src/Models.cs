using System;

namespace CodexQuotaWidget
{
    internal sealed class RateWindow
    {
        public bool IsAvailable { get; set; }
        public int UsedPercent { get; set; }
        public long ResetAfterSeconds { get; set; }
        public long ResetAtUnix { get; set; }

        public int RemainingPercent
        {
            get { return Math.Max(0, Math.Min(100, 100 - UsedPercent)); }
        }

        public bool IsLow
        {
            get { return IsAvailable && RemainingPercent <= 20; }
        }
    }

    internal sealed class UsageSnapshot
    {
        public bool IsOnline { get; set; }
        public string StatusMessage { get; set; }
        public string PlanType { get; set; }
        public string CreditsBalance { get; set; }
        public int AvailableResetCredits { get; set; }
        public DateTime UpdatedAt { get; set; }
        public RateWindow FiveHour { get; set; }
        public RateWindow Weekly { get; set; }
        public RateWindow SparkFiveHour { get; set; }
        public RateWindow SparkWeekly { get; set; }

        public static UsageSnapshot Loading()
        {
            return new UsageSnapshot
            {
                IsOnline = false,
                StatusMessage = "正在读取用量…",
                PlanType = "",
                CreditsBalance = "0",
                FiveHour = new RateWindow(),
                Weekly = new RateWindow(),
                SparkFiveHour = new RateWindow(),
                SparkWeekly = new RateWindow()
            };
        }

        public static UsageSnapshot Error(string message)
        {
            UsageSnapshot snapshot = Loading();
            snapshot.StatusMessage = message;
            snapshot.UpdatedAt = DateTime.Now;
            return snapshot;
        }
    }

    internal static class UsageText
    {
        public static string ResetCompact(long seconds)
        {
            if (seconds < 0) seconds = 0;
            TimeSpan span = TimeSpan.FromSeconds(seconds);
            if (span.TotalHours < 1)
                return Math.Max(1, span.Minutes) + "分后";
            if (span.TotalDays < 1)
                return ((int)span.TotalHours) + "时" + span.Minutes.ToString("00") + "分";
            return ((int)span.TotalDays) + "天" + span.Hours + "时";
        }

        public static string ResetLong(long seconds)
        {
            if (seconds < 0) seconds = 0;
            TimeSpan span = TimeSpan.FromSeconds(seconds);
            if (span.TotalDays >= 1)
                return ((int)span.TotalDays) + " 天 " + span.Hours + " 小时后重置";
            if (span.TotalHours >= 1)
                return ((int)span.TotalHours) + " 小时 " + span.Minutes + " 分钟后重置";
            return Math.Max(1, span.Minutes) + " 分钟后重置";
        }
    }
}
