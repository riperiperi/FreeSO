using System;
using System.IO;
using System.Linq;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using FSO.PackCompiler;

namespace FSO.ArtCalibration
{
    /// <summary>
    /// One-shot measurement tool for ART-PIPELINE-DESIGN.md's calibration spike (§6 step 1).
    /// Loads a known base-game object's DGRP/SPR2 chunks via the same FAR-archive path
    /// AppearanceCloner uses, decodes all 12 frames, and prints exact measured pixel
    /// dimensions and z-buffer statistics per direction/zoom. Not a permanent part of the
    /// pipeline — a throwaway instrument, kept only as long as calibration needs re-checking
    /// against a different reference object.
    /// </summary>
    class Program
    {
        static readonly (uint dir, string name)[] Directions =
        {
            (0x01, "RightBack"),
            (0x04, "RightFront"),
            (0x10, "LeftFront"),
            (0x40, "LeftBack"),
        };

        static readonly (uint zoom, string name)[] Zooms =
        {
            (1, "Far"),
            (2, "Medium"),
            (3, "Near"),
        };

        public static (uint dir, string name)[] DirectionsPublic => Directions;
        public static (uint zoom, string name)[] ZoomsPublic => Zooms;

        static void Main(string[] args)
        {
            var defaultGameDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library/Application Support/The Sims Online/TSOClient");

            if (args.Length > 0 && args[0] == "rendertest")
            {
                var gameDirRt = args.Length > 1 ? args[1] : defaultGameDir;
                var guidRt = args.Length > 2 ? args[2] : "0x35372C14";
                if (!Directory.Exists(gameDirRt))
                    throw new Exception("TSO game content not found at " + gameDirRt);
                FSO.PackCompiler.ArtGen.SimpleQuantizer.Install();
                RenderTest.Run(gameDirRt, guidRt);
                return;
            }

            if (args.Length > 0 && args[0] == "installchair-build")
            {
                var gameDirIc = args.Length > 1 ? args[1] : defaultGameDir;
                if (!Directory.Exists(gameDirIc))
                    throw new Exception("TSO game content not found at " + gameDirIc);
                InstallChair.BuildChairIff(gameDirIc);
                return;
            }

            if (args.Length > 0 && args[0] == "installchair-catalog")
            {
                var gameDirIc = args.Length > 1 ? args[1] : defaultGameDir;
                if (!Directory.Exists(gameDirIc))
                    throw new Exception("TSO game content not found at " + gameDirIc);
                InstallChair.InstallCatalogEntry(gameDirIc);
                return;
            }

            if (args.Length > 0 && args[0] == "genchair")
            {
                var outDir = args.Length > 1 ? args[1] : Path.Combine(Path.GetTempPath(), "fso-genchair");
                FSO.PackCompiler.ArtGen.SimpleQuantizer.Install();
                ChairGenTest.Run(outDir);
                return;
            }

            if (args.Length > 0 && args[0] == "genfurniture")
            {
                var outDir = args.Length > 1 ? args[1] : Path.Combine(Path.GetTempPath(), "fso-genfurniture");
                FSO.PackCompiler.ArtGen.SimpleQuantizer.Install();
                FurnitureGenTest.Run(outDir);
                return;
            }

            if (args.Length > 0 && args[0] == "decodepackdir")
            {
                var packDir = args.Length > 1 ? args[1] : throw new Exception("usage: decodepackdir <packDir> [outDir]");
                var outDir = args.Length > 2 ? args[2] : Path.Combine(Path.GetTempPath(), "fso-decodepackdir");
                DecodePackDir.Run(packDir, outDir);
                return;
            }

            if (args.Length > 0 && args[0] == "genparts")
            {
                var outDir = args.Length > 1 ? args[1] : Path.Combine(Path.GetTempPath(), "fso-genparts");
                FSO.PackCompiler.ArtGen.SimpleQuantizer.Install();
                GenericPartsTest.Run(outDir);
                return;
            }

            if (args.Length > 0 && args[0] == "validatelighting")
            {
                var gameDirLv = args.Length > 1 ? args[1] : defaultGameDir;
                var guidLv = args.Length > 2 ? args[2] : "0x35372C14";
                if (!Directory.Exists(gameDirLv))
                    throw new Exception("TSO game content not found at " + gameDirLv);
                LightingValidation.Run(gameDirLv, guidLv);
                return;
            }

            var gameDir = args.Length > 0 ? args[0] : defaultGameDir;
            var guidHex = args.Length > 1 ? args[1] : "0x35372C14"; // "Table - End - Cardboard Box"

            if (!Directory.Exists(gameDir))
                throw new Exception("TSO game content not found at " + gameDir);

            var guid = Convert.ToUInt32(guidHex, 16);

            var target = new IffFile();
            var d = new Diagnostics();
            var result = AppearanceCloner.Clone(guid, gameDir, target, d, "calibration");

            Console.WriteLine("=== Object 0x" + guid.ToString("X8") + " ===");
            Console.WriteLine("Source file: " + result.SourceFile);
            Console.WriteLine("Ok: " + result.Ok);
            foreach (var err in d.Errors) Console.WriteLine("ERROR: " + err);
            if (!result.Ok) return;

            Console.WriteLine("BaseGraphicID: " + result.BaseGraphicID + "  NumGraphics: " + result.NumGraphics);
            Console.WriteLine("DrawGroupsCopied: " + result.DrawGroupsCopied + "  SpritesCopied: " + result.SpritesCopied + "  PalettesCopied: " + result.PalettesCopied);

            var dgrp = target.List<DGRP>()?.FirstOrDefault(x => x.ChunkID == result.BaseGraphicID);
            if (dgrp == null)
            {
                Console.WriteLine("No DGRP found at BaseGraphicID " + result.BaseGraphicID + " — listing all DGRP ids present:");
                foreach (var g in target.List<DGRP>() ?? new System.Collections.Generic.List<DGRP>())
                    Console.WriteLine("  DGRP chunk id " + g.ChunkID);
                return;
            }

            Console.WriteLine("DGRP images: " + dgrp.Images.Length + " (expect 12 = 4 directions x 3 zooms)");
            Console.WriteLine();

            foreach (var (zoom, zoomName) in Zooms)
            {
                foreach (var (dir, dirName) in Directions)
                {
                    var image = dgrp.Images.FirstOrDefault(i => i.Direction == dir && i.Zoom == zoom);
                    if (image == null)
                    {
                        Console.WriteLine($"[{zoomName,-6} {dirName,-10}] NO IMAGE (dir=0x{dir:X2} zoom={zoom})");
                        continue;
                    }
                    if (image.Sprites.Length == 0)
                    {
                        Console.WriteLine($"[{zoomName,-6} {dirName,-10}] image has 0 sprites");
                        continue;
                    }
                    foreach (var sprite in image.Sprites)
                    {
                        var spr2 = target.Get<SPR2>((ushort)sprite.SpriteID);
                        if (spr2 == null)
                        {
                            Console.WriteLine($"[{zoomName,-6} {dirName,-10}] sprite {sprite.SpriteID} -> no SPR2 chunk found");
                            continue;
                        }
                        var frame = spr2.Frames[sprite.SpriteFrameIndex];
                        // DecodeIfRequired(true) checks frame.Flags to decide whether a
                        // z-buffer decode is needed, but Flags itself isn't populated until
                        // decode happens — a chicken-and-egg check that no-ops on a frame
                        // that has never been decoded. DecodeIfRequired(false) unconditionally
                        // decodes on first call (its PixelData==null check), and the single
                        // underlying Decode() populates PixelData and ZBufferData together
                        // from the same on-disk Flags byte, so this still gets us the z-buffer.
                        frame.DecodeIfRequired(false);

                        var hasZ = (frame.Flags & 0x02) == 0x02;
                        string zStats = "no z-buffer (flag 0x02 not set)";
                        if (hasZ && frame.ZBufferData != null)
                        {
                            var z = frame.ZBufferData;
                            var nonBackground = z.Where(b => b != 255).ToArray();
                            var min = nonBackground.Length > 0 ? nonBackground.Min() : (byte)0;
                            var max = nonBackground.Length > 0 ? nonBackground.Max() : (byte)0;
                            var bgCount = z.Count(b => b == 255);
                            var lt32Count = z.Count(b => b < 32);
                            var zeroCount = z.Count(b => b == 0);
                            zStats = $"z-buffer: total={z.Length} bg(255)={bgCount} nonBgRange=[{min}-{max}] countZ<32={lt32Count} countZ==0={zeroCount}";
                        }

                        Console.WriteLine($"[{zoomName,-6} {dirName,-10}] sprite={sprite.SpriteID} frame={sprite.SpriteFrameIndex} " +
                            $"W={frame.Width} H={frame.Height} flags=0x{frame.Flags:X2} spriteOffset=({sprite.SpriteOffset.X},{sprite.SpriteOffset.Y}) objectOffset=({sprite.ObjectOffset.X},{sprite.ObjectOffset.Y},{sprite.ObjectOffset.Z}) framePos=({frame.Position.X},{frame.Position.Y})");
                        Console.WriteLine($"    {zStats}");

                        if (zoom == 3 && frame.PixelData != null) // Near zoom only — most pixels to sample
                            PrintLightingAnalysis(dirName, frame);
                    }
                }
            }
        }

