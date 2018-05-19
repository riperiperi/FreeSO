using System.Collections.Generic;
using System.Diagnostics;
using VoronoiLib.Structures;

namespace VoronoiLib
{
    public static class FortunesAlgorithm
    {
        public static LinkedList<VEdge> Run(List<FortuneSite> sites, double minX, double minY, double maxX, double maxY)
        {
            var eventQueue = new MinHeap<FortuneEvent>(5*sites.Count);
            foreach (var s in sites)
            {
                eventQueue.Insert(new FortuneSiteEvent(s));
            }
            //init tree
            var beachLine = new BeachLine();
            var edges = new LinkedList<VEdge>();
            var deleted = new HashSet<FortuneCircleEvent>();

            //init edge list
            while (eventQueue.Count != 0)
            {
                var fEvent = eventQueue.Pop();
                if (fEvent is FortuneSiteEvent)
                    beachLine.AddBeachSection((FortuneSiteEvent) fEvent, eventQueue, deleted, edges);
                else
                {
                    if (deleted.Contains((FortuneCircleEvent) fEvent))
                    {
                        deleted.Remove((FortuneCircleEvent) fEvent);
                    }
                    else
                    {
                        beachLine.RemoveBeachSection((FortuneCircleEvent) fEvent, eventQueue, deleted, edges);
                    }
                }
            }
            

            //clip edges
            var edgeNode = edges.First;
            while (edgeNode != null)
            {
                var edge = edgeNode.Value;
                var next = edgeNode.Next;

                var valid = ClipEdge(edge, minX, minY, maxX, maxY);
                if (!valid)
                    edges.Remove(edgeNode);
                //advance
                edgeNode = next;
            }
            return edges;
        }

        //combination of personal ray clipping alg and cohen sutherland
        private static bool ClipEdge(VEdge edge, double minX, double minY, double maxX, double maxY)
        {
            var accept = false;

            //if its a ray
            if (edge.End == null)
            {
                accept = ClipRay(edge, minX, minY, maxX, maxY);
            }
            else
            {
                //Cohen–Sutherland
                var start = ComputeOutCode(edge.Start.X, edge.Start.Y, minX, minY, maxX, maxY);
                var end = ComputeOutCode(edge.End.X, edge.End.Y, minX, minY, maxX, maxY);

                while (true)
                {
                    if ((start | end) == 0)
                    {
                        accept = true;
                        break;
                    }
                    if ((start & end) != 0)
                    {
                        break;
                    }

                    double x = -1, y = -1;
                    var outcode = start != 0 ? start : end;

                    if ((outcode & 0x8) != 0) // top
                    {
                        x = edge.Start.X + (edge.End.X - edge.Start.X)*(maxY - edge.Start.Y)/(edge.End.Y - edge.Start.Y);
                        y = maxY;
                    }
                    else if ((outcode & 0x4) != 0) // bottom
                    {
                        x = edge.Start.X + (edge.End.X - edge.Start.X)*(minY - edge.Start.Y)/(edge.End.Y - edge.Start.Y);
                        y = minY;
                    }
                    else if ((outcode & 0x2) != 0) //right
                    {
                        y = edge.Start.Y + (edge.End.Y - edge.Start.Y)*(maxX - edge.Start.X)/(edge.End.X - edge.Start.X);
                        x = maxX;
                    }
                    else if ((outcode & 0x1) != 0) //left
                    {
                        y = edge.Start.Y + (edge.End.Y - edge.Start.Y)*(minX - edge.Start.X)/(edge.End.X - edge.Start.X);
                        x = minX;
                    }

                    if (outcode == start)
                    {
                        edge.Start = new VPoint(x, y);
                        start = ComputeOutCode(x, y, minX, minY, maxX, maxY);
                    }
                    else
                    {
                        edge.End = new VPoint(x, y);
                        end = ComputeOutCode(x, y, minX, minY, maxX, maxY);
                    }
                }
            }
            //if we have a neighbor
            if (edge.Neighbor != null)
            {
                //check it
                var valid = ClipEdge(edge.Neighbor, minX, minY, maxX, maxY);
                //both are valid
                if (accept && valid)
                {
                    edge.Start = edge.Neighbor.End;
                }
                //this edge isn't valid, but the neighbor is
                //flip and set
                if (!accept && valid)
                {
                    edge.Start = edge.Neighbor.End;
                    edge.End = edge.Neighbor.Start;
                    accept = true;
                }
            }
            return accept;
        }
        
