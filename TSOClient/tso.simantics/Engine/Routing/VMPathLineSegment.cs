using System;
using System.Collections.Generic;
using FSO.LotView.Components;
using FSO.LotView.Model;
using FSO.SimAntics.Model.Routing;
using Microsoft.Xna.Framework;

namespace FSO.SimAntics.Engine.Routing
{
    public class VMPathLineSegment : VMIPathSegment
    {
        public Point From;
        public Point To;

        public int TotalFrames;

        public Point Source => From;
        public Point Destination => To;

        private static int STANDARD_VELOCITY = 20 / 2;

        public VMPathLineSegment(Point from, Point to)
        {
            From = from;
            To = to;
        }

        public static VMPathLineSegment LoP(Point from, Point to)
        {
            return new VMPathLineSegment(new Point(from.X * 0x8000, from.Y * 0x8000), new Point(to.X * 0x8000, to.Y * 0x8000));
        }

        public int CalculateTotalFrames()
        {
            var a = To;
            var b = From;
            TotalFrames = (int)(Math.Sqrt((long)(a.X - b.X) * (a.X - b.X) + (long)(a.Y - b.Y) * (a.Y - b.Y))/0x8000) * STANDARD_VELOCITY;
            if (TotalFrames == 0) TotalFrames = 1;
            return TotalFrames;
        }

        public void UpdateTotalFrames(int frames)
        {
            TotalFrames = Math.Max(1, frames);
        }

        public void ResetToFrame(int frame)
        {
            //no-op
        }

        Tuple<LotTilePos, Vector2, Vector2> VMIPathSegment.NextPointAndVel(int frame)
        {
            var diff = To - From;
            if (TotalFrames == 0) { }
            var velocity = diff.ToVector2() / TotalFrames;
            diff.X = (int)(((long)diff.X * frame) / TotalFrames);
            diff.Y = (int)(((long)diff.Y * frame) / TotalFrames);

            var result = new LotTilePos((short)((From.X + diff.X)/0x8000), (short)((From.Y + diff.Y)/0x8000), 0);
            var visual = Vector2.Lerp(From.ToVector2(), To.ToVector2(), ((float)frame / TotalFrames));
            return new Tuple<LotTilePos, Vector2, Vector2>(result, visual/(0x8000*16), velocity/ (0x8000 * 16));
        }

        // STATIC

        public static LinkedList<VMIPathSegment> GeneratePath(LinkedList<VMWalkableRect> rects, Point dest)
        {
            if (rects == null) return null;
            var result = new LinkedList<VMIPathSegment>();
            VMWalkableRect last = null;
            dest = new Point(dest.X * 0x8000, dest.Y * 0x8000);
            foreach (var rect in rects)
            {
                if (last != null)
                {
                    //line between the two ParentSource points
                    result.AddLast(new VMPathLineSegment(last.ParentSourceHiP, rect.ParentSourceHiP));
                }
                last = rect;
            }
            if (last != null)
            {
                result.AddLast(new VMPathLineSegment(last.ParentSourceHiP, dest));
            }
            return result;
        }

        public void AddToPath(List<Vector2> list, bool start)
        {
            if (start) list.Add(From.ToVector2() / (0x8000 * 16));
            list.Add(To.ToVector2() / (0x8000 * 16));
        }

        public void AddDebugExtras(DebugLinesComponent comp)
        {

        }
    }
}
