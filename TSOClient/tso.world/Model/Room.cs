using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace FSO.LotView.Model
{
    public struct Room
    {
        public ushort RoomID;
        public sbyte Floor;
        public List<Vector2[]> WallLines;
        public List<Vector2[]> FenceLines;

        public bool IsOutside;
        public ushort Area;
        public bool IsPool;
        public ushort Base;

        public Rectangle Bounds;
    }
}
