using FSO.Common;
using FSO.LotView.Model;

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
        public bool SmoothZoom
        {
            get
            {
                return _EnableTransitions;
            }
            set
            {

            }
        }
        public int AA = 0;
        public bool Directional = true;
        public bool Complex = false;

        private bool _EnableTransitions = false;
        public bool EnableTransitions
        {
            get
            {
                return _EnableTransitions;
            }
            set
            {
                _EnableTransitions = FSOEnvironment.Enable3D && value;
            }
        }

        public GlobalGraphicsMode Mode = GlobalGraphicsMode.Hybrid2D;

        public int PassOffset
        {
            get {
                return (AdvancedLighting)?1:0;
            }
        }

        public int DirPassOffset
        {
            get
            {
                return (AdvancedLighting) ? ((Directional)?1:1) : 0;
            }
        }
    }
}
