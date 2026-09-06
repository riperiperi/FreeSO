using FSO.LotView.Model;

namespace FSO.LotView.Components.Model
{
    public struct SM64VisualState
    {
        public bool Active;
        public int GlobalAnimTimer;
        public float PosX;
        public float PosY;
        public float PosZ;
        public float ScaleX;
        public float ScaleY;
        public float ScaleZ;
        public short AngleX;
        public short AngleY;
        public short AngleZ;
        public short AnimID;
        public short AnimYTrans;
        public short AnimFrame;
        public ushort AnimTimer;
        public int AnimFrameAccelAssist;
        public int AnimAccel;

        public LotTilePos? ToPos()
        {
            return new LotTilePos((short)MathF.Round(PosX / 15f), (short)MathF.Round(PosZ / 15f), 1);
        }
    }
}
