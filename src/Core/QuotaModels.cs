using System;

namespace TokenBaby
{
    internal sealed class QuotaWindow
    {
        public double? RemainingPercent;
        public int? DurationMinutes;
        public DateTime? ResetsAt;
    }

    internal sealed class QuotaSnapshot
    {
        public QuotaWindow FiveHour;
        public QuotaWindow SevenDay;
        public DateTime FetchedAt;
    }
}

