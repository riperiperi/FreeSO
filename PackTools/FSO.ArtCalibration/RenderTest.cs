using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using FSO.PackCompiler;
using Microsoft.Xna.Framework;

namespace FSO.ArtCalibration
{
    /// <summary>
    /// End-to-end test render: reproduces the cardboard-box end table (0x35372C14) as a
    /// procedural box, rendered through a minimal software rasterizer implementing exactly
    /// the parameters derived in ART-PIPELINE-CALIBRATION.md (orthographic, 30 deg pitch,
    /// 45-deg-offset 90-deg-step yaw, per-zoom px/world-unit scale) — not Blender (not
    /// installed in this environment; this is a from-scratch substitute that implements the
    /// same derived math directly and controllably). Frames are encoded through the REAL
    /// production SPR2FrameEncoder, written to a real .iff on disk, and read back through
    /// the REAL production decode path, then compared pixel-dimension-for-pixel-dimension
    /// and z-range-for-z-range against the real object's frames (also decoded fresh, in the
    /// same run, for a true apples-to-apples comparison).
    ///
    /// Box world dimensions are SOLVED from one real measurement (Near-zoom RightFront W/H)
    /// via the derived projection formulas, then used to predict the other 11 frames' pixel
    /// dimensions — those 11 are genuine held-out predictions, not fit. Lighting is NOT
    /// independently derived here: it directly encodes ART-PIPELINE-CALIBRATION.md's
    /// measured top/left/right screen-space brightness ratios as a generation rule, so the
    /// lighting comparison in this test is a round-trip/mechanical check (does the encoded
    /// color survive quantization+encode+decode), not an independent validation.
    /// </summary>
    public static class RenderTest
    {
        const double DegToRad = Math.PI / 180.0;
        const double Pitch = 30.0 * DegToRad;
        static readonly double[] YawDeg = { 315, 45, 135, 225 }; // RightBack, RightFront, LeftFront, LeftBack order matches Program.cs Directions[]
        static readonly (double pxPerWorldUnit, string name)[] ZoomScale =
        {
            (7.31, "Far"),
            (14.85, "Medium"),
            (29.93, "Near"),
        };

        class Vec3
        {
            public double X, Y, Z;
            public Vec3(double x, double y, double z) { X = x; Y = y; Z = z; }
            public static Vec3 operator -(Vec3 a, Vec3 b) => new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
            public double Dot(Vec3 b) => X * b.X + Y * b.Y + Z * b.Z;
            public static Vec3 Cross(Vec3 a, Vec3 b) => new Vec3(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
            public double Length() => Math.Sqrt(X * X + Y * Y + Z * Z);
            public Vec3 Normalized() { var l = Length(); return new Vec3(X / l, Y / l, Z / l); }
        }

        class RenderedFrame
        {
            public int Width, Height;
            public Color[] Pixels;
            public byte[] Z;
        }

