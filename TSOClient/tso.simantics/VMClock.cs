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
using FSO.SimAntics.Marshals;

namespace FSO.SimAntics
{
    public class VMClock
    {
        public long Ticks;
        public int MinuteFractions;
        public int TicksPerMinute;
        public int Minutes;
        public int Hours;
        public int TimeOfDay
        {
            get
            {
                //return (Hours >= 6 && Hours < 18) ? 0 : 1;
                return 0; //TODO: hack to make windows always cast full contribution. need to look into real patch.
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
            TicksPerMinute = 30; //30 * 5;
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

        public VMClock() { }

        #region VM Marshalling Functions
        public virtual VMClockMarshal Save()
        {
            return new VMClockMarshal
            {
                Ticks = Ticks,
                MinuteFractions = MinuteFractions,
                TicksPerMinute = TicksPerMinute,
                Minutes = Minutes,
                Hours = Hours
            };
        }

        public virtual void Load(VMClockMarshal input)
        {
            Ticks = input.Ticks;
            MinuteFractions = input.MinuteFractions;
            TicksPerMinute = input.TicksPerMinute;
            Minutes = input.Minutes;
            Hours = input.Hours;
        }

        public VMClock(VMClockMarshal input)
        {
            Load(input);
        }
        #endregion
    }
}
