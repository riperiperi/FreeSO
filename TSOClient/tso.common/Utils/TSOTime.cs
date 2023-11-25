using System;

namespace FSO.Common.Utils
{
    public class TSOTime
    {
        public static Tuple<int,int,int> FromUTC(DateTime time)
        {
            //var count = time.Minute * 60 * 1000 + time.Second * 1000 + time.Millisecond;
            //count *= 8;
            //count %= 1000 * 60 * 24;

            //var hour = count / (1000 * 60);
            //var min = (count / 1000) % 60;
            //var sec = ((count * 60) / 1000) % 60;

            var hour = time.Hour;
            var min = time.Minute;
            var sec = time.Second;
            var ms = time.Millisecond;

            var cycle = (hour % 2 == 1) ? 3600 : 0;
            cycle += min * 60 + sec;
            return new Tuple<int, int, int>(cycle / 300, (cycle % 300) / 5, (cycle % 5)*12 + ((ms * 12) / 1000));
        }
    }
}
