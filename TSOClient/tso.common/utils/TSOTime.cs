using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.Utils
{
    public class TSOTime
    {
        public static Tuple<int,int,int> FromUTC(DateTime time)
        {
            var cycle = (time.Hour % 2 == 1) ? 3600 : 0;
            cycle += time.Minute * 60 + time.Second;
            return new Tuple<int, int, int>(cycle / 300, (cycle % 300) / 5, (cycle % 5)*12 + ((time.Millisecond * 12) / 1000));
        }
    }
}
