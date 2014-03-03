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

        public void Update(GameTime time){

        }

        public void Tick(){
            this.Ticks++;
        }
    }
}
