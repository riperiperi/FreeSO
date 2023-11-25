using FSO.Common.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FSO.LotView.LMap
{
    public class ShadowGeometry
    {
        private static float WallPenumbra = (float)(Math.PI / 12); //offset on both directions
        private static float PenCos = (float)Math.Cos(WallPenumbra);

        private static float DistanceMult = 10000f;


        public static Tuple<GradVertex[], int[]> GenerateObjShadows(List<Rectangle> walls, LightData pointLight)
        {
            List<Rectangle> topDown = new List<Rectangle>();
            List<Vector2[]> projWalls = new List<Vector2[]>();
            List<Vector3> ctrWidths = new List<Vector3>();

            foreach (var i in walls)
            {
                if (i.Contains(pointLight.LightPos)) topDown.Add(i);
                else {
                    projWalls.Add(ClosestPtsClockwise(i, pointLight.LightPos));
                    var ctr = i.Center;
                    ctrWidths.Add(new Vector3(ctr.X, ctr.Y, (float)Math.Sqrt(i.Width * i.Width + i.Height * i.Height) / 2.5f));
                }
            }
            return GenerateShadows(projWalls, pointLight, ctrWidths.ToList(), topDown);
        }

        public static Tuple<GradVertex[], int[]> GenerateWallShadows(List<Vector2[]> walls, LightData pointLight)
        {
            var projWalls = walls.Select(x => ClockwiseLine(x, pointLight.LightPos));
            return GenerateShadows(projWalls, pointLight, null, null);
        }

        public static Tuple<GradVertex[], int[]> GenerateShadows(IEnumerable<Vector2[]> volumes, LightData light, List<Vector3> ctrWidths, List<Rectangle> rectGeom)
        {
            DistanceMult = 5000f;
            var pointLight = light.LightPos;
            var mat_cw = Matrix.CreateRotationZ(WallPenumbra);
            var mat_ccw = Matrix.CreateRotationZ(-WallPenumbra);
            var vertices = new List<GradVertex>();
            var indices = new List<int>();
            var j = 0;
            var basicDesc = new EllipseDesc();
            var hc = Color.White * 0.5f;
            var p = WallPenumbra * 2;
            foreach (var pts in volumes)
            {
                //find sides closest to point
                float distM;
                if (light.LightType == LightType.OUTDOORS && ctrWidths == null)
                {
                    distM = 32f * light.FalloffMultiplier;
                }
                else distM = DistanceMult;
                var baseIdx = vertices.Count;
                //var pts = ClockwiseLine(wall, pointLight);
                //var pts = ClosestPtsClockwise(wall, pointLight);

                //through project the points on each side with angle offsets.

                var mid = (pts[0] + pts[2]) / 2;

                Vector2 leftNorm, midNorm, rightNorm, leftFac, midFac, rightFac;
                if (light.LightType == LightType.OUTDOORS)
                {
                    leftNorm = midNorm = rightNorm = light.LightDir;
                } else { 
                    leftNorm = pts[0] - pointLight; leftNorm.Normalize();
                    midNorm = mid - pointLight; midNorm.Normalize();
                    rightNorm = pts[2] - pointLight; rightNorm.Normalize();
                }

                leftFac = leftNorm* distM; rightFac = rightNorm* distM; midFac = midNorm * distM;

                EllipseDesc ellipse;
                if (ctrWidths != null)
                {
                    //distance * obj2height / (lightheight - obj2height)
                    var ctW = ctrWidths[j++];
                    var mid2 = new Vector2(ctW.X, ctW.Y);
                    float height;
                    if (light.LightType == LightType.OUTDOORS) height = 16*light.FalloffMultiplier;
                    else height = (mid2 - pointLight).Length() * 16 / ((16 * 3) - 16);
                    var midNorm2 = mid2 - pointLight; midNorm2.Normalize();
                    var largeDim = (ctW.Z + height) * midNorm2;
                    var smallDim = new Vector2(largeDim.Y, -largeDim.X);
                    smallDim.Normalize();
                    smallDim *= ctW.Z;
                    ellipse = new EllipseDesc { pos = mid2, dimensions = new Vector4(smallDim, largeDim.X, largeDim.Y) };
                }
                else if (light.LightType == LightType.OUTDOORS)
                {
                    //wall linear falloff
                    var perp = (pts[2] - pts[0]);
                    var px = perp.Y;
                    perp.Y = -perp.X;
                    perp.X = px;
                    perp.Normalize();

                    var midN2 = light.LightDir;

                    var dot = Vector2.Dot(perp, midN2);
//if (Math.Abs(dot) < 0.35) continue;
                    var length = distM * (dot);
                    var spos = pts[0];// - ((dot>0)?perp:(-perp))*2;
                    perp *= length;
                    ellipse = new EllipseDesc { pos = spos, dimensions = new Vector4(length*length, 0, perp.X, perp.Y) };
                }
                else ellipse = basicDesc;

                //rotate the left and right norms to make the new penumbras
                var leftpen1 = pts[0] + Vector2.Transform(leftFac, mat_ccw);
                var leftpen2 = pts[0] + Vector2.Transform(leftFac, mat_cw);

                var rightpen1 = pts[2] + Vector2.Transform(rightFac, mat_ccw);
                var rightpen2 = pts[2] + Vector2.Transform(rightFac, mat_cw);


                //------ filled points ------

                //three cases. left and right penubras intersect at some point, in which case we make 2 triangles.
                //if they diverge, make 4 triangles.
                //penumbras contain each other. use one and center at half the target color.

                //y = mx + c

                var dist1 = (pts[0] - pointLight).LengthSquared();
                var dist2 = (pts[2] - pointLight).LengthSquared();
                if (dist1 > dist2)
                {
                    //is penumbra 1 inside 2?
                    if (Vector2.Dot(Vector2.Normalize(pts[0] - pts[2]), rightNorm) > PenCos)
                    {
                        //yes
                        //continue;
                        var conectr = pts[2] + rightNorm;
                        vertices.Add(GradVertex.ConeVert(pts[2], pts[2], conectr, Color.TransparentBlack, hc, p/2, ellipse));
                        vertices.Add(GradVertex.ConeVert(rightpen1, pts[2], conectr, Color.TransparentBlack, hc, p/2, ellipse));
                        vertices.Add(GradVertex.ConeVert(rightpen2, pts[2], conectr, Color.TransparentBlack, hc, p/2, ellipse));
                        for (int i = 0; i < 3; i++) indices.Add(baseIdx + i);
                        continue;
                    }
                }
                else
                {
                    //is penumbra 2 inside 1?
                    if (Vector2.Dot(Vector2.Normalize(pts[2] - pts[0]), leftNorm) > PenCos)
                    {
                        //yes
                        //continue;
                        var conectr = pts[0] + leftNorm;
                        vertices.Add(GradVertex.ConeVert(pts[0], pts[0], conectr, Color.TransparentBlack, hc, p, ellipse));
                        vertices.Add(GradVertex.ConeVert(leftpen1, pts[0], conectr, Color.TransparentBlack, hc, p, ellipse));
                        vertices.Add(GradVertex.ConeVert(leftpen2, pts[0], conectr, Color.TransparentBlack, hc, p, ellipse));
                        for (int i = 0; i < 3; i++) indices.Add(baseIdx + i);
                        continue;
                    }
                }

                var midBack = pts[1] + midFac;
                //continue;
                var a = leftpen2 - pts[0];
                var b = (rightpen1 - pts[2]);
                var negX = -b.X;
                b.X = b.Y;
                b.Y = negX;
                var c = pts[2] - pts[0];
                var t = Vector2.Dot(c, b) / Vector2.Dot(a, b);

                if (t < 0)
                {
                    vertices.Add(GradVertex.SolidVert(pts[0], Color.White, ellipse));
                    vertices.Add(GradVertex.SolidVert(pts[1], Color.White, ellipse));
                    vertices.Add(GradVertex.SolidVert(pts[2], Color.White, ellipse));

                    vertices.Add(GradVertex.SolidVert(leftpen2, Color.White, ellipse));
                    vertices.Add(GradVertex.SolidVert(midBack, Color.White, ellipse));
                    vertices.Add(GradVertex.SolidVert(rightpen1, Color.White, ellipse));

                    indices.Add(baseIdx); indices.Add(baseIdx + 2); indices.Add(baseIdx + 1); //half of rectangle
                    indices.Add(baseIdx); indices.Add(baseIdx + 3); indices.Add(baseIdx + 2); //extension 1
                    indices.Add(baseIdx + 3); indices.Add(baseIdx + 4); indices.Add(baseIdx + 2); //extension 2
                    indices.Add(baseIdx + 4); indices.Add(baseIdx + 5); indices.Add(baseIdx + 2); //extension 3
                    baseIdx += 6;

                    //penumbras

                    vertices.Add(GradVertex.ConeVert(pts[0], pts[0], leftpen2, Color.TransparentBlack, Color.White, p, ellipse));
                    vertices.Add(GradVertex.ConeVert(leftpen1, pts[0], leftpen2, Color.TransparentBlack, Color.White, p, ellipse));
                    vertices.Add(GradVertex.ConeVert(leftpen2, pts[0], leftpen2, Color.TransparentBlack, Color.White, p, ellipse));

                    vertices.Add(GradVertex.ConeVert(pts[2], pts[2], rightpen1, Color.TransparentBlack, Color.White, p, ellipse));
                    vertices.Add(GradVertex.ConeVert(rightpen1, pts[2], rightpen1, Color.TransparentBlack, Color.White, p, ellipse));
                    vertices.Add(GradVertex.ConeVert(rightpen2, pts[2], rightpen1, Color.TransparentBlack, Color.White, p, ellipse));

                    for (int i = 0; i < 6; i++) indices.Add(baseIdx + i);
                }
                else
                {
                    var inter = pts[0] + a * t;
                    var distant = mid + Vector2.Normalize(inter - pointLight) * distM;
                    vertices.Add(GradVertex.SolidVert(pts[0], Color.White, ellipse));
                    vertices.Add(GradVertex.SolidVert(pts[0] + a*t, Color.White, ellipse));
                    vertices.Add(GradVertex.SolidVert(pts[2], Color.White, ellipse));
                    vertices.Add(GradVertex.SolidVert(pts[1], Color.White, ellipse));

                    indices.Add(baseIdx); indices.Add(baseIdx+1); indices.Add(baseIdx+2);
                    indices.Add(baseIdx); indices.Add(baseIdx + 2); indices.Add(baseIdx + 3); //half of rectangle

                    baseIdx += 4;

                    //penumbras: each penumbra becomes two tris as they intersect

                    vertices.Add(GradVertex.ConeVert(pts[0], pts[0], leftpen2, Color.TransparentBlack, Color.White, p, ellipse));
                    vertices.Add(GradVertex.ConeVert(leftpen1, pts[0], leftpen2, Color.TransparentBlack, Color.White, p, ellipse));
                    vertices.Add(GradVertex.ConeVert(inter, pts[0], leftpen2, Color.TransparentBlack, Color.White, p, ellipse));
                    vertices.Add(GradVertex.ConeVert(distant, pts[0], leftpen2, Color.TransparentBlack, Color.White, p, ellipse));

                    vertices.Add(GradVertex.ConeVert(pts[2], pts[2], rightpen1, Color.TransparentBlack, Color.White, p, ellipse));
                    vertices.Add(GradVertex.ConeVert(rightpen2, pts[2], rightpen1, Color.TransparentBlack, Color.White, p, ellipse));
                    vertices.Add(GradVertex.ConeVert(inter, pts[2], rightpen1, Color.TransparentBlack, Color.White, p, ellipse));
                    vertices.Add(GradVertex.ConeVert(distant, pts[2], rightpen1, Color.TransparentBlack, Color.White, p, ellipse));

                    indices.Add(baseIdx); indices.Add(baseIdx+1); indices.Add(baseIdx+2);
                    indices.Add(baseIdx+1); indices.Add(baseIdx + 3); indices.Add(baseIdx + 2);

                    baseIdx += 4;
                    indices.Add(baseIdx); indices.Add(baseIdx + 1); indices.Add(baseIdx + 2);
                    indices.Add(baseIdx + 1); indices.Add(baseIdx + 3); indices.Add(baseIdx + 2);
                }



            }

            return new Tuple<GradVertex[], int[]>(vertices.ToArray(), indices.ToArray());
        }

        private static Vector2[] ClockwiseLine(Vector2[] line, Vector2 point)
        {
            var det = (line[0].X - point.X) * (line[1].Y - point.Y) - (line[1].X - point.X) * (line[0].Y - point.Y);

            if (det > 0) return new Vector2[] { line[0], line[1], line[1] };
            else return new Vector2[] { line[1], line[0], line[0] };
        }

        private static Vector2[] ClosestPtsClockwise(Rectangle rect, Vector2 point)
        {
            var pts = new List<Tuple<float, int>>();
            float bestDir = 0f;
            int bestInd = 0;
            float bestDirO = 0f;
            int bestIndO = 0;
            var ccwPts = new Vector2[4];

            var pt1 = new Vector2(rect.Left, rect.Top);
            var diff = pt1 - point;
            var dir1 = Math.Atan2(diff.Y, diff.X);
            ccwPts[0] = pt1;

            var pt = new Vector2(rect.Left, rect.Bottom);
            diff = pt - point;
            ccwPts[1] = pt;
            var dir = (float)DirectionUtils.Difference(Math.Atan2(diff.Y, diff.X), dir1);
            if (dir > bestDir) { bestDir = dir; bestInd = 1; };
            if (dir < bestDirO) { bestDirO = dir; bestIndO = 1; };

            pt = new Vector2(rect.Right, rect.Bottom);
            diff = pt - point;
            ccwPts[2] = pt;
            dir = (float)DirectionUtils.Difference(Math.Atan2(diff.Y, diff.X), dir1);
            if (dir > bestDir) { bestDir = dir; bestInd = 2; };
            if (dir < bestDirO) { bestDirO = dir; bestIndO = 2; };

            pt = new Vector2(rect.Right, rect.Top);
            diff = pt - point;
            ccwPts[3] = pt;
            dir = (float)DirectionUtils.Difference(Math.Atan2(diff.Y, diff.X), dir1);
            if (dir > bestDir) { bestDir = dir; bestInd = 3; };
            if (dir < bestDirO) { bestDirO = dir; bestIndO = 3; };

            var result = new Vector2[3];
            for (int i=0; i<3; i++)
            {
                result[i] = ccwPts[bestInd];
                if (bestInd != bestIndO)
                    bestInd = (bestInd + 1) % 4;
            }
            return result;
        }
    }


}
