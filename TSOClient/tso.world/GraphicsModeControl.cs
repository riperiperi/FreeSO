using FSO.Common;
using FSO.LotView.Model;
using System;

namespace FSO.LotView
{
    public static class GraphicsModeControl
    {
        private static GlobalGraphicsMode _Mode = GlobalGraphicsMode.Full2D;
        public static event Action<GlobalGraphicsMode> ModeChanged;
        public static GlobalGraphicsMode Mode => _Mode;
        public static bool GlobalTransitionsEnabled => TransitionsEnabled(Mode);

        public static void ChangeMode(GlobalGraphicsMode mode)
        {
            if (!FSOEnvironment.Enable3D) return;
            _Mode = mode;
            ModeChanged?.Invoke(mode);
        }

        public static bool TransitionsEnabled(GlobalGraphicsMode mode)
        {
            return Mode > GlobalGraphicsMode.Full2D;
        }
    }
}
