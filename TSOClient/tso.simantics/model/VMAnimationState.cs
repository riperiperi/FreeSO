using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.vitaboy;

namespace tso.simantics.model
{
    public class VMAnimationState {
        public uint CurrentFrame;
        public short EventCode;
        public bool EventFired;
        public bool EndReached;
        public bool PlayingBackwards;
        public List<TimePropertyListItem> TimePropertyLists = new List<TimePropertyListItem>();
    }
}
