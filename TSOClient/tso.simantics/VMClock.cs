using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace TSO.Simantics
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

        public void Tick(){
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
