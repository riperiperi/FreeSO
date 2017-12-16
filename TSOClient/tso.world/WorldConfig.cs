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

        //(off, advanced, +3d wall, ultra)
        public int LightingMode;

        public bool AdvancedLighting
        {
            get
            {
                return (LightingMode > 0);
            }
        }
        public bool Shadow3D
        {
            get
            {
                return (LightingMode > 1);
            }
        }
        public bool UltraLighting
        {
            get
            {
                return (LightingMode > 2);
            }
        }
        public bool Weather = true;
        public int SurroundingLots = 0;
        public bool SmoothZoom = false;
        public bool AA = false;

        public int PassOffset
        {
            get {
                return (AdvancedLighting)?1:0;
            }
        }
    }
}