        private static int ComputeOutCode(double x, double y, double minX, double minY, double maxX, double maxY)
        {
            int code = 0;
            if (x.ApproxEqual(minX) || x.ApproxEqual(maxX))
            { }
            else if (x < minX)
                code |= 0x1;
            else if (x > maxX)
                code |= 0x2;

            if (y.ApproxEqual(minY) || x.ApproxEqual(maxY))
            { }
            else if (y < minY)
                code |= 0x4;
            else if (y > maxY)
                code |= 0x8;
            return code;
        }

        private static bool ClipRay(VEdge edge, double minX, double minY, double maxX, double maxY)
        {
            var start = edge.Start;
            //horizontal ray
            if (edge.SlopeRise.ApproxEqual(0))
            {
                if (!Within(start.Y, minY, maxY))
                    return false;
                if (edge.SlopeRun > 0 && start.X > maxX)
                    return false;
                if (edge.SlopeRun < 0 && start.X < minX)
                    return false;
                if (Within(start.X, minX, maxX))
                {
                    if (edge.SlopeRun > 0)
                        edge.End = new VPoint(maxX, start.Y);
                    else
                        edge.End = new VPoint(minX, start.Y);
                }
                else
                {
                    if (edge.SlopeRun > 0)
                    {
                        edge.Start = new VPoint(minX, start.Y);
                        edge.End = new VPoint(maxX, start.Y);
                    }
                    else
                    {
                        edge.Start = new VPoint(maxX, start.Y);
                        edge.End = new VPoint(minX, start.Y);
                    }
                }
                return true;
            }
            //vertical ray
            if (edge.SlopeRun.ApproxEqual(0))
            {
                if (start.X < minX || start.X > maxX)
                    return false;
                if (edge.SlopeRise > 0 && start.Y > maxY)
                    return false;
                if (edge.SlopeRise < 0 && start.Y < minY)
                    return false;
                if (Within(start.Y, minY, maxY))
                {
                    if (edge.SlopeRise > 0)
                        edge.End = new VPoint(start.X, maxY);
                    else
                        edge.End = new VPoint(start.X, minY);
                }
                else
                {
                    if (edge.SlopeRise > 0)
                    {
                        edge.Start = new VPoint(start.X, minY);
                        edge.End = new VPoint(start.X, maxY);
                    }
                    else
                    {
                        edge.Start = new VPoint(start.X, maxY);
                        edge.End = new VPoint(start.X, minY);
                    }
                }
                return true;
            }
            
            //works for outside
            Debug.Assert(edge.Slope != null, "edge.Slope != null");
            Debug.Assert(edge.Intercept != null, "edge.Intercept != null");
            var topX = new VPoint(CalcX(edge.Slope.Value, maxY, edge.Intercept.Value), maxY);
            var bottomX = new VPoint(CalcX(edge.Slope.Value, minY, edge.Intercept.Value), minY);
            var leftY = new VPoint(minX, CalcY(edge.Slope.Value, minX, edge.Intercept.Value));
            var rightY = new VPoint(maxX, CalcY(edge.Slope.Value, maxX, edge.Intercept.Value));

            //reject intersections not within bounds
            var candidates = new List<VPoint>();
            if (Within(topX.X, minX, maxX))
                candidates.Add(topX);
            if (Within(bottomX.X, minX, maxX))
                candidates.Add(bottomX);
            if (Within(leftY.Y, minY, maxY))
                candidates.Add(leftY);
            if (Within(rightY.Y, minY, maxY))
                candidates.Add(rightY);

            //reject candidates which don't align with the slope
            for (var i = candidates.Count - 1; i > -1; i--)
            {
                var candidate = candidates[i];
                //grab vector representing the edge
                var ax = candidate.X - start.X;
                var ay = candidate.Y - start.Y;
                if (edge.SlopeRun*ax + edge.SlopeRise*ay < 0)
                    candidates.RemoveAt(i);
            }

            //if there are two candidates we are outside the closer one is start
            //the further one is the end
            if (candidates.Count == 2)
            {
                var ax = candidates[0].X - start.X;
                var ay = candidates[0].Y - start.Y;
                var bx = candidates[1].X - start.X;
                var by = candidates[1].Y - start.Y;
                if (ax*ax + ay*ay > bx*bx + by*by)
                {
                    edge.Start = candidates[1];
                    edge.End = candidates[0];
                }
                else
                {
                    edge.Start = candidates[0];
                    edge.End = candidates[1];
                }
            }

            //if there is one candidate we are inside
            if (candidates.Count == 1)
                edge.End = candidates[0];

            //there were no candidates
            return edge.End != null;
        }

        private static bool Within(double x, double a, double b)
        {
            return x.ApproxGreaterThanOrEqualTo(a) && x.ApproxLessThanOrEqualTo(b);
        }

        private static double CalcY(double m, double x, double b)
        {
            return m * x + b;
        }

        private static double CalcX(double m, double y, double b)
        {
            return (y - b) / m;
        }
    }
}
