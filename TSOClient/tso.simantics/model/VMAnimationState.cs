using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Vitaboy;

namespace TSO.Simantics.model
{
    public class VMAnimationState {
        public int CurrentFrame;
        public short EventCode;
        public bool EventFired;
        public bool EndReached;
        public bool PlayingBackwards;
        public List<TimePropertyListItem> TimePropertyLists = new List<TimePropertyListItem>();
    }
}
