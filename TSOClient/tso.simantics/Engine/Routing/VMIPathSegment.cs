using FSO.LotView.Components;
using FSO.LotView.Model;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace FSO.SimAntics.Engine.Routing
{
    public interface VMIPathSegment
    {
        int CalculateTotalFrames();
        void UpdateTotalFrames(int total);
        Tuple<LotTilePos, Vector2, Vector2> NextPointAndVel(int Frame);
        void ResetToFrame(int frame);

        Point Source { get; }
        Point Destination { get; }
        void AddToPath(List<Vector2> list, bool start);
        void AddDebugExtras(DebugLinesComponent comp);
    }
}
