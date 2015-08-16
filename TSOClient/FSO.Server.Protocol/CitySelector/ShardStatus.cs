using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Protocol.CitySelector
{
    public enum ShardStatus
    {
        Up,
	    Down,
	    Busy,
	    Full,
	    Closed,
	    Frontier
    }
}
