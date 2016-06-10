using System;
using System.Collections.Generic;
using System.Drawing;

namespace SimplePaletteQuantizer.PathProviders
{
    public class SerpentinePathProvider : IPathProvider
    {
        public IList<Point> GetPointPath(Int32 width, Int32 height)
        {
            Boolean leftToRight = true;
            List<Point> result = new List<Point>(width * height);

            for (Int32 y = 0; y < height; y++)
            {
                for (Int32 x = leftToRight ? 0 : width - 1; leftToRight ? x < width : x >= 0; x += leftToRight ? 1 : -1)
                {
                    Point point = new Point(x, y);
                    result.Add(point);
                }

                leftToRight = !leftToRight;
            }

            return result;
        }
    }
}
