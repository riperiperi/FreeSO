using FSO.Common.Model;
using System;
using System.Collections.Generic;

namespace FSO.SimAntics.Model.Routing
{
    /// <summary>
    /// A k-d Tree for looking up rectangle intersections
    /// Ideally much faster lookups for routing rectangular cover, avatar and object movement. O(log n) vs O(n)
    /// 
    /// Concerns:
    ///  - Tree balancing: a random insert may be the best solution, as algorithms for this can be quite complex.
    ///  - Should be mutable, as rect cover routing will add new entries. We also may want to add or remove elements from a "static" set.
    ///  - Tree cloning: wall and object sets would be nice, but for routing ideally we want to add new free-rects to the set dynamically. This means we need to clone the tree.
    ///  - Return true if ANY found, or return ALL found. First useful for routing, second for checking collision validity.
    /// </summary>
    public class VMObstacleSetOld
    {
        public VMObstacleSetNodeOld Root;
        public int Count;

        public VMObstacleSetOld() { }

        public VMObstacleSetOld(VMObstacleSetOld last)
        {
            if (last.Root != null)
            {
                Count = last.Count;
                Root = new VMObstacleSetNodeOld(last.Root);
            }
        }

        public VMObstacleSetOld(IEnumerable<VMObstacle> obstacles)
        {
            foreach (var obstacle in obstacles)
                Add(obstacle);
        }

        public void Add(VMObstacle rect)
        {
            Count++;
            if (Root == null)
            {
                Root = new VMObstacleSetNodeOld
                {
                    Dimension = IntersectRectDimension.Left,
                    Rect = rect
                };
            }
            else
            {
                Root.AddAsChild(rect);
            }
        }

        public void RecursiveReAdd(VMObstacleSetNodeOld node)
        {
            Count--;
            Add(node.Rect);
            if (node.LeftChild != null) RecursiveReAdd(node.LeftChild);
            if (node.RightChild != null) RecursiveReAdd(node.RightChild);
        }

        public bool SearchForIntersect(VMObstacle rect)
        {
            if (Root == null) return false;
            else
            {
                return Root.SearchForIntersect(rect);
            }
        }

        public List<VMObstacle> AllIntersect(VMObstacle rect)
        {
            var result = new List<VMObstacle>();
            if (Root == null) return result;
            else
            {
                Root.AllIntersect(rect, result);
                return result;
            }
        }

        public List<VMObstacle> OnEdge(VMObstacle rect)
        {
            var result = new List<VMObstacle>();
            if (Root == null) return result;
            else
            {
                Root.OnEdge(rect, result);
                return result;
            }
        }

        public static VMObstacleSetOld RoughBalanced(List<VMObstacle> input)
        {
            //roughly attempts to balance the set. 
            //...currently by random shuffle. at least it's deterministic?
            var rand = new Random(1);
            for (int i=1; i<input.Count; i++)
            {
                var swap = input[i-1];
                var ind = rand.Next(input.Count - i) + i;
                input[i - 1] = input[ind];
                input[ind] = swap;
            }

            return new VMObstacleSetOld(input);
        }

        public bool Delete(VMEntityObstacle rect)
        {
            if (Root == null) return false;
            else
            {
                var result = Root.Delete(rect, null, this);
                if (!result) { }
                return result;
            }
        }
    }

    public class VMObstacleSetNodeOld
    {
        public VMObstacleSetNodeOld LeftChild;
        public VMObstacleSetNodeOld RightChild;
        public IntersectRectDimension Dimension;
        public VMObstacle Rect;

        public VMObstacleSetNodeOld() { }

        public VMObstacleSetNodeOld(VMObstacleSetNodeOld last)
        {
            Rect = last.Rect;
            Dimension = last.Dimension;
            if (last.LeftChild != null) LeftChild = new VMObstacleSetNodeOld(last.LeftChild);
            if (last.RightChild != null) RightChild = new VMObstacleSetNodeOld(last.RightChild);
        }

        public void AddAsChild(VMObstacle rect)
        {
            bool rightSide = false;
            switch (Dimension)
            {
                case IntersectRectDimension.Top:
                    rightSide = rect.y1 > Rect.y1; break;
                case IntersectRectDimension.Left:
                    rightSide = rect.x1 > Rect.x1; break;
                case IntersectRectDimension.Bottom:
                    rightSide = rect.y2 > Rect.y2; break;
                case IntersectRectDimension.Right:
                    rightSide = rect.x2 > Rect.x2; break;
            }
            if (rightSide)
            {
                if (RightChild != null) RightChild.AddAsChild(rect);
                else
                {
                    RightChild = new VMObstacleSetNodeOld
                    {
                        Dimension = (IntersectRectDimension)(((int)Dimension + 1) % 4),
                        Rect = rect
                    };
                }
            }
            else
            {
                if (LeftChild != null) LeftChild.AddAsChild(rect);
                else
                {
                    LeftChild = new VMObstacleSetNodeOld
                    {
                        Dimension = (IntersectRectDimension)(((int)Dimension + 1) % 4),
                        Rect = rect
                    };
                }
            }
        }

