using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.LotView
{
    public class WorldConfig
    {
        public static WorldConfig Current = new WorldConfig();

        public bool AdvancedLighting = false;
        public int SurroundingLots = 0;
        public bool SmoothZoom = false;

        public int PassOffset
        {
            get {
                return (AdvancedLighting)?1:0;
            }
        }
    }
}
