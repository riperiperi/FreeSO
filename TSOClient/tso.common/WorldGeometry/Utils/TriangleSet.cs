using FSO.Common.Model;
using FSO.Common.WorldGeometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FSO.SimAntics.Model.Routing
{
    /// <summary>
    /// (copied from VMObstacleSet)
    /// A k-d Tree for looking up rectangle intersections
    /// Ideally much faster lookups for routing rectangular cover, avatar and object movement. O(log n) vs O(n)
    /// 
    /// Concerns:
    ///  - Tree balancing: a random insert may be the best solution, as algorithms for this can be quite complex.
    ///  - Should be mutable, as rect cover routing will add new entries. We also may want to add or remove elements from a "static" set.
    ///  - Tree cloning: wall and object sets would be nice, but for routing ideally we want to add new free-rects to the set dynamically. This means we need to clone the tree.
    ///  - Return true if ANY found, or return ALL found. First useful for routing, second for checking collision validity.
    /// </summary>
    public class BaseTriangleSet
    {
        public VMObstacleSetNode[] Nodes;
        protected List<int> FreeList = new List<int>();
        protected int PoolInd = 0;
        public int Root = -1;
        public int Count;

        public BaseTriangleSet()
        {
            InitNodes(64);
        }

        public BaseTriangleSet(BaseTriangleSet last)
        {
            if (last.Root != -1)
            {
                Count = last.Count;
                Nodes = (VMObstacleSetNode[])last.Nodes.Clone();
                Root = last.Root;
                FreeList = last.FreeList.ToList();
                PoolInd = last.PoolInd;
            }
            else
            {
                InitNodes(64);
            }
        }

        public BaseTriangleSet(IEnumerable<BaseMeshTriangle> obstacles)
        {
            InitNodes(obstacles.Count());
            foreach (var obstacle in obstacles)
                Add(obstacle);
        }

        private void InitNodes(int capacity)
        {
            if (Nodes == null)
            {
                Nodes = new VMObstacleSetNode[capacity];
            }
            else
            {
                Array.Resize(ref Nodes, capacity);
            }
        }

        private int GetNode()
        {
            if (FreeList.Count > 0)
            {
                var free = FreeList.Last();
                FreeList.RemoveAt(FreeList.Count - 1);
                return free;
            }
            else
            {
                return PoolInd++;
            }
        }

        private int GetNode(IntersectRectDimension dir, BaseMeshTriangle rect)
        {
            var ind = GetNode();
            Nodes[ind] = new VMObstacleSetNode()
            {
                Dimension = dir,
                Rect = rect,
                LeftChild = -1,
                RightChild = -1,
                Index = ind,

                x1 = rect.x1,
                x2 = rect.x2,
                y1 = rect.y1,
                y2 = rect.y2
            };
            return ind;
        }

        private void Reclaim(int index)
        {
            if (index == PoolInd - 1) PoolInd--;
            else FreeList.Add(index);
        }

        public void Add(BaseMeshTriangle rect)
        {
            if (PoolInd >= Nodes.Length && FreeList.Count == 0) InitNodes(Nodes.Length * 2);
            Count++;
            if (Root == -1)
            {
                Root = GetNode(IntersectRectDimension.Left, rect);
            }
            else
            {
                AddAsChild(ref Nodes[Root], rect);
            }
        }

        private void AddAsChild(ref VMObstacleSetNode node, BaseMeshTriangle rect)
        {
            bool rightSide = false;
            switch (node.Dimension)
            {
                case IntersectRectDimension.Top:
                    rightSide = rect.y1 > node.Rect.y1; break;
                case IntersectRectDimension.Left:
                    rightSide = rect.x1 > node.Rect.x1; break;
                case IntersectRectDimension.Bottom:
                    rightSide = rect.y2 > node.Rect.y2; break;
                case IntersectRectDimension.Right:
                    rightSide = rect.x2 > node.Rect.x2; break;
            }
            if (rightSide)
            {
                if (node.RightChild != -1) AddAsChild(ref Nodes[node.RightChild], rect);
                else
                {
                    node.RightChild = GetNode((IntersectRectDimension)(((int)node.Dimension + 1) % 4), rect);
                }
            }
            else
            {
                if (node.LeftChild != -1) AddAsChild(ref Nodes[node.LeftChild], rect);
                else
                {
                    node.LeftChild = GetNode((IntersectRectDimension)(((int)node.Dimension + 1) % 4), rect);
                }
            }
        }

        public void RecursiveReAdd(VMObstacleSetNode node)
        {
            Count--;
            Reclaim(node.Index);
            Add(node.Rect);
            if (node.LeftChild != -1) RecursiveReAdd(Nodes[node.LeftChild]);
            if (node.RightChild != -1) RecursiveReAdd(Nodes[node.RightChild]);
        }

        public bool SearchForIntersect(BaseMeshTriangle rect)
        {
            if (Root == -1) return false;
            else
            {
                return SearchForIntersect(ref Nodes[Root], rect);
            }
        }

        public bool SearchForIntersect(ref VMObstacleSetNode node, BaseMeshTriangle rect)
        {
            if (node.Intersects(rect)) return true;
            //search in child nodes.
            int dontSearch = 0;
            switch (node.Dimension)
            {
                case IntersectRectDimension.Top:
                    dontSearch = (rect.y2 <= node.y1) ? 2 : 0; break; //if true, do not have to search right (where top greater)
                case IntersectRectDimension.Left:
                    dontSearch = (rect.x2 <= node.x1) ? 2 : 0; break; //if true, do not have to search right (where left greater)
                case IntersectRectDimension.Bottom:
                    dontSearch = (rect.y1 >= node.y2) ? 1 : 0; break; //if true, do not have to search left (where bottom less)
                case IntersectRectDimension.Right:
                    dontSearch = (rect.x1 >= node.x2) ? 1 : 0; break; //if true, do not have to search left (where right less)
            }

            //may need to search both :'( won't happen often with our small rectangles over large space though.
            return ((dontSearch != 1 && node.LeftChild != -1 && SearchForIntersect(ref Nodes[node.LeftChild], rect))
                || (dontSearch != 2 && node.RightChild != -1 && SearchForIntersect(ref Nodes[node.RightChild], rect)));
        }

        public List<BaseMeshTriangle> AllIntersect(BaseMeshTriangle rect)
        {
            var result = new List<BaseMeshTriangle>();
            if (Root == -1) return result;
            else
            {
                AllIntersect(ref Nodes[Root], rect, result);
                return result;
            }
        }

        public void AllIntersect(ref VMObstacleSetNode node, BaseMeshTriangle rect, List<BaseMeshTriangle> result)
        {
            if (node.Intersects(rect)) result.Add(node.Rect);
            //search in child nodes.
            int dontSearch = 0;
            switch (node.Dimension)
            {
                case IntersectRectDimension.Top:
                    dontSearch = (rect.y2 <= node.y1) ? 2 : 0; break; //if true, do not have to search right (where top greater)
                case IntersectRectDimension.Left:
                    dontSearch = (rect.x2 <= node.x1) ? 2 : 0; break; //if true, do not have to search right (where left greater)
                case IntersectRectDimension.Bottom:
                    dontSearch = (rect.y1 >= node.y2) ? 1 : 0; break; //if true, do not have to search left (where bottom less)
                case IntersectRectDimension.Right:
                    dontSearch = (rect.x1 >= node.x2) ? 1 : 0; break; //if true, do not have to search left (where right less)
            }

            //may need to search both :'( won't happen often with our small rectangles over large space though.
            //if (node.LeftChild != -1) AllIntersect(ref Nodes[node.LeftChild], rect, result);
            //if (node.RightChild != -1) AllIntersect(ref Nodes[node.RightChild], rect, result);

            if (dontSearch != 1 && node.LeftChild != -1) AllIntersect(ref Nodes[node.LeftChild], rect, result);
            if (dontSearch != 2 && node.RightChild != -1) AllIntersect(ref Nodes[node.RightChild], rect, result);
        }

        public List<BaseMeshTriangle> OnEdge(BaseMeshTriangle rect)
        {
            var result = new List<BaseMeshTriangle>();
            if (Root == -1) return result;
            else
            {
                OnEdge(ref Nodes[Root], rect, result);
                return result;
            }
        }

        public void OnEdge(ref VMObstacleSetNode node, BaseMeshTriangle rect, List<BaseMeshTriangle> result)
        {
            if (node.OnEdge(rect)) result.Add(node.Rect);
            //search in child nodes.
            //binary search to find equal opposing edges.
            int dontSearch = 0;
            switch (node.Dimension)
            {
                case IntersectRectDimension.Top:
                    dontSearch = (rect.y2 < node.y1) ? 2 : 0; break; //if true, do not have to search right (where top greater)
                case IntersectRectDimension.Left:
                    dontSearch = (rect.x2 < node.x1) ? 2 : 0; break; //if true, do not have to search right (where left greater)
                case IntersectRectDimension.Bottom:
                    dontSearch = (rect.y1 > node.y2) ? 1 : 0; break; //if true, do not have to search left (where bottom less)
                case IntersectRectDimension.Right:
                    dontSearch = (rect.x1 > node.x2) ? 1 : 0; break; //if true, do not have to search left (where right less)
            }

            //may need to search both :'( won't happen often with our small rectangles over large space though.
            //if (node.LeftChild != -1) OnEdge(ref Nodes[node.LeftChild], rect, result);
            //if (node.RightChild != -1) OnEdge(ref Nodes[node.RightChild], rect, result);

            if (dontSearch != 1 && node.LeftChild != -1) OnEdge(ref Nodes[node.LeftChild], rect, result);
            if (dontSearch != 2 && node.RightChild != -1) OnEdge(ref Nodes[node.RightChild], rect, result);
        }

        public static BaseTriangleSet RoughBalanced(List<BaseMeshTriangle> input)
        {
            //roughly attempts to balance the set. 
            //...currently by random shuffle. at least it's deterministic?
            var rand = new Random(1);
            for (int i = 1; i < input.Count; i++)
            {
                var swap = input[i - 1];
                var ind = rand.Next(input.Count - i) + i;
                input[i - 1] = input[ind];
                input[ind] = swap;
            }

            return new BaseTriangleSet(input);
        }

        /*
        public bool Delete(VMEntityObstacle rect)
        {
            if (Root == -1) return false;
            else
            {
                var result = Delete(ref Nodes[Root], rect, ref Nodes[Root]);
                if (result) { Count--; }
                return result;
            }
        }
        */

        /*
        public bool Delete(ref VMObstacleSetNode node, VMEntityObstacle rect, ref VMObstacleSetNode parent)
        {
            if (rect.Parent == (node.Rect as VMEntityObstacle).Parent)
            {
                if (parent.Index == node.Index)
                {
                    Root = -1;
                }
                else
                {
                    if (parent.LeftChild == node.Index) parent.LeftChild = -1;
                    if (parent.RightChild == node.Index) parent.RightChild = -1;
                }
                if (node.LeftChild != -1) RecursiveReAdd(Nodes[node.LeftChild]);
                if (node.RightChild != -1) RecursiveReAdd(Nodes[node.RightChild]);
                Reclaim(node.Index);
                return true;
            }
            //search in child nodes.
            //binary search to find equal opposing edges.

            bool rightSide = false;
            switch (node.Dimension)
            {
                case IntersectRectDimension.Top:
                    rightSide = rect.y1 > node.y1; break;
                case IntersectRectDimension.Left:
                    rightSide = rect.x1 > node.x1; break;
                case IntersectRectDimension.Bottom:
                    rightSide = rect.y2 > node.y2; break;
                case IntersectRectDimension.Right:
                    rightSide = rect.x2 > node.x2; break;
            }

            return ((rightSide && node.RightChild != -1 && Delete(ref Nodes[node.RightChild], rect, ref node))
            || (!rightSide && node.LeftChild != -1 && Delete(ref Nodes[node.LeftChild], rect, ref node)));
        }
        */
    }

    public struct VMObstacleSetNode
    {
        public int LeftChild;
        public int RightChild;
        public IntersectRectDimension Dimension;
        public BaseMeshTriangle Rect;
        public int Index;

        public float x1;
        public float x2;
        public float y1;
        public float y2;

        public bool Intersects(BaseMeshTriangle other)
        {
            return !((other.x1 >= x2 || other.x2 <= x1) || (other.y1 >= y2 || other.y2 <= y1));
        }

        public bool OnEdge(BaseMeshTriangle other)
        {
            return (x2 == other.x1) || (x1 == other.x2) || (y1 == other.y2) || (y2 == other.y1);
        }
    }
}
