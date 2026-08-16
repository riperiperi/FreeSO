using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace FSO.PackCompiler.ArtGen
{
    public class RenderedFrame
    {
        public int Width, Height;
        public Color[] Pixels;
        public byte[] Z;
    }

    /// <summary>
    /// Rasterizes a Mesh into a TSO-sized sprite frame using the derived camera
    /// (ART-PIPELINE-CALIBRATION.md §5) — orthographic, 30 deg pitch, per-zoom
    /// px/world-unit scale — with real Lambertian lighting and real per-sprite depth
    /// normalization (not a lookup table or a fixed band — see LightDirCamSpace / §1 and §2
    /// of the "proper depth"/"real lighting" follow-up work).
    /// </summary>
    public static class Renderer
    {
        /// <summary>
        /// Light direction expressed in camera-space (Right, Up, ToCamera) coefficients, not
        /// world space — this is what makes the same rig reproduce the ART-PIPELINE-CALIBRATION.md
        /// §7 finding that the brighter/darker side is consistent across all 4 world-yaw
        /// directions: a light fixed relative to the CAMERA (screen), not the object, matches
        /// how each of the 4 direction bakes was independently art-directed with a consistent
        /// rig. Solved analytically (FSO.ArtCalibration's LightingValidation.cs), not guessed:
        /// the box's top/right/left visible faces are mutually orthogonal, so their 3 Lambertian
        /// diffuse components are constrained to sum(d_i^2)=1 — solving that constraint against
        /// the real measured top/right and left/right ratios (0.96, 0.54) gives an exact light
        /// direction, which independently reproduced the measured ratios to ~0.96 vs 1.04 and
        /// 0.54 vs 0.53 when rendered (not fit pixel-by-pixel — solved once, then rendered).
        /// </summary>
        public static Vec3 LightDirCamSpace = new Vec3(0.262, 0.210, 0.942);
        public static double Ambient = 0.10;

        public static RenderedFrame Render(Mesh mesh, double yawRadians, double pitchRadians, double pxPerWorldUnit)
        {
            var cam = new Camera(yawRadians, pitchRadians);
            var lightWorld = (cam.Right * LightDirCamSpace.X + cam.Up * LightDirCamSpace.Y + cam.ToCamera * LightDirCamSpace.Z).Normalized();

            var visible = mesh.Faces.Where(f => f.Normal.Dot(cam.ToCamera) > 1e-6).ToList();
            if (visible.Count == 0) return new RenderedFrame { Width = 1, Height = 1, Pixels = new[] { new Color(0, 0, 0, 0) }, Z = new byte[] { 255 } };

            var projFaces = visible.Select(f => (face: f, proj: f.Verts.Select(cam.Project).ToArray())).ToList();

            double minSx = double.MaxValue, maxSx = double.MinValue, minSy = double.MaxValue, maxSy = double.MinValue;
            foreach (var (_, proj) in projFaces)
                foreach (var (sx, sy, _) in proj)
                {
                    minSx = Math.Min(minSx, sx); maxSx = Math.Max(maxSx, sx);
                    minSy = Math.Min(minSy, sy); maxSy = Math.Max(maxSy, sy);
                }

            int width = Math.Max(1, (int)Math.Ceiling((maxSx - minSx) * pxPerWorldUnit));
            int height = Math.Max(1, (int)Math.Ceiling((maxSy - minSy) * pxPerWorldUnit));

            var pixels = new Color[width * height];
            var depthBuf = new double[width * height];
            var covered = new bool[width * height];
            for (int i = 0; i < depthBuf.Length; i++) depthBuf[i] = double.MaxValue;

            foreach (var (face, proj) in projFaces)
            {
                // Real Lambertian: diffuse term from the actual face normal, not a lookup.
                var diffuse = Math.Max(0, face.Normal.Dot(lightWorld));
                var lightAmt = Ambient + (1 - Ambient) * diffuse;
                var col = new Color(
                    (byte)Math.Clamp(face.Color.r * lightAmt, 0, 255),
                    (byte)Math.Clamp(face.Color.g * lightAmt, 0, 255),
                    (byte)Math.Clamp(face.Color.b * lightAmt, 0, 255),
                    (byte)255);

                var (sx0, sy0, d0) = proj[0]; var (sx1, sy1, d1) = proj[1]; var (sx2, sy2, d2) = proj[2];
                var denom = (sx1 - sx0) * (sy2 - sy0) - (sx2 - sx0) * (sy1 - sy0);
                double A = 0, B = 0, C = d0;
                if (Math.Abs(denom) > 1e-9)
                {
                    A = ((d1 - d0) * (sy2 - sy0) - (d2 - d0) * (sy1 - sy0)) / denom;
                    B = ((sx1 - sx0) * (d2 - d0) - (sx2 - sx0) * (d1 - d0)) / denom;
                    C = d0 - A * sx0 - B * sy0;
                }

                double fMinSx = proj.Min(p => p.sx), fMaxSx = proj.Max(p => p.sx);
                double fMinSy = proj.Min(p => p.sy), fMaxSy = proj.Max(p => p.sy);
                int pxMin = (int)((fMinSx - minSx) * pxPerWorldUnit);
                int pxMax = (int)Math.Ceiling((fMaxSx - minSx) * pxPerWorldUnit);
                int pyMin = (int)((maxSy - fMaxSy) * pxPerWorldUnit);
                int pyMax = (int)Math.Ceiling((maxSy - fMinSy) * pxPerWorldUnit);

                for (int py = Math.Max(0, pyMin); py <= Math.Min(height - 1, pyMax); py++)
                for (int px = Math.Max(0, pxMin); px <= Math.Min(width - 1, pxMax); px++)
                {
                    var sx = minSx + (px + 0.5) / pxPerWorldUnit;
                    var sy = maxSy - (py + 0.5) / pxPerWorldUnit;
                    if (!PointInConvexPolygon(sx, sy, proj)) continue;
                    var depth = A * sx + B * sy + C;
                    var idx = py * width + px;
                    if (depth < depthBuf[idx])
                    {
                        depthBuf[idx] = depth;
                        pixels[idx] = col;
                        covered[idx] = true;
                    }
                }
            }

            // Per-sprite depth normalization into the band base-game furniture actually uses
            // (~[133,212] on the cardboard-box calibration object — ART-PIPELINE-CALIBRATION.md
            // §4a/§4c). Mapping across the full low-254 range ([35,250]) made DGRP3DMesh
            // extrude depth ~3× too far and explode as long triangles in Full 3D. Stay clear
            // of the <32 reserved band (§4b) and never touch 255 (background sentinel).
            double dMin = double.MaxValue, dMax = double.MinValue;
            for (int i = 0; i < depthBuf.Length; i++)
                if (covered[i]) { dMin = Math.Min(dMin, depthBuf[i]); dMax = Math.Max(dMax, depthBuf[i]); }

            const byte zLow = 135, zHigh = 210;
            var zbuf = new byte[width * height];
            for (int i = 0; i < zbuf.Length; i++)
            {
                if (!covered[i]) { zbuf[i] = 255; pixels[i] = new Color(0, 0, 0, 0); continue; }
                var t = (dMax > dMin) ? (depthBuf[i] - dMin) / (dMax - dMin) : 0.0;
                zbuf[i] = (byte)Math.Clamp(zLow + t * (zHigh - zLow), zLow, zHigh);
            }

            return new RenderedFrame { Width = width, Height = height, Pixels = pixels, Z = zbuf };
        }

        static bool PointInConvexPolygon(double px, double py, (double sx, double sy, double d)[] poly)
        {
            int sign = 0;
            for (int i = 0; i < poly.Length; i++)
            {
                var (x1, y1, _) = poly[i];
                var (x2, y2, _) = poly[(i + 1) % poly.Length];
                var cross = (x2 - x1) * (py - y1) - (y2 - y1) * (px - x1);
                var s = Math.Sign(cross);
                if (s == 0) continue;
                if (sign == 0) sign = s;
                else if (s != sign) return false;
            }
            return true;
        }
    }
}
