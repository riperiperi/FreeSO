using Microsoft.Xna.Framework;
using System;
using System.Linq;

namespace FSO.Common.TS1
{
    /// <summary>
    /// 
    /// </summary>
    public class TS1Curve
    {
        public Point[] Points; // ordered by x

        public int CacheStart;
        public float[] Cache;
        public int[] CacheFixed;

        public TS1Curve(string input)
        {
            input = input.Replace(");(", ") (");
            var points = input.Split(' ');
            var pointParse = points.Select(pointStr =>
            {
                var pointClean = pointStr.Substring(1, pointStr.Length - 2);
                var split = pointClean.Split(';');
                return new Point(int.Parse(split[0]), int.Parse(split[1]));
            });

            Points = pointParse.OrderBy(pt => pt.X).ToArray();
        }

        public float GetPoint(float input)
        {
            if (input < Points[0].X) return Points[0].Y;
            // find the first point we're ahead of
            // we want to interpolate between that point and the one after it
            int i = 0;
            for (i = Points.Length-1; i >= 0; i--)
            {
                var point = Points[i];
                if (input >= point.X)
                {
                    break;
                }
            }
            if (i == Points.Length-1)
            {
                return Points[i].Y;
            }

            var start = Points[i];
            var end = Points[i + 1];
            var iF = (input - start.X) / (float)(end.X - start.X);
            return (1 - iF) * start.Y + iF * end.Y;
        }

        public int GetPointFixed(int input)
        {
            if (input < Points[0].X) return Points[0].Y;
            // find the first point we're ahead of
            // we want to interpolate between that point and the one after it
            int i = 0;
            for (i = 0; i < Points.Length; i++)
            {
                var point = Points[i];
                if (input >= point.X)
                {
                    break;
                }
            }
            if (i == Points.Length - 1)
            {
                return Points[i].Y;
            }

            var start = Points[i];
            var end = Points[i + 1];
            var iF = ((input - start.X) * 65536) / (end.X - start.X);
            return (65536 - iF) * start.Y + iF * end.Y;
        }

        // CACHED ACCESS

        public void BuildCache(int min, int max)
        {
            Cache = new float[(max - min) + 1];
            CacheFixed = new int[(max - min) + 1];
            CacheStart = min;

            for (int i=min; i<=max; i++)
            {
                Cache[i - min] = GetPoint(i);
                CacheFixed[i - min] = GetPointFixed(i);
            }
        }

        public float GetCachedPoint(int point)
        {
            point = Math.Max(CacheStart, Math.Min(CacheStart + Cache.Length - 1, point));
            return Cache[point];
        }

        public int GetCachedPointFixed(int point)
        {
            point = Math.Max(CacheStart, Math.Min(CacheStart + CacheFixed.Length - 1, point));
            return CacheFixed[point];
        }
    }
}
