/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace FSO.SimAntics
{
    public class VMClock
    {
        public long Ticks { get; internal set; }
        public int MinuteFractions;
        public int TicksPerMinute;
        public int Minutes;
        public int Hours;
        public int TimeOfDay
        {
            get
            {
                return (Hours >= 6 && Hours < 18) ? 1 : 0;
            }
        }
        public int Seconds
        {
            get
            {
                return ((MinuteFractions * 60) / TicksPerMinute);
            }
        }

        public void Tick()
        {
            TicksPerMinute = 30 * 5;
            if (++MinuteFractions >= TicksPerMinute)
            {
                MinuteFractions = 0;
                if (++Minutes >= 60) {
                    Minutes = 0;
                    if (++Hours >= 24)
                    {
                        Hours = 0;
                    }
                }
            }
            this.Ticks++;
        }
    }
}
