using FSO.LotView.Model;
using Microsoft.Xna.Framework;
using System.Runtime.CompilerServices;

namespace FSO.LotView.Utils
{
    internal interface ITileRaycastTarget<TResult>
        where TResult : struct
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static abstract (float, TResult)? TestRay(Ray ray, Point tile, Point nextTile, float? edge, sbyte level, Blueprint bp);
    }

    public struct WallRayHit { public WallSegments Segment; public Point Tile; }

    internal class WallTileRaycastTarget : ITileRaycastTarget<WallRayHit>
    {
        // Wall height in world units: 2.95 floors * 3 units/floor.
        private const float WallHeight = 8.85f;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (float, WallRayHit)? TestRay(Ray ray, Point tile, Point nextTile, float? edge, sbyte level, Blueprint bp)
        {
            var segs = bp.GetWall((short)tile.X, (short)tile.Y, level).Segments;
            if (segs == 0) return null;

            float hitDist;
            WallSegments hitSegment;

            if ((segs & WallSegments.AnyDiag) != 0)
            {
                var isVertical = (segs & WallSegments.VerticalDiag) != 0;
                var mid = new Vector3((tile.X + 0.5f) * 3, 0, (tile.Y + 0.5f) * 3);
                var corner = mid + (isVertical ? new Vector3(-1.5f, 0, -1.5f) : new Vector3(-1.5f, 0, 1.5f));
                var d = ray.Intersects(new Plane(mid, mid + Vector3.Up, corner));
                if (!d.HasValue) return null;
                hitDist = d.Value;
                hitSegment = isVertical ? WallSegments.VerticalDiag : WallSegments.HorizontalDiag;
            }
            else
            {
                if ((segs & WallSegments.AnyAdj) == 0 || !edge.HasValue) return null;

                WallSegments edgeSegs = 0;
                var dx = nextTile.X - tile.X;
                var dy = nextTile.Y - tile.Y;
                if (dy > 0) edgeSegs |= WallSegments.BottomLeft;
                if (dx < 0) edgeSegs |= WallSegments.TopLeft;
                if (dy < 0) edgeSegs |= WallSegments.TopRight;
                if (dx > 0) edgeSegs |= WallSegments.BottomRight;

                hitSegment = edgeSegs & segs;
                if (hitSegment == 0) return null;
                hitDist = edge.Value;
            }

            var wallBottom = bp.GetAltitude(tile.X, tile.Y) * 3;
            var rayY = ray.Position.Y + ray.Direction.Y * hitDist;
            if (rayY < wallBottom || rayY >= wallBottom + WallHeight) return null;

            return (hitDist, new WallRayHit { Segment = hitSegment, Tile = tile });
        }
    }

    internal class CombinedTileRaycastTarget<TFirst, TFirstResult, TSecond, TSecondResult> : ITileRaycastTarget<(TFirstResult?, TSecondResult?)>
        where TFirst : ITileRaycastTarget<TFirstResult>
        where TSecond : ITileRaycastTarget<TSecondResult>
        where TFirstResult : struct
        where TSecondResult : struct
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (float, (TFirstResult?, TSecondResult?))? TestRay(Ray ray, Point tile, Point nextTile, float? edge, sbyte level, Blueprint bp)
        {
            var first = TFirst.TestRay(ray, tile, nextTile, edge, level, bp);
            var second = TSecond.TestRay(ray, tile, nextTile, edge, level, bp);

            if (first.HasValue && second.HasValue)
            {
                if (first.Value.Item1 <= second.Value.Item1)
                {
                    return (first.Value.Item1, (first.Value.Item2, default));
                }
                else
                {
                    return (second.Value.Item1, (default, second.Value.Item2));
                }
            }

            if (first.HasValue)
            {
                return (first.Value.Item1, (first.Value.Item2, default));
            }

            if (second.HasValue)
            {
                return (second.Value.Item1, (default, second.Value.Item2));
            }

            return null;
        }
    }

    internal class TileRaycaster<T, TResult>
        where T : ITileRaycastTarget<TResult>
        where TResult : struct
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float? BoxRC2(Ray ray, float tileSize)
        {
            //find next tile boundary
            var px = ray.Direction.X > 0;
            var py = ray.Direction.Z > 0;
            int x = !px ? (int)MathF.Ceiling(ray.Position.X / tileSize) : (int)(ray.Position.X / tileSize);
            int y = !py ? (int)MathF.Ceiling(ray.Position.Z / tileSize) : (int)(ray.Position.Z / tileSize);
            float nx = (px ? x + 1 : x - 1) * tileSize;
            float ny = (py ? y + 1 : y - 1) * tileSize;

            const float Epsilon = 1e-6f;
            float? min = null;
            if (MathF.Abs(ray.Direction.X) > Epsilon)
            {
                min = (nx - ray.Position.X) / ray.Direction.X;
            }
            if (MathF.Abs(ray.Direction.Z) > Epsilon)
            {
                var min2 = (ny - ray.Position.Z) / ray.Direction.Z;
                if (min == null || min.Value > min2) min = min2;
            }
            return min;
        }

        public static (float, TResult)? Raycast(Ray ray, sbyte level, Blueprint bp, float maxDist)
        {
            if (bp?.Altitude == null) return null;

            var baseBox = new BoundingBox(new Vector3(0, -5000, 0), new Vector3(bp.Width * 3, 5000, bp.Height * 3));
            if (baseBox.Contains(ray.Position) != ContainmentType.Contains)
            {
                var i = baseBox.Intersects(ray);
                if (i == null || i.Value > maxDist) return null;
                ray.Position += ray.Direction * (i.Value + 0.01f);
            }

            var mx = (int)ray.Position.X / 3;
            var my = (int)ray.Position.Z / 3;
            var px = ray.Direction.X > 0;
            var py = ray.Direction.Z > 0;
            float totalDist = 0;
            int width = bp.Width;

            for (int iteration = 0; iteration < 1000; iteration++)
            {
                if ((uint)mx >= (uint)width || (uint)my >= (uint)width) break;

                var tileDist = BoxRC2(ray, 3);
                if (tileDist == null) break;

                float addDist = tileDist.Value + 0.00001f;
                var nextPos = ray.Position + ray.Direction * addDist;
                int nextX = !px ? ((int)MathF.Ceiling(nextPos.X / 3) - 1) : (int)(nextPos.X / 3);
                int nextY = !py ? ((int)MathF.Ceiling(nextPos.Z / 3) - 1) : (int)(nextPos.Z / 3);

                var result = T.TestRay(ray, new Point(mx, my), new Point(nextX, nextY), tileDist, level, bp);
                if (result != null && result.Value.Item1 <= tileDist)
                {
                    var hitDist = totalDist + result.Value.Item1 + 0.00001f;
                    if (hitDist > maxDist) return null;
                    return (hitDist, result.Value.Item2);
                }

                totalDist += addDist;
                if (totalDist > maxDist) break;

                ray.Position = nextPos;
                mx = nextX;
                my = nextY;
            }

            return null;
        }

        public static (float, TResult)? RaycastMultifloor(Ray ray, Blueprint bp, float maxDist, int maxFloor = -1)
        {
            if (maxFloor == -1) maxFloor = bp.Stories;

            (float, TResult)? bestResult = null;
            for (int i = 1; i <= maxFloor; i++)
            {
                var result = Raycast(ray, (sbyte)i, bp, maxDist);
                if (result != null && (bestResult == null || result.Value.Item1 < bestResult.Value.Item1))
                {
                    bestResult = result;
                    maxDist = result.Value.Item1;
                }
                // Next floor
                ray.Position -= new Vector3(0, 2.95f * 3, 0);
            }

            return bestResult;
        }
    }

    internal class WallRaycaster : TileRaycaster<WallTileRaycastTarget, WallRayHit>
    {

    }
}
