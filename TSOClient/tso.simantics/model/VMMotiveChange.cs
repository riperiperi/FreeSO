using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSO.Simantics.model
{
    public class VMMotiveChange
    {
        public short PerHourChange;
        public short MaxValue;
        public VMMotive Motive;
        private double fractional;

        public void Clear()
        {
            PerHourChange = 0;
            MaxValue = short.MaxValue;
        }

        public void Tick(VMAvatar avatar)
        {
            if (PerHourChange != 0)
            {
                double rate = (PerHourChange/60.0)/30.0;     //remember to fix when we implement the clock! right now assumes time for an hour is a realtime minute
                fractional += rate;
                if (Math.Abs(fractional) >= 1)
                {
                    var motive = avatar.GetMotiveData(Motive);
                    motive += (short)(fractional);
                    fractional %= 1.0;

                    if (motive > MaxValue) { motive = MaxValue; Clear(); }
                    avatar.SetMotiveData(Motive, motive);
                }
            }
        }
    }
}