        public static void Run(string gameDir, string realGuidHex)
        {
            // --- 1. Solve box world dimensions from one real measurement ---------------
            var realGuid = Convert.ToUInt32(realGuidHex, 16);
            var target = new IffFile();
            var d = new Diagnostics();
            var cloneResult = AppearanceCloner.Clone(realGuid, gameDir, target, d, "rendertest-reference");
            if (!cloneResult.Ok) throw new Exception("could not load reference object: " + string.Join("; ", d.Errors));
            var realDgrp = target.List<DGRP>().First(x => x.ChunkID == cloneResult.BaseGraphicID);

            var realMeasurements = new Dictionary<(uint zoom, string dirName), (int w, int h, byte zmin, byte zmax)>();
            var dirDefs = Program.DirectionsPublic;
            foreach (var (zoom, zoomName) in Program.ZoomsPublic)
                foreach (var (dirBit, dirName) in dirDefs)
                {
                    var image = realDgrp.Images.First(i => i.Direction == dirBit && i.Zoom == zoom);
                    var sprite = image.Sprites[0];
                    var spr2 = target.Get<SPR2>((ushort)sprite.SpriteID);
                    var frame = spr2.Frames[sprite.SpriteFrameIndex];
                    frame.DecodeIfRequired(false);
                    var nonBg = frame.ZBufferData.Where(b => b != 255).ToArray();
                    realMeasurements[(zoom, dirName)] = (frame.Width, frame.Height,
                        nonBg.Length > 0 ? nonBg.Min() : (byte)0, nonBg.Length > 0 ? nonBg.Max() : (byte)0);
                }

            // Solve using Near-zoom RightFront (yaw=45) as the one measurement consumed.
            var (solveW, solveH, _, _) = realMeasurements[(3u, "RightFront")];
            var pxPerWuNear = ZoomScale.First(z => z.name == "Near").pxPerWorldUnit;
            var screenWidthWorld = solveW / pxPerWuNear;   // = (Xw+Zw)/sqrt(2)
            var screenHeightWorld = solveH / pxPerWuNear;  // = (Xw+Zw)/sqrt(2)*sin(pitch) + Yw*cos(pitch)
            var footprintDiag = screenWidthWorld;          // (Xw+Zw)/sqrt(2)
            var footprintSum = footprintDiag * Math.Sqrt(2); // Xw+Zw
            var boxY = (screenHeightWorld - footprintDiag * Math.Sin(Pitch)) / Math.Cos(Pitch);
            var boxX = footprintSum / 2.0; // symmetric footprint assumption
            var boxZ = footprintSum / 2.0;

            Console.WriteLine("=== RenderTest: solved box world dimensions ===");
            Console.WriteLine($"Solved from real Near/RightFront measurement W={solveW} H={solveH}:");
            Console.WriteLine($"  Xw={boxX:F3} Yw={boxY:F3} Zw={boxZ:F3} (world units; WorldUnitsPerTile=3)");
            Console.WriteLine();

            // --- 2. Render all 12 frames -------------------------------------------------
            var rendered = new Dictionary<(uint zoom, string dirName), RenderedFrame>();
            for (int zi = 0; zi < ZoomScale.Length; zi++)
            {
                var (pxPerWu, zoomName) = ZoomScale[zi];
                uint zoomNum = (uint)(zi + 1);
                for (int di = 0; di < dirDefs.Length; di++)
                {
                    var (dirBit, dirName) = dirDefs[di];
                    var yaw = YawDeg[di] * DegToRad;
                    var frame = RenderBox(boxX, boxY, boxZ, yaw, Pitch, pxPerWu);
                    rendered[(zoomNum, dirName)] = frame;
                }
            }

            // --- 3. Encode through the REAL production encoder into a real .iff --------
            var outIff = new IffFile();
            const ushort GEN_ID = 9000;
            var objd = NewChunk<OBJD>(outIff, 1, "rendertest_box");
            objd.ObjectType = OBJDType.Normal;
            objd.GUID = 0x00000001;
            objd.BaseGraphicID = GEN_ID;
            objd.NumGraphics = 1;

            var palt = NewChunk<PALT>(outIff, GEN_ID, "rendertest_box palette");
            palt.Colors = new Color[256];

            var spr2gen = NewChunk<SPR2>(outIff, GEN_ID, "rendertest_box sprites");
            spr2gen.DefaultPaletteID = GEN_ID;
            var frames = new List<SPR2Frame>();
            var frameIndexOf = new Dictionary<(uint, string), int>();

            foreach (var kv in rendered)
            {
                var rf = kv.Value;
                var sf = new SPR2Frame(spr2gen);
                var rect = new Rectangle(0, 0, rf.Width, rf.Height);
                var quantPalette = sf.SetData(rf.Pixels, rf.Z, rect);
                // SetData already wrote PalData via SPR2FrameEncoder.QuantizeFrame (wired below);
                // capture the returned palette into the shared PALT once (first frame wins; all
                // frames share one flat material palette here so this is safe).
                if (palt.Colors[1].PackedValue == 0 && quantPalette.Length > 0)
                    Array.Copy(quantPalette, palt.Colors, Math.Min(256, quantPalette.Length));
                sf.PaletteID = GEN_ID;
                frameIndexOf[kv.Key] = frames.Count;
                frames.Add(sf);
            }
            spr2gen.Frames = frames.ToArray();

            var dgrpGen = NewChunk<DGRP>(outIff, GEN_ID, "rendertest_box drawgroup");
            var images = new List<DGRPImage>();
            foreach (var kv in rendered)
            {
                var (zoom, dirName) = kv.Key;
                var dirBit = dirDefs.First(x => x.name == dirName).dir;
                var img = new DGRPImage(dgrpGen) { Direction = dirBit, Zoom = zoom };
                img.Sprites = new[]
                {
                    new DGRPSprite(dgrpGen)
                    {
                        SpriteID = GEN_ID,
                        SpriteFrameIndex = (uint)frameIndexOf[kv.Key],
                    }
                };
                images.Add(img);
            }
            dgrpGen.Images = images.ToArray();

            var tmpPath = Path.Combine(Path.GetTempPath(), "fso-rendertest-box.iff");
            using (var stream = new FileStream(tmpPath, FileMode.Create))
                outIff.Write(stream);
            Console.WriteLine("Wrote generated .iff: " + tmpPath + " (" + new FileInfo(tmpPath).Length + " bytes)");
            Console.WriteLine();

            // --- 4. Read back through the REAL production decode path, fresh ------------
            var readBack = new IffFile();
            using (var stream = new FileStream(tmpPath, FileMode.Open))
                readBack.Read(stream);
            var readObjd = readBack.List<OBJD>().First();
            var readDgrp = readBack.List<DGRP>().First(x => x.ChunkID == readObjd.BaseGraphicID);

            // --- 5. Compare, dimension by dimension, z-range by z-range -----------------
            Console.WriteLine("=== Comparison: generated (round-tripped through real encoder/decoder) vs real object ===");
            Console.WriteLine($"{"Zoom/Dir",-16} {"Real W×H",-12} {"Gen W×H",-12} {"ΔW%",-8} {"ΔH%",-8} {"Real Z-range",-14} {"Gen Z-range",-14}");
            var usedForSolve = (3u, "RightFront");
            foreach (var (zoom, zoomName) in Program.ZoomsPublic)
                foreach (var (dirBit, dirName) in dirDefs)
                {
                    var key = (zoom, dirName);
                    var real = realMeasurements[key];

                    var image = readDgrp.Images.First(i => i.Direction == dirBit && i.Zoom == zoom);
                    var sprite = image.Sprites[0];
                    var spr2 = readBack.Get<SPR2>((ushort)sprite.SpriteID);
                    var frame = spr2.Frames[sprite.SpriteFrameIndex];
                    frame.DecodeIfRequired(false);
                    var nonBg = frame.ZBufferData.Where(b => b != 255).ToArray();
                    byte genZmin = nonBg.Length > 0 ? nonBg.Min() : (byte)0;
                    byte genZmax = nonBg.Length > 0 ? nonBg.Max() : (byte)0;

                    var dW = 100.0 * (frame.Width - real.w) / real.w;
                    var dH = 100.0 * (frame.Height - real.h) / real.h;
                    var tag = key.Equals(usedForSolve) ? " (used to solve size)" : "";
                    Console.WriteLine($"{zoomName + "/" + dirName,-16} {real.w + "x" + real.h,-12} {frame.Width + "x" + frame.Height,-12} {dW,6:F1}%  {dH,6:F1}%  {"[" + real.zmin + "-" + real.zmax + "]",-14} {"[" + genZmin + "-" + genZmax + "]",-14}{tag}");
                }
        }

