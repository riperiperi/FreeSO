using System;
using System.Collections.Generic;

namespace VoronoiLib.Structures
{
    internal class BeachSection
    {
        internal FortuneSite Site { get;}
        internal VEdge Edge { get; set; }
        //NOTE: this will change
        internal FortuneCircleEvent CircleEvent { get; set; }

        internal BeachSection(FortuneSite site)
        {
            Site = site;
            CircleEvent = null;
        }
    }

    internal class BeachLine
    {
        private readonly RBTree<BeachSection> beachLine;

        internal BeachLine()
        {
            beachLine = new RBTree<BeachSection>();
        }

        internal void AddBeachSection(FortuneSiteEvent siteEvent, MinHeap<FortuneEvent> eventQueue, HashSet<FortuneCircleEvent> deleted, LinkedList<VEdge> edges)
        {
            var site = siteEvent.Site;
            var x = site.X;
            var directrix = site.Y;

            RBTreeNode<BeachSection> leftSection = null;
            RBTreeNode<BeachSection> rightSection = null;
            var node = beachLine.Root;

            //find the parabola(s) above this site
            while (node != null && leftSection == null && rightSection == null)
            {
                var distanceLeft = LeftBreakpoint(node, directrix) - x;
                if (distanceLeft > 0)
                {
                    //the new site is before the left breakpoint
                    if (node.Left == null)
                    {
                        rightSection = node;
                    }
                    else
                    {
                        node = node.Left;
                    }
                    continue;
                }

                var distanceRight = x - RightBreakpoint(node, directrix);
                if (distanceRight > 0)
                {
                    //the new site is after the right breakpoint
                    if (node.Right == null)
                    {
                        leftSection = node;
                    }
                    else
                    {
                        node = node.Right;
                    }
                    continue;
                }

                //the point lies below the left breakpoint
                if (distanceLeft.ApproxEqual(0))
                {
                    leftSection = node.Previous;
                    rightSection = node;
                    continue;
                }

                //the point lies below the right breakpoint
                if (distanceRight.ApproxEqual(0))
                {
                    leftSection = node;
                    rightSection = node.Next;
                    continue;
                }

                // distance Right < 0 and distance Left < 0
                // this section is above the new site
                leftSection = rightSection = node;
            }

            //our goal is to insert the new node between the
            //left and right sections
            var section = new BeachSection(site);

            //left section could be null, in which case this node is the first
            //in the tree
            var newSection = beachLine.InsertSuccessor(leftSection, section);

            //new beach section is the first beach section to be added
            if (leftSection == null && rightSection == null)
            {
                return;
            }

            //main case:
            //if both left section and right section point to the same valid arc
            //we need to split the arc into a left arc and a right arc with our 
            //new arc sitting in the middle
            if (leftSection != null && leftSection == rightSection)
            {
                //if the arc has a circle event, it was a false alarm.
                //remove it
                if (leftSection.Data.CircleEvent != null)
                {
                    deleted.Add(leftSection.Data.CircleEvent);
                    leftSection.Data.CircleEvent = null;
                }

                //we leave the existing arc as the left section in the tree
                //however we need to insert the right section defined by the arc
                var copy = new BeachSection(leftSection.Data.Site);
                rightSection = beachLine.InsertSuccessor(newSection, copy);

                //grab the projection of this site onto the parabola
                var y = ParabolaMath.EvalParabola(leftSection.Data.Site.X, leftSection.Data.Site.Y, directrix, x);
                var intersection = new VPoint(x, y);

                //create the two half edges corresponding to this intersection
                var leftEdge = new VEdge(intersection, site, leftSection.Data.Site);
                var rightEdge = new VEdge(intersection, leftSection.Data.Site, site);
                leftEdge.Neighbor = rightEdge;

                //put the edge in the list
                edges.AddFirst(leftEdge);

                //store the left edge on each arc section
                newSection.Data.Edge = leftEdge;
                rightSection.Data.Edge = rightEdge;
                
                //store neighbors for delaunay
                leftSection.Data.Site.Neighbors.Add(newSection.Data.Site);
                newSection.Data.Site.Neighbors.Add(leftSection.Data.Site);

                //create circle events
                CheckCircle(leftSection, eventQueue);
                CheckCircle(rightSection, eventQueue);
            }

            //site is the last beach section on the beach line
            //this can only happen if all previous sites
            //had the same y value
            else if (leftSection != null && rightSection == null)
            {
                var start = new VPoint((leftSection.Data.Site.X + site.X)/ 2, double.MinValue);
                var infEdge = new VEdge(start, leftSection.Data.Site, site);
                var newEdge = new VEdge(start, site, leftSection.Data.Site);

                newEdge.Neighbor = infEdge;
                edges.AddFirst(newEdge);

                leftSection.Data.Site.Neighbors.Add(newSection.Data.Site);
                newSection.Data.Site.Neighbors.Add(leftSection.Data.Site);

                newSection.Data.Edge = newEdge;
                
                //cant check circles since they are colinear
            }

            //site is directly above a break point
            else if (leftSection != null && leftSection != rightSection)
            {
                //remove false alarms
                if (leftSection.Data.CircleEvent != null)
                {
                    deleted.Add(leftSection.Data.CircleEvent);
                    leftSection.Data.CircleEvent = null;
                }

                if (rightSection.Data.CircleEvent != null)
                {
                    deleted.Add(rightSection.Data.CircleEvent);
                    rightSection.Data.CircleEvent = null;
                }

                //the breakpoint will dissapear if we add this site
                //which means we will create an edge
                //we treat this very similar to a circle event since
                //an edge is finishing at the center of the circle
                //created by circumscribing the left center and right
                //sites

                //bring a to the origin
                var leftSite = leftSection.Data.Site;
                var ax = leftSite.X;
                var ay = leftSite.Y;
                var bx = site.X - ax;
                var by = site.Y - ay;

                var rightSite = rightSection.Data.Site;
                var cx = rightSite.X - ax;
                var cy = rightSite.Y - ay;
                var d = bx*cy - by*cx;
                var magnitudeB = bx*bx + by*by;
                var magnitudeC = cx*cx + cy*cy;
                var vertex = new VPoint(
                    (cy*magnitudeB - by * magnitudeC)/(2*d) + ax,
                    (bx*magnitudeC - cx * magnitudeB)/(2*d) + ay);

                rightSection.Data.Edge.End = vertex;

                //next we create a two new edges
                newSection.Data.Edge = new VEdge(vertex, site, leftSection.Data.Site);
                rightSection.Data.Edge = new VEdge(vertex, rightSection.Data.Site, site);

                edges.AddFirst(newSection.Data.Edge);
                edges.AddFirst(rightSection.Data.Edge);

                //add neighbors for delaunay
                newSection.Data.Site.Neighbors.Add(leftSection.Data.Site);
                leftSection.Data.Site.Neighbors.Add(newSection.Data.Site);

                newSection.Data.Site.Neighbors.Add(rightSection.Data.Site);
                rightSection.Data.Site.Neighbors.Add(newSection.Data.Site);

                CheckCircle(leftSection, eventQueue);
                CheckCircle(rightSection, eventQueue);
            }
        }

