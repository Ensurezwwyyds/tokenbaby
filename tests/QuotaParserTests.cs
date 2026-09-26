using System;
using System.Collections.Generic;

namespace TokenBaby
{
    internal static class QuotaParserTests
    {
        private static int failures;

        private static int Main()
        {
            MapsCurrentWindowsByDuration();
            MapsReversedWindowsByDuration();
            SupportsLegacyResponseWithoutDurations();
            ClampsUnexpectedPercentages();
            HandlesMissingLimits();
            Console.WriteLine(failures == 0 ? "QuotaParser: 5 checks passed" :
                "QuotaParser: " + failures + " checks failed");
            return failures == 0 ? 0 : 1;
        }

        private static void MapsCurrentWindowsByDuration()
        {
            var result = Map("rateLimitsByLimitId", Map("codex", Map(
                "primary", Window(300, 25), "secondary", Window(10080, 75))));
            QuotaSnapshot snapshot = QuotaParser.ParseSnapshot(result);
            Check("current 5h", snapshot.FiveHour.RemainingPercent, 75);
            Check("current 7d", snapshot.SevenDay.RemainingPercent, 25);
        }

        private static void MapsReversedWindowsByDuration()
        {
            var result = Map("rateLimits", Map(
                "primary", Window(10080, 20), "secondary", Window(300, 65)));
            QuotaSnapshot snapshot = QuotaParser.ParseSnapshot(result);
            Check("reversed 5h", snapshot.FiveHour.RemainingPercent, 35);
            Check("reversed 7d", snapshot.SevenDay.RemainingPercent, 80);
        }

        private static void SupportsLegacyResponseWithoutDurations()
        {
            var result = Map("rateLimits", Map(
                "primary", Map("usedPercent", 0), "secondary", Map("usedPercent", 100)));
            QuotaSnapshot snapshot = QuotaParser.ParseSnapshot(result);
            Check("legacy 5h", snapshot.FiveHour.RemainingPercent, 100);
            Check("legacy 7d", snapshot.SevenDay.RemainingPercent, 0);
        }

        private static void ClampsUnexpectedPercentages()
        {
            var result = Map("rateLimits", Map(
                "primary", Window(300, -5), "secondary", Window(10080, 150)));
            QuotaSnapshot snapshot = QuotaParser.ParseSnapshot(result);
            Check("clamped high", snapshot.FiveHour.RemainingPercent, 100);
            Check("clamped low", snapshot.SevenDay.RemainingPercent, 0);
        }

        private static void HandlesMissingLimits()
        {
            if (QuotaParser.ParseSnapshot(Map("other", 1)) != null)
                Fail("missing rate limits should return null");
        }

        private static Dictionary<string, object> Window(int minutes, int used)
        {
            return Map("windowDurationMins", minutes, "usedPercent", used);
        }

        private static Dictionary<string, object> Map(params object[] values)
        {
            var result = new Dictionary<string, object>();
            for (int i = 0; i < values.Length; i += 2)
                result[(string)values[i]] = values[i + 1];
            return result;
        }

        private static void Check(string name, double? actual, double expected)
        {
            if (!actual.HasValue || Math.Abs(actual.Value - expected) > 0.001)
                Fail(name + ": expected " + expected + ", got " +
                    (actual.HasValue ? actual.Value.ToString() : "null"));
        }

        private static void Fail(string message)
        {
            failures++;
            Console.Error.WriteLine(message);
        }
    }
}
