using System;

namespace FSO.Server.Database.DA.LotVisits
{
    public static class LotVisitUtils
    {
        /// <summary>
        /// Calculates the overlap between two date ranges.
        /// Useful utility function for calculating visit hours.
        /// </summary>
        /// <param name="day"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public static TimeSpan CalculateDateOverlap(DateTime r1_start, DateTime r1_end, DateTime r2_start, DateTime r2_end)
        {
            var startsInRange = r2_start >= r1_start && r2_start <= r1_end;
            var endsInRange = r2_end <= r1_end && r2_end >= r1_start;

            if (startsInRange && endsInRange)
            {
                //Within the range / equal
                return r2_end.Subtract(r2_start);
            }
            else if (startsInRange)
            {
                //Starts within range but does not end in range
                return r1_end.Subtract(r2_start);
            }
            else if (endsInRange)
            {
                //Ends in range but does not start in range
                return r2_end.Subtract(r1_start);
            }
            else
            {
                return new TimeSpan(0);
            }
        }

        /// <summary>
        /// Returns midnight of the current day.
        /// Useful utility function for calculating visit hours.
        /// </summary>
        public static DateTime Midnight()
        {
            var now = DateTime.UtcNow;
            return new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
        }
    }
}