        internal void RemoveBeachSection(FortuneCircleEvent circle, MinHeap<FortuneEvent> eventQueue, HashSet<FortuneCircleEvent> deleted, LinkedList<VEdge> edges)
        {
            var section = circle.ToDelete;
            var x = circle.X;
            var y = circle.YCenter;
            var vertex = new VPoint(x, y);

            //multiple edges could end here
            var toBeRemoved = new List<RBTreeNode<BeachSection>>();

            //look left
            var prev = section.Previous;
            while (prev.Data.CircleEvent != null && 
                (x - prev.Data.CircleEvent.X).ApproxEqual(0) && 
                (y - prev.Data.CircleEvent.Y).ApproxEqual(0))
            {
                toBeRemoved.Add(prev);
                prev = prev.Previous;
            }

            var next = section.Next;
            while (next.Data.CircleEvent != null &&
                (x - next.Data.CircleEvent.X).ApproxEqual(0) &&
                (y - next.Data.CircleEvent.Y).ApproxEqual(0))
            {
                toBeRemoved.Add(next);
                next = next.Next;
            }

            section.Data.Edge.End = vertex;
            section.Next.Data.Edge.End = vertex;
            section.Data.CircleEvent = null;

            //odds are this double writes a few edges but this is clean...
            foreach (var remove in toBeRemoved)
            {
                remove.Data.Edge.End = vertex;
                remove.Next.Data.Edge.End = vertex;
                deleted.Add(remove.Data.CircleEvent);
                remove.Data.CircleEvent = null;
            }


            //need to delete all upcoming circle events with this node
            if (prev.Data.CircleEvent != null)
            {
                deleted.Add(prev.Data.CircleEvent);
                prev.Data.CircleEvent = null;
            }
            if (next.Data.CircleEvent != null)
            {
                deleted.Add(next.Data.CircleEvent);
                next.Data.CircleEvent = null;
            }


            //create a new edge with start point at the vertex and assign it to next
            var newEdge = new VEdge(vertex, next.Data.Site, prev.Data.Site);
            next.Data.Edge = newEdge;
            edges.AddFirst(newEdge);

            //add neighbors for delaunay
            prev.Data.Site.Neighbors.Add(next.Data.Site);
            next.Data.Site.Neighbors.Add(prev.Data.Site);
            
            //remove the sectionfrom the tree
            beachLine.RemoveNode(section);
            foreach (var remove in toBeRemoved)
            {
                beachLine.RemoveNode(remove);
            }

            CheckCircle(prev, eventQueue);
            CheckCircle(next, eventQueue);
        }