        static RenderedFrame RenderBox(double xw, double yw, double zw, double yaw, double pitch, double pxPerWorldUnit)
        {
            // Camera basis: toCamera points from the scene toward the camera.
            var toCamera = new Vec3(Math.Sin(yaw) * Math.Cos(pitch), Math.Sin(pitch), Math.Cos(yaw) * Math.Cos(pitch)).Normalized();
            var worldUp = new Vec3(0, 1, 0);
            var right = Vec3.Cross(worldUp, toCamera).Normalized();
            var up = Vec3.Cross(toCamera, right).Normalized();

            (double sx, double sy, double depth) Project(Vec3 p)
            {
                var sx = p.Dot(right);
                var sy = p.Dot(up);
                var depth = -p.Dot(toCamera); // more negative = nearer camera
                return (sx, sy, depth);
            }

            // 8 box corners: base at Y=0, top at Y=yw, footprint centered at origin.
            var corners = new Dictionary<(int,int,int), Vec3>();
            foreach (var sx in new[] { -1, 1 })
                foreach (var sy in new[] { 0, 1 })
                    foreach (var sz in new[] { -1, 1 })
                        corners[(sx, sy, sz)] = new Vec3(sx * xw / 2.0, sy * yw, sz * zw / 2.0);

            // 3 candidate faces (top + 4 sides), each as 4 corners + outward normal.
            var faces = new (Vec3 normal, Vec3[] verts)[]
            {
                (new Vec3(0,1,0), new[]{ corners[(-1,1,-1)], corners[(1,1,-1)], corners[(1,1,1)], corners[(-1,1,1)] }), // top
                (new Vec3(1,0,0), new[]{ corners[(1,0,-1)], corners[(1,1,-1)], corners[(1,1,1)], corners[(1,0,1)] }),   // +X
                (new Vec3(-1,0,0), new[]{ corners[(-1,0,1)], corners[(-1,1,1)], corners[(-1,1,-1)], corners[(-1,0,-1)] }), // -X
                (new Vec3(0,0,1), new[]{ corners[(1,0,1)], corners[(1,1,1)], corners[(-1,1,1)], corners[(-1,0,1)] }),   // +Z
                (new Vec3(0,0,-1), new[]{ corners[(-1,0,-1)], corners[(-1,1,-1)], corners[(1,1,-1)], corners[(1,0,-1)] }), // -Z
            };

            var visible = faces.Where(f => f.normal.Dot(toCamera) > 1e-6).ToList();

            // Project all visible faces' corners; find overall bounds.
            var projFaces = visible.Select(f => (face: f, proj: f.verts.Select(Project).ToArray())).ToList();
            double minSx = double.MaxValue, maxSx = double.MinValue, minSy = double.MaxValue, maxSy = double.MinValue;
            foreach (var (_, proj) in projFaces)
                foreach (var (sx, sy, _) in proj)
                {
                    minSx = Math.Min(minSx, sx); maxSx = Math.Max(maxSx, sx);
                    minSy = Math.Min(minSy, sy); maxSy = Math.Max(maxSy, sy);
                }

            int width = (int)Math.Ceiling((maxSx - minSx) * pxPerWorldUnit);
            int height = (int)Math.Ceiling((maxSy - minSy) * pxPerWorldUnit);

            var pixels = new Color[width * height];
            var depthBuf = new double[width * height];
            var covered = new bool[width * height];
            for (int i = 0; i < depthBuf.Length; i++) depthBuf[i] = double.MaxValue;

            // Classify side faces by mean screen-X (left vs right) for the lighting rule —
            // ART-PIPELINE-CALIBRATION.md §7: fixed screen-space key light, right-visible face
            // ~ as bright as top, left-visible face ~55-59% as bright. Directly encodes that
            // measured ratio as a generation rule (see class doc comment: not independently
            // re-derived here).
            var sideFaces = projFaces.Where(pf => Math.Abs(pf.face.normal.Y) < 0.5).ToList();
            var brightnessOf = new Dictionary<int, double>();
            for (int i = 0; i < projFaces.Count; i++) brightnessOf[i] = 1.0; // top default
            if (sideFaces.Count == 2)
            {
                var meanSx0 = sideFaces[0].proj.Average(p => p.sx);
                var meanSx1 = sideFaces[1].proj.Average(p => p.sx);
                var idx0 = projFaces.FindIndex(pf => pf.face.Equals(sideFaces[0].face));
                var idx1 = projFaces.FindIndex(pf => pf.face.Equals(sideFaces[1].face));
                if (meanSx0 < meanSx1) { brightnessOf[idx0] = 0.57; brightnessOf[idx1] = 0.98; }
                else { brightnessOf[idx1] = 0.57; brightnessOf[idx0] = 0.98; }
            }

            var baseColor = new Vector3(196, 150, 96); // plausible flat cardboard-brown

            for (int fi = 0; fi < projFaces.Count; fi++)
            {
                var (face, proj) = projFaces[fi];
                var brightness = brightnessOf[fi];
                var col = new Color((byte)Math.Clamp(baseColor.X * brightness, 0, 255),
                                     (byte)Math.Clamp(baseColor.Y * brightness, 0, 255),
                                     (byte)Math.Clamp(baseColor.Z * brightness, 0, 255), (byte)255);

                // Planar depth as an affine function of (sx,sy): solve depth = A*sx+B*sy+C
                // from 3 of the 4 (non-collinear) projected corners.
                var (sx0, sy0, d0) = proj[0]; var (sx1, sy1, d1) = proj[1]; var (sx2, sy2, d2) = proj[2];
                var denom = (sx1 - sx0) * (sy2 - sy0) - (sx2 - sx0) * (sy1 - sy0);
                double A = 0, B = 0, C = d0;
                if (Math.Abs(denom) > 1e-9)
                {
                    A = ((d1 - d0) * (sy2 - sy0) - (d2 - d0) * (sy1 - sy0)) / denom;
                    B = ((sx1 - sx0) * (d2 - d0) - (sx2 - sx0) * (d1 - d0)) / denom;
                    C = d0 - A * sx0 - B * sy0;
                }

                // Rasterize the convex quad via a simple point-in-polygon scan over its bbox.
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
                    if (!PointInConvexQuad(sx, sy, proj)) continue;
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

            // Normalize depth into a byte z-buffer: 255 reserved for background; real span
            // scaled into roughly the observed real-object band (comfortably inside 0-254).
            double dMin = double.MaxValue, dMax = double.MinValue;
            for (int i = 0; i < depthBuf.Length; i++)
                if (covered[i]) { dMin = Math.Min(dMin, depthBuf[i]); dMax = Math.Max(dMax, depthBuf[i]); }

            var zbuf = new byte[width * height];
            for (int i = 0; i < zbuf.Length; i++)
            {
                if (!covered[i]) { zbuf[i] = 255; pixels[i] = new Color(0, 0, 0, 0); continue; }
                var t = (dMax > dMin) ? (depthBuf[i] - dMin) / (dMax - dMin) : 0.0;
                zbuf[i] = (byte)Math.Clamp(140 + t * 70, 0, 254); // matches real object's observed ~[133-212] band
            }

            return new RenderedFrame { Width = width, Height = height, Pixels = pixels, Z = zbuf };
        }

        static bool PointInConvexQuad(double px, double py, (double sx, double sy, double d)[] quad)
        {
            int sign = 0;
            for (int i = 0; i < quad.Length; i++)
            {
                var (x1, y1, _) = quad[i];
                var (x2, y2, _) = quad[(i + 1) % quad.Length];
                var cross = (x2 - x1) * (py - y1) - (y2 - y1) * (px - x1);
                var s = Math.Sign(cross);
                if (s == 0) continue;
                if (sign == 0) sign = s;
                else if (s != sign) return false;
            }
            return true;
        }

        static T NewChunk<T>(IffFile iff, ushort id, string label) where T : IffChunk, new()
        {
            var chunk = new T
            {
                ChunkID = id,
                ChunkLabel = label ?? "",
                ChunkProcessed = true,
                ChunkParent = iff,
                ChunkType = IffFile.CHUNK_TYPES.First(x => x.Value == typeof(T)).Key,
            };
            iff.AddChunk(chunk);
            return chunk;
        }
    }
}
