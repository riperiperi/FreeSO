using System;
using System.Collections.Generic;
using System.Drawing;

namespace SimplePaletteQuantizer.PathProviders
{
    public class ReversedPathProvider : IPathProvider
    {
        public IList<Point> GetPointPath(Int32 width, Int32 height)
        {
            List<Point> result = new List<Point>(width*height);

            for (Int32 y = height - 1; y >= 0; y--)
            for (Int32 x = width - 1; x >= 0; x--)
            {
                Point point = new Point(x, y);
                result.Add(point);
            }

            return result;
        }
    }
}
