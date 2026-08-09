using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using FSO.PackCompiler;
using FSO.PackCompiler.ArtGen;

namespace FSO.ArtCalibration
{
    /// <summary>
    /// Validates Renderer's Lambertian lighting model against the real measured box ratios
    /// from ART-PIPELINE-CALIBRATION.md §7 (top~176, right~179, left~101, i.e. top/right~0.98,
    /// left/right~0.57) — the test is whether those ratios emerge from real per-face dot
    /// products against a tuned light direction, not whether they were typed in.
    /// </summary>
    public static class LightingValidation
    {
        public static void Run(string gameDir, string realGuidHex)
        {
            var realGuid = Convert.ToUInt32(realGuidHex, 16);
            var target = new IffFile();
            var d = new Diagnostics();
            var cloneResult = AppearanceCloner.Clone(realGuid, gameDir, target, d, "lighting-validation");
            if (!cloneResult.Ok) throw new Exception("could not load reference object");
            var realDgrp = target.List<DGRP>().First(x => x.ChunkID == cloneResult.BaseGraphicID);
            var image = realDgrp.Images.First(i => i.Direction == 0x04 && i.Zoom == 3); // RightFront, Near
            var realSprite = image.Sprites[0];
            var realSpr2 = target.Get<SPR2>((ushort)realSprite.SpriteID);
            var realFrame = realSpr2.Frames[realSprite.SpriteFrameIndex];
            realFrame.DecodeIfRequired(false);

            var (realTop, realLeft, realRight) = MeasureFaceLuminance(realFrame.PixelData, realFrame.Width, realFrame.Height);
            Console.WriteLine($"REAL  (RightFront/Near): top={realTop:F1} left={realLeft:F1} right={realRight:F1} | top/right={(realTop/realRight):F2} left/right={(realLeft/realRight):F2}");

            var mesh = new Mesh();
            mesh.AddBox(new Vec3(0, 1.2, 0), new Vec3(2.4, 2.4, 2.4), (196, 150, 96));
            var yaw = 45.0 * Math.PI / 180.0;

            var targetTopRight = realTop / realRight;
            var targetLeftRight = realLeft / realRight;

            // Analytical solve: top/right/left face normals are mutually orthogonal (a box
            // corner), so a pure Lambertian light's 3 diffuse components are constrained by
            // sum(diffuse_i^2) = 1. With a small fixed ambient A, solve for the exact
            // right-face lightAmt R that makes (top,right,left) hit the measured ratios while
            // satisfying that constraint, then back out the world-space light direction from
            // the box's actual face normals (not hand-derived — read from the mesh at
            // runtime to avoid a hand-algebra sign error).
            const double A = 0.10;
            var k = 1.0 / (1.0 - A);
            // (R-A)^2 + (T*R-A)^2 + (Le*R-A)^2 = 1/k^2, solve quadratic in R.
            var T = targetTopRight; var Le = targetLeftRight;
            var aCoef = 1 + T * T + Le * Le;
            var bCoef = -2 * A * (1 + T + Le);
            var cCoef = 3 * A * A - 1.0 / (k * k);
            var disc = bCoef * bCoef - 4 * aCoef * cCoef;
            var R = (-bCoef + Math.Sqrt(disc)) / (2 * aCoef);
            var diffuseRight = k * (R - A);
            var diffuseTop = k * (T * R - A);
            var diffuseLeft = k * (Le * R - A);
            Console.WriteLine($"Analytical solve: R={R:F4} diffuseTop={diffuseTop:F4} diffuseRight={diffuseRight:F4} diffuseLeft={diffuseLeft:F4} " +
                $"(unit check: {diffuseTop*diffuseTop+diffuseRight*diffuseRight+diffuseLeft*diffuseLeft:F4} should be ~1)");

            // Read the actual face normals the renderer will use at this yaw, classified the
            // same way Renderer does (mean screen-X), so the sign/axis mapping is exact.
            var cam = new Camera(yaw, Camera.Pitch);
            var visibleFaces = mesh.Faces.Where(f => f.Normal.Dot(cam.ToCamera) > 1e-6).ToList();
            var topFace = visibleFaces.First(f => Math.Abs(f.Normal.Y) > 0.9);
            var sideFaces = visibleFaces.Where(f => Math.Abs(f.Normal.Y) < 0.5).ToList();
            var s0MeanSx = s0MeanScreenX(sideFaces[0], cam);
            var s1MeanSx = sideFaces.Count > 1 ? s0MeanScreenX(sideFaces[1], cam) : 0;
            var rightFace = s0MeanSx > s1MeanSx ? sideFaces[0] : sideFaces[1];
            var leftFace = s0MeanSx > s1MeanSx ? sideFaces[1] : sideFaces[0];

            var lightWorld = (topFace.Normal * diffuseTop + rightFace.Normal * diffuseRight + leftFace.Normal * diffuseLeft).Normalized();
            var p = lightWorld.Dot(cam.Right);
            var q = lightWorld.Dot(cam.Up);
            var r = lightWorld.Dot(cam.ToCamera);

            Renderer.LightDirCamSpace = new Vec3(p, q, r);
            Renderer.Ambient = A;
            var rendered = Renderer.Render(mesh, yaw, Camera.Pitch, 29.93);
            Console.WriteLine($"SOLVED: LightDirCamSpace=({p:F3},{q:F3},{r:F3}) Ambient={A:F2}");

            // Exact per-face measurement: the render is flat-shaded with exactly one distinct
            // RGB per face, so group pixels by exact color rather than the crude top/left/right
            // positional heuristic (that heuristic is a necessary approximation for the REAL
            // sprite, which has no per-face ground truth to key off — here we do, so use it).
            var byColor = new Dictionary<(byte, byte, byte), int>();
            foreach (var px in rendered.Pixels)
            {
                if (px.A == 0) continue;
                var key = (px.R, px.G, px.B);
                byColor[key] = byColor.GetValueOrDefault(key) + 1;
            }
            Console.WriteLine("Distinct face colors in render (should be 3, sorted by luminance):");
            foreach (var kv in byColor.OrderByDescending(kv => 0.299 * kv.Key.Item1 + 0.587 * kv.Key.Item2 + 0.114 * kv.Key.Item3))
            {
                var lum = 0.299 * kv.Key.Item1 + 0.587 * kv.Key.Item2 + 0.114 * kv.Key.Item3;
                Console.WriteLine($"  RGB={kv.Key} luminance={lum:F1} pixelCount={kv.Value}");
            }
            var lums = byColor.Keys.Select(c => 0.299 * c.Item1 + 0.587 * c.Item2 + 0.114 * c.Item3).OrderByDescending(x => x).ToArray();
            if (lums.Length == 3)
                Console.WriteLine($"top/right~={lums[0]/lums[1]:F2} (target {targetTopRight:F2})  darkest/brightest~={lums[2]/lums[0]:F2} (target left/right {targetLeftRight:F2})");
        }

