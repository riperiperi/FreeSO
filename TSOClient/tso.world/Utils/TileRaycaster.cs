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

    internal class WallTileRaycastTarget : ITileRaycastTarget<ushort>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (float, ushort)? TestRay(Ray ray, Point tile, Point nextTile, float? edge, sbyte level, Blueprint bp)
        {
            var wall = bp.GetWall((short)tile.X, (short)tile.Y, level);

            if (wall.Segments != 0)
            {
                float wallBottom = bp.GetAltitude(tile.X, tile.Y) * 3;
                float wallTop = wallBottom + 2.95f * 3f;

                float? wallIntersectPoint = default;
                ushort wallId = 0;

                if ((wall.Segments & WallSegments.AnyDiag) != 0)
                {
                    var mid = new Vector3((tile.X + 0.5f) * 3, 0, (tile.Y + 0.5f) * 3);
                    var corner = mid + (wall.Segments.HasFlag(WallSegments.VerticalDiag) ? new Vector3(-1.5f, 0, -1.5f) : new Vector3(-1.5f, 0, 1.5f));
                    var plane = new Plane(mid, mid + Vector3.Up, corner);

                    var dist = ray.Intersects(plane);
                    if (dist.HasValue)
                    {
                        wallIntersectPoint = dist.Value;
                        wallId = 1; // TODO
                    }
                }
                else if ((wall.Segments & WallSegments.AnyAdj) != 0 && edge.HasValue)
                {
                    var bound = nextTile - tile;

                    WallSegments edgeSegs = 0;
                    if (bound.Y > 0) edgeSegs |= WallSegments.BottomLeft;
                    if (bound.X < 0) edgeSegs |= WallSegments.TopLeft;
                    if (bound.Y < 0) edgeSegs |= WallSegments.TopRight;
                    if (bound.X > 0) edgeSegs |= WallSegments.BottomRight;

                    if ((edgeSegs & wall.Segments) != 0)
                    {
                        wallIntersectPoint = edge;
                        wallId = 1; //TODO
                    }
                }

                if (wallIntersectPoint != null)
                {
                    var rayY = ray.Position.Y + ray.Direction.Y * wallIntersectPoint.Value;

                    if (rayY >= wallBottom && rayY < wallTop)
                    {
                        return (wallIntersectPoint.Value, wallId);
                    }
                }
            }

            return null;
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
        private static float? BoxRC2(Ray ray, float tileSize)
        {
            var px = (ray.Direction.X > 0);
            var py = (ray.Direction.Z > 0);
            //find current tile
            int x = (!px) ? (int)Math.Ceiling(ray.Position.X / tileSize) :
                           (int)(ray.Position.X / tileSize);
            int y = (!py) ? (int)Math.Ceiling(ray.Position.Z / tileSize) :
                           (int)(ray.Position.Z / tileSize);

            //find next tile boundary
            float nx = ((px) ? (x + 1) : (x - 1)) * 3;
            float ny = ((py) ? (y + 1) : (y - 1)) * 3;

            const float Epsilon = 1e-6f;
            float? min = null;
            if (Math.Abs(ray.Direction.X) > Epsilon)
            {
                min = (nx - ray.Position.X) / ray.Direction.X;
            }

            if (Math.Abs(ray.Direction.Z) > Epsilon)
            {
                var min2 = (ny - ray.Position.Z) / ray.Direction.Z;
                if (min == null || min.Value > min2) min = min2;
            }
            return min;
        }

        public static (float, TResult)? Raycast(Ray ray, sbyte level, Blueprint bp, float maxDist)
        {
            Ray baseRay = ray;
            var baseBox = new BoundingBox(new Vector3(0, -5000, 0), new Vector3(bp.Width * 3, 5000, bp.Height * 3));
            if (baseBox.Contains(ray.Position) != ContainmentType.Contains)
            {
                //move ray start inside box
                var i = baseBox.Intersects(ray);
                if (i != null)
                {
                    ray.Position += ray.Direction * (i.Value + 0.01f);
                }
            }

            var mx = (int)ray.Position.X / 3;
            var my = (int)ray.Position.Z / 3;

            var px = (ray.Direction.X > 0);
            var py = (ray.Direction.Z > 0);

            var canProj = bp?.Altitude != null;

            float totalDist = 0;

            int iteration = 0;
            while (mx >= 0 && mx < bp.Width && my >= 0 && my < bp.Width && canProj)
            {
                var tileDist = BoxRC2(ray, 3); // T to the next tile

                Ray nextRay = ray;

                if (tileDist == null) break;

                float addDist = (tileDist.Value + 0.00001f);
                nextRay.Position += nextRay.Direction * addDist;

                int nextX = (!px) ? ((int)Math.Ceiling(nextRay.Position.X / 3) - 1) :
                               (int)(nextRay.Position.X / 3);
                int nextY = (!py) ? ((int)Math.Ceiling(nextRay.Position.Z / 3) - 1) :
                               (int)(nextRay.Position.Z / 3);

                var result = T.TestRay(ray, new Point(mx, my), new Point(nextX, nextY), tileDist, level, bp);

                if (tileDist != null && result != null && result.Value.Item1 <= tileDist)
                {
                    addDist = result.Value.Item1 + 0.00001f;
                    totalDist += addDist;

                    if (totalDist > maxDist)
                    {
                        return null;
                    }

                    // The result was hit first.
                    return (totalDist, result.Value.Item2);
                }

                ray = nextRay;
                totalDist += addDist;

                mx = nextX;
                my = nextY;

                if (iteration++ > 1000 || totalDist > maxDist) break;
            }

            return null;
        }

        public static (float, TResult)? RaycastMultifloor(Ray ray, Blueprint bp, float maxDist, int maxFloor = -1)
        {
            if (maxFloor == -1)
            {
                maxFloor = bp.Stories;
            }

            (float, TResult)? bestResult = null;
            for (int i = 1; i <= maxFloor; i++)
            {
                var result = Raycast(ray, (sbyte)i, bp, maxDist);

                if (result != null && (bestResult == null || result.Value.Item1 < bestResult.Value.Item1))
                {
                    bestResult = result;
                }

                // Next floor
                ray.Position -= new Vector3(0, 2.95f * 3, 0);
            }

            return bestResult;
        }
    }

    internal class WallRaycaster : TileRaycaster<WallTileRaycastTarget, ushort>
    {

    }
}
