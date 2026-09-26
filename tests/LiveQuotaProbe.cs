using System;
using System.Threading;

namespace TokenBaby
{
    // A live read-only integration probe. It prints field availability, not account data.
    internal static class QuotaProbe
    {
        private static int Main()
        {
            using (var client = new CodexClient())
            using (var ready = new ManualResetEventSlim(false))
            {
                client.SnapshotReceived += snapshot =>
                {
                    Console.WriteLine("five_hour_available=" +
                        (snapshot.FiveHour != null && snapshot.FiveHour.RemainingPercent.HasValue));
                    Console.WriteLine("seven_day_available=" +
                        (snapshot.SevenDay != null && snapshot.SevenDay.RemainingPercent.HasValue));
                    ready.Set();
                };
                client.StatusChanged += status =>
                {
                    if (status.Contains("失败") || status.Contains("找不到") ||
                        status.Contains("无法") || status.Contains("请先"))
                        Console.Error.WriteLine(status);
                };
                client.Refresh(true);
                return ready.Wait(TimeSpan.FromSeconds(20)) ? 0 : 1;
            }
        }
    }
}
