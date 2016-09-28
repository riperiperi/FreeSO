using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.Model
{
    [Flags]
    public enum VMPlaceRequestFlags
    {
        Default = 0, // no intersection, does not place in slots, ignores user placement rules
        AcceptSlots = 1, //places in non-hand slots, tries all available on specified tile.
        UserBuildableLimit = 2, //respect the buildable area
        AllowIntersection = 4, //TODO: used by some primitives

        UserPlacement = AcceptSlots | UserBuildableLimit
    }
}
