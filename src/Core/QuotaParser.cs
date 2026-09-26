using System;
using System.Collections.Generic;
using System.Globalization;

namespace TokenBaby
{
    internal static class QuotaParser
    {
        internal static QuotaSnapshot ParseSnapshot(Dictionary<string, object> result)
        {
            Dictionary<string, object> bucket = null;
            var buckets = GetMap(result, "rateLimitsByLimitId");
            if (buckets != null) bucket = GetMap(buckets, "codex");
            if (bucket == null) bucket = GetMap(result, "rateLimits");
            if (bucket == null) return null;

            var first = ParseWindow(GetMap(bucket, "primary"));
            var second = ParseWindow(GetMap(bucket, "secondary"));
            var snapshot = new QuotaSnapshot();
            snapshot.FetchedAt = DateTime.Now;
            if (first != null && first.DurationMinutes == 300) snapshot.FiveHour = first;
            if (second != null && second.DurationMinutes == 300) snapshot.FiveHour = second;
            if (first != null && first.DurationMinutes == 10080) snapshot.SevenDay = first;
            if (second != null && second.DurationMinutes == 10080) snapshot.SevenDay = second;
            // Older server versions may omit the duration; the primary/secondary order
            // is retained as a fallback, while the UI still labels unavailable data.
            if (snapshot.FiveHour == null && first != null && !first.DurationMinutes.HasValue)
                snapshot.FiveHour = first;
            if (snapshot.SevenDay == null && second != null && !second.DurationMinutes.HasValue)
                snapshot.SevenDay = second;
            return snapshot;
        }

        private static QuotaWindow ParseWindow(Dictionary<string, object> data)
        {
            if (data == null) return null;
            var window = new QuotaWindow();
            double? used = GetNumber(data, "usedPercent");
            if (used.HasValue)
                window.RemainingPercent = Math.Max(0, Math.Min(100, 100 - used.Value));
            double? minutes = GetNumber(data, "windowDurationMins");
            if (minutes.HasValue) window.DurationMinutes = (int)minutes.Value;
            double? unix = GetNumber(data, "resetsAt");
            if (unix.HasValue)
            {
                try { window.ResetsAt = DateTimeOffset.FromUnixTimeSeconds((long)unix.Value).LocalDateTime; }
                catch (ArgumentOutOfRangeException) { }
            }
            return window;
        }

        internal static Dictionary<string, object> GetMap(Dictionary<string, object> data, string key)
        {
            if (data == null) return null;
            object value;
            if (!data.TryGetValue(key, out value)) return null;
            return value as Dictionary<string, object>;
        }

        internal static string GetString(Dictionary<string, object> data, string key, string fallback)
        {
            object value;
            return data != null && data.TryGetValue(key, out value) && value != null
                ? Convert.ToString(value, CultureInfo.InvariantCulture) : fallback;
        }

        private static double? GetNumber(Dictionary<string, object> data, string key)
        {
            object value;
            if (data == null || !data.TryGetValue(key, out value) || value == null) return null;
            try { return Convert.ToDouble(value, CultureInfo.InvariantCulture); }
            catch (Exception) { return null; }
        }
    }
}

