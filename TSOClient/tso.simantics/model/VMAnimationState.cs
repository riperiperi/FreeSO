using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.vitaboy;

namespace tso.simantics.model
{
    public class VMAnimationState {
        public uint CurrentFrame;

        public List<TimePropertyListItem> TimePropertyLists = new List<TimePropertyListItem>();
    }
}
