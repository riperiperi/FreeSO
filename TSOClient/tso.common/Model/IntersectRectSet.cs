using Microsoft.Xna.Framework;

namespace FSO.Common.Model
{
    /// <summary>
    /// A k-d Tree for looking up rectangle intersections
    /// TODO: balancing? could make performance gains more stable at the cost of some of the worst case.
    /// </summary>
    public class IntersectRectTree 
    {
        public IntersectRectNode Root;

        public void Add(Rectangle rect)
        {
            if (Root == null)
            {
                Root = new IntersectRectNode
                {
                    Dimension = IntersectRectDimension.Left,
                    Rect = rect
                };
            } else
            {
                Root.AddAsChild(rect);
            }
        }

        public bool SearchForIntersect(Rectangle rect)
        {
            if (Root == null) return false;
            else
            {
                return Root.SearchForIntersect(rect);
            }
        }
    }

    public class IntersectRectNode
    {
        public IntersectRectNode LeftChild;
        public IntersectRectNode RightChild;
        public IntersectRectDimension Dimension;
        public Rectangle Rect;
        
        public void AddAsChild(Rectangle rect)
        {
            bool rightSide = false;
            switch (Dimension)
            {
                case IntersectRectDimension.Top:
                    rightSide = rect.Top > Rect.Top; break;
                case IntersectRectDimension.Left:
                    rightSide = rect.Left > Rect.Left; break;
                case IntersectRectDimension.Bottom:
                    rightSide = rect.Bottom > Rect.Bottom; break;
                case IntersectRectDimension.Right:
                    rightSide = rect.Right > Rect.Right; break;
            }
            if (rightSide)
            {
                if (RightChild != null) RightChild.AddAsChild(rect);
                else
                {
                    RightChild = new IntersectRectNode
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
                    LeftChild = new IntersectRectNode
                    {
                        Dimension = (IntersectRectDimension)(((int)Dimension + 1) % 4),
                        Rect = rect
                    };
                }
            }
        }

        public bool SearchForIntersect(Rectangle rect)
        {
            if (rect.Intersects(Rect)) return true;
            //search in child nodes.
            int dontSearch = 0;
            switch (Dimension)
            {
                case IntersectRectDimension.Top:
                    dontSearch = (rect.Bottom < Rect.Top)?2:0; break; //if true, do not have to search right (where top greater)
                case IntersectRectDimension.Left:
                    dontSearch = (rect.Right < Rect.Left)?2:0; break; //if true, do not have to search right (where left greater)
                case IntersectRectDimension.Bottom:
                    dontSearch = (rect.Top > Rect.Bottom)?1:0; break; //if true, do not have to search left (where bottom less)
                case IntersectRectDimension.Right:
                    dontSearch = (rect.Left > Rect.Right)?1:0; break; //if true, do not have to search left (where right less)
            }

            //may need to search both :'( won't happen often with our small rectangles over large space though.
            return ((dontSearch != 1 && LeftChild != null && LeftChild.SearchForIntersect(rect)) 
                || (dontSearch != 2 && RightChild != null && RightChild.SearchForIntersect(rect)));
        }
    }

    public enum IntersectRectDimension : byte
    {
        Top,
        Left,
        Bottom,
        Right
    }
}
