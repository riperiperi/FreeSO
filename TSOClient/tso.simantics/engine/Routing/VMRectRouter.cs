/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using FSO.SimAntics.Model.Routing;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.SimAntics.Engine.Routing
{
    public class VMRectRouter
    {
        private VMObstacleSet TreeMap;

        public VMRectRouter(VMObstacleSet map)
        {
            TreeMap = map;
        }

        public LinkedList<VMWalkableRect> Route(Point from, Point to, int startCardinal)
        {
            var openSet = new List<VMWalkableRect>();

            var startRect = new VMWalkableRect(from.X, from.Y, from.X, from.Y);
            ConstructFirstFree(startRect);
            startRect.Start = true;
            ExtendFrom(startRect, startCardinal);
            startRect = startRect.Adj.FirstOrDefault() ?? startRect;
            startRect.Adj.Clear();
            startCardinal = 0;

            startRect.Start = true;
            startRect.ParentSource = from;
            startRect.ParentSourceHiP = new Point(from.X * 0x8000, from.Y * 0x8000);
            startRect.OriginalG = 0;

            openSet.Add(startRect);

            while (openSet.Count > 0)
            {
                var current = openSet[0];
                openSet.RemoveAt(0);

                if (current.Contains(to))
                {
                    var result = new LinkedList<VMWalkableRect>();
                    if (!to.Equals(current.ParentSource)) result.AddFirst(current);
                    Point last = current.ParentSource;
                    while (current != startRect)
                    {
                        current = current.Parent;
                        if (!last.Equals(current.ParentSource)) result.AddFirst(current);
                        last = current.ParentSource;
                    }
                    return result;
                }

                current.State = 2; //this rectangle is now closed

                //generate all adj
                ExtendFrom(current, (startCardinal++));
                ExtendFrom(current, (startCardinal++) % 4);
                ExtendFrom(current, (startCardinal++) % 4);
                ExtendFrom(current, (startCardinal++) % 4);

                startCardinal = 0;

                foreach (VMWalkableRect r in current.Adj)
                {
                    if (r.State == 2) continue; //closed
                    bool newcomer = (r.State == 0);

                    var parentPt = RectIntersect(r, current, current.ParentSource);
                    var originalG = current.OriginalG + PointDist(current.ParentSource, parentPt);
                    var closest = r.Closest(to.X, to.Y);
                    var newGScore = originalG + PointDist(parentPt, closest);

                    if (newcomer || newGScore < r.GScore)
                    {
                        r.State = 1;
                        r.ParentSource = parentPt;
                        r.ParentSourceHiP = new Point(parentPt.X*0x8000, parentPt.Y*0x8000);
                        r.Parent = current;
                        r.OriginalG = originalG;
                        r.GScore = newGScore;
                        r.FScore = newGScore + PointDist(closest, to);

                        if (newcomer)
                        {
                            OpenSetSortedInsert(openSet, r);
                        }
                        else
                        {
                            openSet.Remove(r);
                            OpenSetSortedInsert(openSet, r);
                        }
                    }
                }
            }
            return null; //failed
        }

        public void OptimizeLines(LinkedList<VMWalkableRect> route, Point to, float? dirIn) {
            if ((route?.Count ?? 0) == 0) return;
            var node = route.First;
            to = new Point(to.X * 0x8000, to.Y * 0x8000);

            //remove dupes
            node = node?.Next;
            while (node?.Next != null)
            {
                var next = node.Next;
                if (node.Value.ParentSource == node.Next.Value.ParentSource) route.Remove(node);
                node = next;
            }
            node = route.First;

            if (dirIn != null && node.Next != null)
            {
                var node2 = node;
                var next = node.Next;
                var dirVec = new Vector2((float)Math.Sin(dirIn.Value), (float)-Math.Cos(dirIn.Value));
                var previousDist = Math.Max(0, Vector2.Dot((next.Value.ParentSource - node.Value.ParentSource).ToVector2(), dirVec));
                var dist = 8 - previousDist;
                while (next != route.Last && dist > 0)
                {
                    var newPt = next.Value.ClosestOnSharedEdge(node2.Value, next.Value.ParentSource + (dirVec * dist).ToPoint());
                    var diff = newPt - next.Value.ParentSource;
                    dist = (float)Math.Sqrt(diff.X * diff.X + diff.Y * diff.Y);
                    next.Value.ParentSource = newPt;
                    next.Value.ParentSourceHiP = new Point(newPt.X * 0x8000, newPt.Y * 0x8000);
                    node2 = next;
                    next = node2.Next;
                }
                node = node.Next;
            }

            int lineID = 0;
            while (node?.Next != null)
            {
                //from the point starting at node...
                //and points after the next point
                //see if we can cut the corner (WITHOUT leaving free rects)

                var start = node.Value.ParentSourceHiP;

                var success = false;
                var beforeDest = node.Next;
                var destPoint = beforeDest.Next;
                var setNext = false;
                while (true)
                {
                    //verify that the line between start and end is within the relevant free rects

                    //raycast through the free rects in order.
                    //remember the point we left the rectangle so we can test if that point is within the next
                    //this is our main tell to see if the line left the rectangle.
                    
                    var goodLine = true;
                    var lastLeftPt = start;
                    if (beforeDest != null)
                    {
                        var rect = node;
                        var end = (destPoint == null)?to:destPoint.Value.ParentSourceHiP;
                        while (rect != destPoint)
                        {
                            if (!rect.Value.ContainsHiP(lastLeftPt))
                            {
                                //the line didn't leave the last rectangle into this one,
                                //it left it into an empty space.
                                goodLine = false;
                                break;
                            }
                            var lineTest = rect.Value.RaycastLineHiP(start, end);
                            if (lineTest == null)
                            {
                                //we didn't hit this rectangle at all... 
                                goodLine = false;
                                break;
                            }
                            lastLeftPt = lineTest.Value;
                            rect = rect.Next;
                        }
                    }
                    else goodLine = false;
                    
                    if (goodLine && destPoint != null)
                    {
                        goodLine = destPoint.Value.ContainsHiP(lastLeftPt);
                    }

                    if (goodLine)
                    {
                        //yes? keep going
                        success = true;
                        beforeDest = destPoint;
                        destPoint = destPoint?.Next;
                    }
                    else
                    {
                        if (!success) break;
                        //no? if we succeeded, take our last result.
                        //update entry points for each of the 
                        var rect = node;
                        var end = (beforeDest == null)?to:beforeDest.Value.ParentSourceHiP;
                        lastLeftPt = start;
                        while (rect != beforeDest)
                        {
                            //don't change node's entry.
                            if (rect != node)
                            {
                                if (rect.Next != beforeDest) rect.Value.LineID = lineID;
                                rect.Value.ParentSourceHiP = lastLeftPt;
                                rect.Value.ParentSource = new Point(lastLeftPt.X / 0x8000, lastLeftPt.Y / 0x8000);
                            }
                            
                            lastLeftPt = rect.Value.RaycastLineHiP(start, end).Value;
                            rect = rect.Next;
                        }
                        setNext = true;
                        node = beforeDest; //new start is where we ended
                        lineID++;
                        break;
                    }
                }
                if (!setNext) node = node.Next;
            }
        }

        private int PointDist(Point pt1, Point pt2)
        {
            Point diff = pt1 - pt2;
            return (int)Math.Sqrt(diff.X * diff.X + diff.Y * diff.Y);
        }

        private void OpenSetSortedInsert(List<VMWalkableRect> set, VMWalkableRect item)
        {
            for (var i = 0; i < set.Count; i++)
            {
                if (set[i].FScore > item.FScore)
                {
                    set.Insert(i, item);
                    return;
                }
            }
            set.Add(item);
        }


        private Point RectIntersect(VMObstacle r1, VMObstacle r2, Point destPoint)
        {
            bool vert = true;
            int d1, d2, p=0;
            if (r1.x1 == r2.x2) { vert = false; p = r1.x1; }
            if (r1.x2 == r2.x1) { vert = false; p = r1.x2; }

            if (r1.y1 == r2.y2) { vert = true; p = r1.y1; }
            if (r1.y2 == r2.y1) { vert = true; p = r1.y2; }

            if (vert)
            {
                d1 = Math.Max(r1.x1, r2.x1); d2 = Math.Min(r1.x2, r2.x2);
                return new Point(Math.Max(d1, Math.Min(d2, destPoint.X)), p);
            }
            else
            {
                d1 = Math.Max(r1.y1, r2.y1); d2 = Math.Min(r1.y2, r2.y2);
                return new Point(p, Math.Max(d1, Math.Min(d2, destPoint.Y)));
            }
        }

        private void ExtendFrom(VMWalkableRect source, int dir)
        {
            var free = source.Free[dir].List;

            foreach (VMFreeListRegion line in free)
            {
                VMExtendRectResult extension = new VMExtendRectResult();
                VMWalkableRect newRect = null;
                switch (dir)
                {
                    case 0:
                        extension = ExtendRect(dir, line.a, line.b, source.y1);
                        newRect = (extension.BestN == source.y1) ? null : new VMWalkableRect(line.a, extension.BestN, line.b, source.y1);
                        break;
                    case 1:
                        extension = ExtendRect(dir, line.a, line.b, source.x2);
                        newRect = (extension.BestN == source.x2) ? null : new VMWalkableRect(source.x2, line.a, extension.BestN, line.b);
                        break;
                    case 2:
                        extension = ExtendRect(dir, line.a, line.b, source.y2);
                        newRect = (extension.BestN == source.y2) ? null : new VMWalkableRect(line.a, source.y2, line.b, extension.BestN);
                        break;
                    case 3:
                        extension = ExtendRect(dir, line.a, line.b, source.x1);
                        newRect = (extension.BestN == source.x1) ? null : new VMWalkableRect(extension.BestN, line.a, source.x1, line.b);
                        break;
                }

                if (newRect == null || extension.BestN == int.MaxValue || extension.BestN == int.MinValue) continue;
                source.Adj.Add(newRect);
                newRect.Adj.Add(source);

                var bounds = ((dir % 2) == 0) ? new VMFreeListRegion(newRect.x1, newRect.x2) : new VMFreeListRegion(newRect.y1, newRect.y2);
                var free2 = new VMFreeList(bounds);
                foreach (var r in extension.Best)
                {
                    free2.Subtract(new VMFreeListRegion(r.a, r.b));
                    if (r.rect is VMWalkableRect)
                    {
                        var w = (VMWalkableRect)r.rect;
                        w.Free[(dir + 2) % 4].Subtract(bounds);
                        w.Adj.Add(newRect);
                        newRect.Adj.Add(w);
                    }
                }

                newRect.Free[dir] = free2;
                newRect.Free[(dir + 2) % 4] = new VMFreeList(0, 0);

                ConstructFree(newRect, ((dir % 2) == 1), ((dir % 2) == 0), ((dir % 2) == 1), ((dir % 2) == 0));
                if (!source.Start)
                {
                    TreeMap.Add(newRect);
                }
            }
		}

        private VMExtendRectResult ExtendRect(int dir, int d1, int d2, int p)
        {
            int bestN = ((dir + 1) % 4 < 2) ? int.MinValue : int.MaxValue;
            var best = new List<VMExtendRegion>();
            VMObstacle extendRange;

            switch (dir)
            {
                case 0: //top
                    extendRange = new VMObstacle(d1, int.MinValue, d2, p);
                    foreach (VMObstacle r in IntersectSet(extendRange))
                    {
                        if (r.y2 > p) continue; //bottom of rect lower than start point = no hit
                        if (r.x1 >= d2 || r.x2 <= d1) continue; //does not intersect
                        if (r.y2 > bestN)
                        {
                            bestN = r.y2;
                            best.Clear();
                            best.Add(new VMExtendRegion(r.x1, r.x2, r));
                        }
                        else if (r.y2 == bestN)
                        {
                            best.Add(new VMExtendRegion(r.x1, r.x2, r));
                        }
                    }
                    break;
                case 1: //right
                    extendRange = new VMObstacle(p, d1, int.MaxValue, d2);
                    foreach (VMObstacle r in IntersectSet(extendRange))
                    {
                        if (r.x1 < p) continue; //left of rect lefter than start point = no hit
                        if (r.y1 >= d2 || r.y2 <= d1) continue; //does not intersect
                        if (r.x1 < bestN)
                        {
                            bestN = r.x1;
                            best.Clear();
                            best.Add(new VMExtendRegion(r.y1, r.y2, r));
                        }
                        else if (r.x1 == bestN)
                        {
                            best.Add(new VMExtendRegion(r.y1, r.y2, r));
                        }
                    }
                    break;
                case 2: //bottom
                    extendRange = new VMObstacle(d1, p, d2, int.MaxValue);
                    foreach (VMObstacle r in IntersectSet(extendRange))
                    {
                        if (r.y1 < p) continue; //top of rect higher than start point = no hit
                        if (r.x1 >= d2 || r.x2 <= d1) continue; //does not intersect
                        if (r.y1 < bestN)
                        {
                            bestN = r.y1;
                            best.Clear();
                            best.Add(new VMExtendRegion(r.x1, r.x2, r));
                        }
                        else if (r.y1 == bestN)
                        {
                            best.Add(new VMExtendRegion(r.x1, r.x2, r));
                        }
                    }
                    break;
                case 3: //left
                    extendRange = new VMObstacle(int.MinValue, d1, p, d2);
                    foreach (VMObstacle r in IntersectSet(extendRange))
                    {
                        if (r.x2 > p) continue; //right of rect righter than start point = no hit
                        if (r.y1 >= d2 || r.y2 <= d1) continue; //does not intersect
                        if (r.x2 > bestN)
                        {
                            bestN = r.x2;
                            best.Clear();
                            best.Add(new VMExtendRegion(r.y1, r.y2, r));
                        }
                        else if (r.x2 == bestN)
                        {
                            best.Add(new VMExtendRegion(r.y1, r.y2, r));
                        }
                    }
                    break;
            }
		    return new VMExtendRectResult { Best = best, BestN = bestN };
	    }

        private void ConstructFree(VMWalkableRect rect, bool d1, bool d2, bool d3, bool d4)
        {
            if (d1) rect.Free[0] = new VMFreeList(rect.x1, rect.x2);
            if (d2) rect.Free[1] = new VMFreeList(rect.y1, rect.y2);
            if (d3) rect.Free[2] = new VMFreeList(rect.x1, rect.x2);
            if (d4) rect.Free[3] = new VMFreeList(rect.y1, rect.y2);

            foreach (VMObstacle r in EdgeSet(rect))
            {
                if (r == rect) continue;
                if (d1 && r.y2 == rect.y1 && !(r.x2 <= rect.x1 || r.x1 >= rect.x2))
                {
                    rect.Free[0].Subtract(new VMFreeListRegion(r.x1, r.x2));
				    if (r is VMWalkableRect)
                    {
                        var w = (VMWalkableRect)r;
                        w.Free[2].Subtract(new VMFreeListRegion(rect.x1, rect.x2));
					    rect.Adj.Add(w);
                        w.Adj.Add(rect);
                    }
                }

			    if (d2 && r.x1 == rect.x2 && !(r.y2 <= rect.y1 || r.y1 >= rect.y2)) {
                    rect.Free[1].Subtract(new VMFreeListRegion(r.y1, r.y2));
                    if (r is VMWalkableRect)
                    {
                        var w = (VMWalkableRect)r;
                        w.Free[3].Subtract(new VMFreeListRegion(rect.y1, rect.y2));
                        rect.Adj.Add(w);
					    w.Adj.Add(rect);
				    }
                }

			    if (d3 && r.y1 == rect.y2 && !(r.x2 <= rect.x1 || r.x1 >= rect.x2)) {
                    rect.Free[2].Subtract(new VMFreeListRegion(r.x1, r.x2));
                    if (r is VMWalkableRect)
                    {
                        var w = (VMWalkableRect)r;
                        w.Free[0].Subtract(new VMFreeListRegion(rect.x1, rect.x2));
                        rect.Adj.Add(w);
					    w.Adj.Add(rect);
				    }
			    }

			    if (d4 && r.x2 == rect.x1 && !(r.y2 <= rect.y1 || r.y1 >= rect.y2)) {
                    rect.Free[3].Subtract(new VMFreeListRegion(r.y1, r.y2));
                    if (r is VMWalkableRect)
                    {
                        var w = (VMWalkableRect)r;
                        w.Free[1].Subtract(new VMFreeListRegion(rect.y1, rect.y2));
                        rect.Adj.Add(w);
					    w.Adj.Add(rect);
				    }
			    }
            }
	    }

        private List<VMObstacle> IntersectSet(VMObstacle search)
        {
            return TreeMap.AllIntersect(search);
        }

        private List<VMObstacle> EdgeSet(VMObstacle search)
        {
            return TreeMap.OnEdge(search);
        }

        private void ConstructFirstFree(VMWalkableRect rect)
        {
            rect.Free[0] = new VMFreeList(rect.x1);
            rect.Free[1] = new VMFreeList(rect.y1);
            rect.Free[2] = new VMFreeList(rect.x1);
            rect.Free[3] = new VMFreeList(rect.y1);

            foreach (VMObstacle r in EdgeSet(rect))
            {
                if (r == rect) continue;
                if (r.y2 == rect.y1 && !(r.x2 <= rect.x1 || r.x1 >= rect.x2))
                    rect.Free[0].Subtract(new VMFreeListRegion(r.x1, r.x2));

                if (r.x1 == rect.x2 && !(r.y2 <= rect.y1 || r.y1 >= rect.y2))
                    rect.Free[1].Subtract(new VMFreeListRegion(r.y1, r.y2));

                if (r.y1 == rect.y2 && !(r.x2 <= rect.x1 || r.x1 >= rect.x2))
                    rect.Free[2].Subtract(new VMFreeListRegion(r.x1, r.x2));

                if (r.x2 == rect.x1 && !(r.y2 <= rect.y1 || r.y1 >= rect.y2))
                    rect.Free[3].Subtract(new VMFreeListRegion(r.y1, r.y2));
            }
        }
    }

    public struct VMExtendRectResult
    {
        public List<VMExtendRegion> Best;
        public int BestN;
    }

    public struct VMExtendRegion
    {
        public int a;
        public int b;
        public VMObstacle rect;
        public VMExtendRegion(int a, int b, VMObstacle rect)
        {
            this.a = a;
            this.b = b;
            this.rect = rect;
        }
    }
}