        private static double LeftBreakpoint(RBTreeNode<BeachSection> node, double directrix)
        {
            var leftNode = node.Previous;
            //degenerate parabola
            if ((node.Data.Site.Y - directrix).ApproxEqual(0))
                return node.Data.Site.X;
            //node is the first piece of the beach line
            if (leftNode == null)
                return double.NegativeInfinity;
            //left node is degenerate
            if ((leftNode.Data.Site.Y - directrix).ApproxEqual(0))
                return leftNode.Data.Site.X;
            var site = node.Data.Site;
            var leftSite = leftNode.Data.Site;
            return ParabolaMath.IntersectParabolaX(leftSite.X, leftSite.Y, site.X, site.Y, directrix);
        }

        private static double RightBreakpoint(RBTreeNode<BeachSection> node, double directrix)
        {
            var rightNode = node.Next;
            //degenerate parabola
            if ((node.Data.Site.Y - directrix).ApproxEqual(0))
                return node.Data.Site.X;
            //node is the last piece of the beach line
            if (rightNode == null)
                return double.PositiveInfinity;
            //left node is degenerate
            if ((rightNode.Data.Site.Y - directrix).ApproxEqual(0))
                return rightNode.Data.Site.X;
            var site = node.Data.Site;
            var rightSite = rightNode.Data.Site;
            return ParabolaMath.IntersectParabolaX(site.X, site.Y, rightSite.X, rightSite.Y, directrix);
        }

        private static void CheckCircle(RBTreeNode<BeachSection> section, MinHeap<FortuneEvent> eventQueue)
        {
            //if (section == null)
            //    return;
            var left = section.Previous;
            var right = section.Next;
            if (left == null || right == null)
                return;

            var leftSite = left.Data.Site;
            var centerSite = section.Data.Site;
            var rightSite = right.Data.Site;

            //if the left arc and right arc are defined by the same
            //focus, the two arcs cannot converge
            if (leftSite == rightSite)
                return;

            // http://mathforum.org/library/drmath/view/55002.html
            // because every piece of this program needs to be demoed in maple >.<

            //MATH HACKS: place center at origin and
            //draw vectors a and c to
            //left and right respectively
            double bx = centerSite.X,
                by = centerSite.Y,
                ax = leftSite.X - bx,
                ay = leftSite.Y - by,
                cx = rightSite.X - bx,
                cy = rightSite.Y - by;

            //The center beach section can only dissapear when
            //the angle between a and c is negative
            var d = ax*cy - ay*cx;
            if (d.ApproxGreaterThanOrEqualTo(0))
                return;

            var magnitudeA = ax*ax + ay*ay;
            var magnitudeC = cx*cx + cy*cy;
            var x = (cy*magnitudeA - ay*magnitudeC)/(2*d);
            var y = (ax*magnitudeC - cx*magnitudeA)/(2*d);

            //add back offset
            var ycenter = y + by;
            //y center is off
            var circleEvent = new FortuneCircleEvent(
                new VPoint(x + bx, ycenter + Math.Sqrt(x * x + y * y)),
                ycenter, section
            );
            section.Data.CircleEvent = circleEvent;
            eventQueue.Insert(circleEvent);
        }
    }
}