        static double s0MeanScreenX(Face f, Camera cam) => f.Verts.Select(cam.Project).Average(p => p.sx);

        static (double top, double left, double right) MeasureFaceLuminance(Microsoft.Xna.Framework.Color[] pixels, int width, int height)
        {
            int xmin = width, xmax = -1, ymin = height, ymax = -1;
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                var px = pixels[y * width + x];
                if (px.A == 0) continue;
                if (x < xmin) xmin = x; if (x > xmax) xmax = x;
                if (y < ymin) ymin = y; if (y > ymax) ymax = y;
            }
            var h = ymax - ymin + 1;
            var xmid = (xmin + xmax) / 2;
            var topBoundary = ymin + h / 3;

            double topSum = 0, topN = 0, leftSum = 0, leftN = 0, rightSum = 0, rightN = 0;
            for (int y = ymin; y <= ymax; y++)
            for (int x = xmin; x <= xmax; x++)
            {
                var px = pixels[y * width + x];
                if (px.A == 0) continue;
                var lum = 0.299 * px.R + 0.587 * px.G + 0.114 * px.B;
                if (y < topBoundary) { topSum += lum; topN++; }
                else if (x < xmid) { leftSum += lum; leftN++; }
                else { rightSum += lum; rightN++; }
            }
            return (topSum / Math.Max(1, topN), leftSum / Math.Max(1, leftN), rightSum / Math.Max(1, rightN));
        }
    }
}
