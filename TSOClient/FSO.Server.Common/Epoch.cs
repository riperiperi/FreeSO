using System;

namespace FSO.Server.Common
{
    public class Epoch
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
            TimeSpan span = (ToDate(date) - ToDate(Epoch.Now));

            return String.Format("{0} hours, {1} minutes and {2} seconds", span.Hours, span.Minutes, span.Seconds);
        }

        public static uint Default
        {
            get { return 0; }
        }
    }
}