        /// <summary>
        /// Crude face-brightness estimate for a box-like object: splits the non-transparent
        /// silhouette into top-third (top face) vs bottom-two-thirds left/right halves (the
        /// two visible side faces), by pixel position only — no real 3D segmentation, but
        /// "approximate is fine" for confirming generated art isn't lit obviously differently
        /// than base-game pieces.
        /// </summary>
        static void PrintLightingAnalysis(string dirName, FSO.Files.Formats.IFF.Chunks.SPR2Frame frame)
        {
            int xmin = frame.Width, xmax = -1, ymin = frame.Height, ymax = -1;
            for (int y = 0; y < frame.Height; y++)
            for (int x = 0; x < frame.Width; x++)
            {
                var px = frame.PixelData[y * frame.Width + x];
                if (px.A == 0) continue;
                if (x < xmin) xmin = x;
                if (x > xmax) xmax = x;
                if (y < ymin) ymin = y;
                if (y > ymax) ymax = y;
            }
            if (xmax < 0) { Console.WriteLine("    lighting: silhouette empty, skipped"); return; }

            var h = ymax - ymin + 1;
            var xmid = (xmin + xmax) / 2;
            var topBoundary = ymin + h / 3;

            double topSum = 0, topN = 0, leftSum = 0, leftN = 0, rightSum = 0, rightN = 0;
            for (int y = ymin; y <= ymax; y++)
            for (int x = xmin; x <= xmax; x++)
            {
                var px = frame.PixelData[y * frame.Width + x];
                if (px.A == 0) continue;
                var lum = 0.299 * px.R + 0.587 * px.G + 0.114 * px.B;
                if (y < topBoundary) { topSum += lum; topN++; }
                else if (x < xmid) { leftSum += lum; leftN++; }
                else { rightSum += lum; rightN++; }
            }

            var top = topN > 0 ? topSum / topN : double.NaN;
            var left = leftN > 0 ? leftSum / leftN : double.NaN;
            var right = rightN > 0 ? rightSum / rightN : double.NaN;
            Console.WriteLine($"    lighting ({dirName}): top={top:F1} (n={topN}) left={left:F1} (n={leftN}) right={right:F1} (n={rightN}) " +
                $"| top/left={(top / left):F2} top/right={(top / right):F2} left/right={(left / right):F2}");
        }
    }
}
