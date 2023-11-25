using System;

namespace FSO.Common
{
    /// <summary>
    /// TODO: apply a time delta to sync with server time. right now we assume client time is correct.
    /// </summary>
    public class ClientEpoch
    {
        public static uint Now
        {
            get
            {
                uint epoch = (uint)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
                return epoch;
            }
        }

        public static uint FromDate(DateTime time)
        {
            return (uint)(time.ToUniversalTime() - new DateTime(1970, 1, 1)).TotalSeconds;
        }

        public static DateTime ToDate(uint time)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(time);
        }

        public static string HMSRemaining(uint date)
        {
            TimeSpan span = (ToDate(date) - ToDate(ClientEpoch.Now));

            return String.Format("{0} hours, {1} minutes and {2} seconds", (int)span.TotalHours, span.Minutes, span.Seconds);
        }

        public static string DHMRemaining(uint date)
        {
            if (date == uint.MaxValue) return "Permanent";
            TimeSpan span = (ToDate(date) - ToDate(ClientEpoch.Now));

            return String.Format("{0} days, {1} hours and {2} minutes", (int)span.TotalDays, span.Hours, span.Minutes);
        }

        public static uint Default
        {
            get { return 0; }
        }
    }
}
