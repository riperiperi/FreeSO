using FSO.LotView.Components;
using FSO.LotView.Model;
using FSO.SimAntics.Model.Routing;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.Engine.Routing
{
    public class VMPathBezierSegment : VMIPathSegment
    {
        public Point A; //start point
        public Point B; //control point 1
        public Point C; //control point 2
        public Point D; //end point

        //runtime info
        public float[] ArcLengths;
        public int TotalFrames;
        public int LastArcLengthInd = 0;
        private Vector2? Last;

        //params
        private static int SUBDIV_COUNT = 20;
        private static int STANDARD_VELOCITY = 20 / 2;

        public Point Source => A;// new Point(A.X/0x8000, A.Y/0x8000);
        public Point Destination => D;// new Point(D.X / 0x8000, D.Y / 0x8000);

        private Vector2 GetInterp(float t)
        {
            //good luck optimising THIS, Roslyn
            return new Vector2(
               ((1 - t) * (1 - t) * (1 - t)) * A.X
               + 3 * ((1 - t) * (1 - t)) * t * B.X
               + 3 * (1 - t) * (t * t) * C.X
               + (t * t * t) * D.X,

               ((1 - t) * (1 - t) * (1 - t)) * A.Y
               + 3 * ((1 - t) * (1 - t)) * t * B.Y
               + 3 * (1 - t) * (t * t) * C.Y
               + (t * t * t) * D.Y
            ) / 0x8000;
        }

        public int CalculateTotalFrames()
        {
            //init arc lengths
            //the time parameter for a bezier does not vary linearly with the distance travelled.
            //we would like a way to find a point in the bezier by distance travelled.
            //by sampling a few points in the bezier and getting their distance from last,
            //we can make a lookup table for distance travelled from input t
            //...then use that as a reverse lookup to find a t for distance travelled.

            ArcLengths = new float[SUBDIV_COUNT];
            var subdiv1 = (float)(SUBDIV_COUNT);
            var ptLast = GetInterp(0);
            var runningTotal = 0f;
            for (int i=0; i<SUBDIV_COUNT; i++)
            {
                var ptCurrent = GetInterp((i + 1) / subdiv1);
                var dist = (ptCurrent - ptLast).Length();
                runningTotal += dist;
                ArcLengths[i] = runningTotal;
                ptLast = ptCurrent;
            }

            //righto, now that that's initialized, actually calculate how many frames this motion will take to complete.
            TotalFrames = (int)(runningTotal * STANDARD_VELOCITY);
            return Math.Max(1, TotalFrames);
        }

        public void UpdateTotalFrames(int frames)
        {
            TotalFrames = Math.Max(1, frames);
        }

        private Vector2 InternalGetNext(int frame)
        {
            var pt = (frame * ArcLengths[SUBDIV_COUNT-1]) / TotalFrames;

            var arc = ArcLengths[LastArcLengthInd];
            while (pt > arc && LastArcLengthInd < SUBDIV_COUNT - 1)
            {
                arc = ArcLengths[++LastArcLengthInd];
            }

            float a = (LastArcLengthInd == 0) ? 0 : ArcLengths[LastArcLengthInd - 1];
            float b = arc;
            var f = (pt - a) / (b - a);
            var t = (LastArcLengthInd + f) / (SUBDIV_COUNT);

            var result = GetInterp(t);
            return result;
        }

        public Tuple<LotTilePos, Vector2, Vector2> NextPointAndVel(int frame)
        {
            //if (Next == null) Next = InternalGetNext(frame);
            var current = InternalGetNext(frame);
            Vector2 velocity;
            if (frame >= TotalFrames)
            {
                if (Last != null) velocity = Last.Value;
                else velocity = ((D-A).ToVector2()/(0x8000*16))/TotalFrames;
            }
            else
            {
                var next = InternalGetNext(frame + 1);
                velocity = (next - current) / 16;
            }
            Last = velocity;

            return new Tuple<LotTilePos, Vector2, Vector2>(LotTilePos.FromVec2(current), current / 16, velocity);
        }

        public void ResetToFrame(int frame)
        {
            LastArcLengthInd = 0;
            Last = null;
        }

        // STATIC

        private static void GenerateBezierControl(VMIPathSegment fromSegI, VMIPathSegment toSegI,
            VMWalkableRect from, VMWalkableRect to)
        {
            //find average direction line

            if (fromSegI is VMPathBezierSegment)
            {
                var fromSeg = (VMPathBezierSegment)fromSegI;
                if (toSegI is VMPathBezierSegment)
                {
                    var toSeg = (VMPathBezierSegment)toSegI;
                    var fromDiff = (fromSeg.D - fromSeg.A).ToVector2();
                    var toDiff = (toSeg.D - toSeg.A).ToVector2();

                    //essentially normalizing, but keeping the lengths stored
                    var fromLength = fromDiff.Length();
                    var toLength = toDiff.Length();
                    fromDiff /= fromLength;
                    toDiff /= toLength;

                    //our control points should create a line with average direction between its bordering lines.
                    var avg = fromDiff + toDiff;
                    avg.Normalize();

                    var controlStrength = Math.Min(fromLength, toLength) / 2;
                    toSeg.B = toSeg.A + (avg * toLength / 3).ToPoint();
                    fromSeg.C = fromSeg.D - (avg * fromLength / 3).ToPoint();

                    int changed = 2;
                    bool testTo = true;

                    int emergency = 0;
                    while (emergency++ < 1000 && changed-- > 0)
                    {
                        if (testTo)
                        {
                            //make sure to is within rect bounds
                            var n = to.ClosestHiP(toSeg.B.X, toSeg.B.Y);
                            if (n != toSeg.B)
                            {
                                //if we changed, we'll need to check the other side again.
                                changed = 1;

                                //multiply the other side by the change factors here
                                var diff = (toSeg.B - toSeg.A);
                                var diff2 = (n - toSeg.A);

                                var otherDiff = (fromSeg.C - fromSeg.D);
                                if (diff.X != 0)
                                    otherDiff.X = (int)(((long)otherDiff.X * diff2.X) / diff.X);
                                if (diff.Y != 0)
                                    otherDiff.Y = (int)(((long)otherDiff.Y * diff2.Y) / diff.Y);

                                toSeg.B = n;

                                fromSeg.C = fromSeg.D + otherDiff;
                            }
                        }
                        else
                        {
                            //make sure from is within rect bounds
                            var n = from.ClosestHiP(fromSeg.C.X, fromSeg.C.Y);
                            if (n != fromSeg.C)
                            {
                                //if we changed, we'll need to check the other side again.
                                changed = 1;

                                //multiply the other side by the change factors here
                                var diff = (fromSeg.C - fromSeg.D);
                                var diff2 = (n - fromSeg.D);

                                var otherDiff = (toSeg.B - toSeg.A);
                                if (diff.X != 0)
                                    otherDiff.X = (int)(((long)otherDiff.X * diff2.X) / diff.X);
                                if (diff.Y != 0)
                                    otherDiff.Y = (int)(((long)otherDiff.Y * diff2.Y) / diff.Y);

                                fromSeg.C = n;
                                toSeg.B = toSeg.A + otherDiff;
                            }
                        }
                        testTo = !testTo;
                    }
                } else
                {
                    var fromDiff = (fromSeg.D - fromSeg.A).ToVector2();
                    var toDiff = (toSegI.Destination - toSegI.Source).ToVector2();

                    //essentially normalizing, but keeping the lengths stored
                    var fromLength = fromDiff.Length();
                    var toLength = toDiff.Length();
                    fromDiff /= fromLength;
                    toDiff /= toLength;

                    //our control points should create a line with average direction between its bordering lines.
                    fromSeg.C = fromSeg.D - (toDiff * fromLength / 3).ToPoint();
                    fromSeg.C = from.ClosestHiP(fromSeg.C.X, fromSeg.C.Y);
                }
            } else if (toSegI is VMPathBezierSegment)
            {
                var toSeg = (VMPathBezierSegment)toSegI;
                var fromDiff = (fromSegI.Destination - fromSegI.Source).ToVector2();
                var toDiff = (toSeg.D - toSeg.A).ToVector2();

                //essentially normalizing, but keeping the lengths stored
                var fromLength = fromDiff.Length();
                var toLength = toDiff.Length();
                fromDiff /= fromLength;
                toDiff /= toLength;

                //our control points should create a line with average direction between its bordering lines.
                toSeg.B = toSeg.A + (fromDiff * toLength / 3).ToPoint();
                toSeg.B = to.ClosestHiP(toSeg.B.X, toSeg.B.Y);
            }
        }

        public static LinkedList<VMIPathSegment> GeneratePath(LinkedList<VMWalkableRect> rects, Point dest, float? dirIn, float? dirOut)
        {
            if (rects == null) return null;
            var result = new LinkedList<VMIPathSegment>();
            VMWalkableRect last = null;
            VMWalkableRect last2 = null;
            VMIPathSegment lastSeg = null;
            dest = new Point(dest.X * 0x8000, dest.Y * 0x8000);

            int lineProcess = -1;
            VMWalkableRect lineStart = null;
            VMWalkableRect preLineStart = null;

            foreach (var rect in rects)
            {
                if (last != null)
                {
                    if (lineProcess != -1 || last.LineID != -1)
                    {
                        if (lineProcess == -1)
                        {
                            //
                            lineStart = last;
                            preLineStart = last2;
                            lineProcess = last.LineID;
                        }
                        if (rect.LineID != lineProcess)
                        {
                            var seg2 = new VMPathLineSegment(lineStart.ParentSourceHiP, rect.ParentSourceHiP);
                            //var seg2 = new VMPathBezierSegment() { A = lineStart.ParentSourceHiP, D = rect.ParentSourceHiP };
                            //seg2.B = seg2.A;
                            //seg2.C = seg2.D;
                            GenerateBezierControl(lastSeg, seg2, preLineStart ?? lineStart, lineStart);
                            result.AddLast(seg2);
                            lastSeg = seg2;
                            lineProcess = -1;
                        }
                        last2 = last;
                        last = rect;
                        continue;
                    }
                    if (rect.ParentSourceHiP == last.ParentSourceHiP)
                    {
                        last = rect;
                        continue; //there's not any value in this line, just skip it
                    }
                    //new line from 
                    var seg = new VMPathBezierSegment() { A = last.ParentSourceHiP, D = rect.ParentSourceHiP };
                    if (lastSeg == null)
                    {
                        //starting. 
                        if (dirIn != null)
                        {
                            //we've been instructed to set up a basic control point for entry.
                            //right now rather weak
                            var dirVec = new Vector2((float)Math.Sin(dirIn.Value), (float)-Math.Cos(dirIn.Value)) * (6*0x8000);
                            var dirPt = seg.A + dirVec.ToPoint();
                            seg.B = last.ClosestHiP(dirPt.X, dirPt.Y);
                        }
                        else
                        {
                            //initialize control point as empty
                            seg.B = seg.A;
                        }
                    } else
                    {
                        //initialize our first control point and the last segment's second
                        //based on the angle between each line, within rectangle limits.
                        //these should be symmetrical
                        GenerateBezierControl(lastSeg, seg, last2, last);
                    }
                    //line between the two ParentSource points
                    result.AddLast(seg);
                    lastSeg = seg;
                }
                last2 = last;
                last = rect;
            }

            //finally, a line to the destination point.
            if (last != null)
            {
                var seg = new VMPathBezierSegment() { A = last.ParentSourceHiP, C = dest, D = dest };
                if (dirOut != null)
                {
                    var dirVec = new Vector2((float)Math.Sin(dirOut.Value), (float)-Math.Cos(dirOut.Value)) * (16*0x8000);
                    var dirPt = seg.D - dirVec.ToPoint();
                    seg.C = last.ClosestHiP(dirPt.X, dirPt.Y);
                }

                if (last2 == null)
                {
                    //starting. 
                    if (dirIn != null)
                    {
                        //we've been instructed to set up a basic control point for entry.
                        //right now rather weak
                        var dirVec = new Vector2((float)Math.Sin(dirIn.Value), (float)-Math.Cos(dirIn.Value)) * (6 * 0x8000);
                        var dirPt = seg.A + dirVec.ToPoint();
                        seg.B = last.ClosestHiP(dirPt.X, dirPt.Y);
                    }
                    else
                    {
                        //initialize control point as empty
                        seg.B = seg.A;
                    }
                }
                else
                {
                    //initialize our first control point and the last segment's second
                    //based on the angle between each line, within rectangle limits.
                    //these should be symmetrical
                    GenerateBezierControl(lastSeg, seg, last2, last);
                }
                //line between the two ParentSource points
                result.AddLast(seg);
            }
            return result;
        }

        public void AddToPath(List<Vector2> list, bool start)
        {
            for (int i=(start)?0:1; i<=30; i++)
            {
                list.Add(GetInterp(i / 30f)/16f);
            }
        }

        public void AddDebugExtras(DebugLinesComponent comp)
        {
            comp.AddLine(A.ToVector2() / (0x8000 * 16f), B.ToVector2() / (0x8000 * 16f), Color.DeepSkyBlue);
            comp.AddLine(C.ToVector2() / (0x8000 * 16f), D.ToVector2() / (0x8000 * 16f), Color.DodgerBlue);
        }
    }
}
