using FSO.Common;
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
        public bool UltraLighting = FSOEnvironment.Enable3D;
        public int SurroundingLots = 0;
        public bool SmoothZoom = false;
        public bool AA = false;
        public bool Shadow3D = false;

        public int PassOffset
        {
            get {
                return (AdvancedLighting)?1:0;
            }
        }
    }
}
