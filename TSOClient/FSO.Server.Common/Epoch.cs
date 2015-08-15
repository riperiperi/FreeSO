using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Common
{
    public class Epoch
    {
        public static int Now
        {
            get
            {
                int epoch = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
                return epoch;
            }
        }
    }
}