        public bool SearchForIntersect(VMObstacle rect)
        {
            if (rect.Intersects(Rect)) return true;
            //search in child nodes.
            int dontSearch = 0;
            switch (Dimension)
            {
                case IntersectRectDimension.Top:
                    dontSearch = (rect.y2 <= Rect.y1) ? 2 : 0; break; //if true, do not have to search right (where top greater)
                case IntersectRectDimension.Left:
                    dontSearch = (rect.x2 <= Rect.x1) ? 2 : 0; break; //if true, do not have to search right (where left greater)
                case IntersectRectDimension.Bottom:
                    dontSearch = (rect.y1 >= Rect.y2) ? 1 : 0; break; //if true, do not have to search left (where bottom less)
                case IntersectRectDimension.Right:
                    dontSearch = (rect.x1 >= Rect.x2) ? 1 : 0; break; //if true, do not have to search left (where right less)
            }

            //may need to search both :'( won't happen often with our small rectangles over large space though.
            return ((dontSearch != 1 && LeftChild != null && LeftChild.SearchForIntersect(rect))
                || (dontSearch != 2 && RightChild != null && RightChild.SearchForIntersect(rect)));
        }

        public void AllIntersect(VMObstacle rect, List<VMObstacle> result)
        {
            if (rect.Intersects(Rect)) result.Add(Rect);
            //search in child nodes.
            int dontSearch = 0;
            switch (Dimension)
            {
                case IntersectRectDimension.Top:
                    dontSearch = (rect.y2 <= Rect.y1) ? 2 : 0; break; //if true, do not have to search right (where top greater)
                case IntersectRectDimension.Left:
                    dontSearch = (rect.x2 <= Rect.x1) ? 2 : 0; break; //if true, do not have to search right (where left greater)
                case IntersectRectDimension.Bottom:
                    dontSearch = (rect.y1 >= Rect.y2) ? 1 : 0; break; //if true, do not have to search left (where bottom less)
                case IntersectRectDimension.Right:
                    dontSearch = (rect.x1 >= Rect.x2) ? 1 : 0; break; //if true, do not have to search left (where right less)
            }

            //may need to search both :'( won't happen often with our small rectangles over large space though.
            //if (LeftChild != null) LeftChild.AllIntersect(rect, result);
            //if (RightChild != null) RightChild.AllIntersect(rect, result);

            if (dontSearch != 1 && LeftChild != null) LeftChild.AllIntersect(rect, result);
            if (dontSearch != 2 && RightChild != null) RightChild.AllIntersect(rect, result);
        }

        public void OnEdge(VMObstacle rect, List<VMObstacle> result)
        {
            if (rect.OnEdge(Rect)) result.Add(Rect);
            //search in child nodes.
            //binary search to find equal opposing edges.
            int dontSearch = 0;
            switch (Dimension)
            {
                case IntersectRectDimension.Top:
                    dontSearch = (rect.y2 < Rect.y1) ? 2 : 0; break; //if true, do not have to search right (where top greater)
                case IntersectRectDimension.Left:
                    dontSearch = (rect.x2 < Rect.x1) ? 2 : 0; break; //if true, do not have to search right (where left greater)
                case IntersectRectDimension.Bottom:
                    dontSearch = (rect.y1 > Rect.y2) ? 1 : 0; break; //if true, do not have to search left (where bottom less)
                case IntersectRectDimension.Right:
                    dontSearch = (rect.x1 > Rect.x2) ? 1 : 0; break; //if true, do not have to search left (where right less)
            }

            //may need to search both :'( won't happen often with our small rectangles over large space though.
            //if (LeftChild != null) LeftChild.AllIntersect(rect, result);
            //if (RightChild != null) RightChild.AllIntersect(rect, result);

            if (dontSearch != 1 && LeftChild != null) LeftChild.OnEdge(rect, result);
            if (dontSearch != 2 && RightChild != null) RightChild.OnEdge(rect, result);
        }

        public bool Delete(VMEntityObstacle rect, VMObstacleSetNodeOld parent, VMObstacleSetOld set)
        {
            if (rect.Parent == (Rect as VMEntityObstacle).Parent) {
                if (parent == null)
                {
                    set.Root = null;
                }
                else
                {
                    if (parent.LeftChild == this) parent.LeftChild = null;
                    if (parent.RightChild == this) parent.RightChild = null;
                }
                if (LeftChild != null) set.RecursiveReAdd(LeftChild);
                if (RightChild != null) set.RecursiveReAdd(RightChild);
                return true;
            }
            //search in child nodes.
            //binary search to find equal opposing edges.

            bool rightSide = false;
            switch (Dimension)
            {
                case IntersectRectDimension.Top:
                    rightSide = rect.y1 > Rect.y1; break;
                case IntersectRectDimension.Left:
                    rightSide = rect.x1 > Rect.x1; break;
                case IntersectRectDimension.Bottom:
                    rightSide = rect.y2 > Rect.y2; break;
                case IntersectRectDimension.Right:
                    rightSide = rect.x2 > Rect.x2; break;
            }

            return ((rightSide && RightChild != null && RightChild.Delete(rect, this, set))
            || (!rightSide && LeftChild != null && LeftChild.Delete(rect, this, set)));
        }
    }
}
