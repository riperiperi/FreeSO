using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using FSO.PackCompiler.ArtGen;

namespace FSO.ArtCalibration
{
    /// <summary>
    /// Exercises PartsGenerator on three of PackTools/examples/'s whimsical props — the exact
    /// category GENERIC-GENERATOR-DESIGN.md was written to unblock. Same discipline as
    /// ChairGenTest.cs/FurnitureGenTest.cs: real renderer, real SPR2FrameEncoder, real .iff
    /// round-tripped through fresh IffFile.Read, PNG dumps so output can be eyeballed.
    /// </summary>
    public static class GenericPartsTest
    {
        public static void Run(string outDir)
        {
            Directory.CreateDirectory(outDir);

            // Gossip gnome: cone hat, sphere head, tapered-cylinder body, ellipsoid beard.
            // Not rotationally symmetric (the beard/hat placement isn't axis-centered relative
            // to itself the way a barrel is) — 12 frames.
            var gnome = new PartsGenerator.Params
            {
                Parts = new List<PartsGenerator.Part>
                {
                    new PartsGenerator.Part { Type = "cone",     Pos = new Vec3(0, 0.95, 0), Size = new Vec3(0.32, 0.4, 0),    Color = (180, 40, 40) },
                    new PartsGenerator.Part { Type = "sphere",   Pos = new Vec3(0, 0.60, 0), Size = new Vec3(0.28, 0.28, 0.28), Color = (230, 195, 150) },
                    new PartsGenerator.Part { Type = "cylinder", Pos = new Vec3(0, 0.28, 0), Size = new Vec3(0.30, 0.56, 0.24), Color = (40, 80, 160) },
                    new PartsGenerator.Part { Type = "sphere",   Pos = new Vec3(0, 0.42, 0.20), Size = new Vec3(0.20, 0.22, 0.14), Color = (245, 245, 245) },
                },
                Symmetric = false,
            };
            GenAsymmetric("gnome", PartsGenerator.Build(gnome), 0x00000020, 9900, outDir);

            // Pet rock: one irregular-ish ellipsoid (squashed on Y, stretched on X), one color.
            // Genuinely rotationally symmetric about Y (single part, centered on the axis) —
            // 3 frames instead of 12.
            var rock = new PartsGenerator.Params
            {
                Parts = new List<PartsGenerator.Part>
                {
                    new PartsGenerator.Part { Type = "sphere", Pos = new Vec3(0, 0.22, 0), Size = new Vec3(0.38, 0.24, 0.32), Color = (118, 112, 102) },
                },
                Symmetric = true,
            };
            GenSymmetric("pet_rock", PartsGenerator.Build(rock), 0x00000021, 9950, outDir);

            // Wishing well: squat cylinder base, two support posts (off-axis -> NOT
            // rotationally symmetric even though the base alone would be), cone roof.
            // Demonstrates Symmetric staying false when a part breaks the axis.
            var well = new PartsGenerator.Params
            {
                Parts = new List<PartsGenerator.Part>
                {
                    new PartsGenerator.Part { Type = "cylinder", Pos = new Vec3(0, 0.20, 0), Size = new Vec3(0.42, 0.4, 0.42), Color = (140, 128, 112) },
                    new PartsGenerator.Part { Type = "cylinder", Pos = new Vec3(-0.32, 0.65, 0), Size = new Vec3(0.04, 0.9, 0.04), Color = (96, 68, 42) },
                    new PartsGenerator.Part { Type = "cylinder", Pos = new Vec3(0.32, 0.65, 0),  Size = new Vec3(0.04, 0.9, 0.04), Color = (96, 68, 42) },
                    new PartsGenerator.Part { Type = "cone",     Pos = new Vec3(0, 1.25, 0), Size = new Vec3(0.5, 0.32, 0), Color = (150, 40, 40) },
                },
                Symmetric = false,
            };
            GenAsymmetric("wishing_well", PartsGenerator.Build(well), 0x00000022, 9960, outDir);

            Console.WriteLine();
            Console.WriteLine("PNGs written to: " + outDir);
        }

        static void GenAsymmetric(string name, Mesh mesh, uint guid, ushort chunkId, string outDir)
        {
            Console.WriteLine($"{name}: {mesh.Faces.Count} faces");
            FSO.PackCompiler.ArtGen.SimpleQuantizer.Reset();
            var iff = SpriteAssembler.BuildIff(mesh, name, guid, chunkId, out _);
            WriteAndDump(name, iff, outDir);
        }

        static void GenSymmetric(string name, Mesh mesh, uint guid, ushort chunkId, string outDir)
        {
            Console.WriteLine($"{name}: {mesh.Faces.Count} faces (rotationally symmetric — 3 renders, not 12)");
            FSO.PackCompiler.ArtGen.SimpleQuantizer.Reset();
            var iff = SymmetricAssembler.BuildIff(mesh, name, guid, chunkId, out _);
            WriteAndDump(name, iff, outDir);
        }

        static void WriteAndDump(string name, IffFile iff, string outDir)
        {
            var iffPath = Path.Combine(outDir, name + ".iff");
            using (var stream = new FileStream(iffPath, FileMode.Create))
                iff.Write(stream);
            Console.WriteLine("  wrote " + iffPath + " (" + new FileInfo(iffPath).Length + " bytes)");

            var readBack = new IffFile();
            using (var stream = new FileStream(iffPath, FileMode.Open))
                readBack.Read(stream);
            var readObjd = readBack.List<OBJD>().First();
            var readDgrp = readBack.List<DGRP>().First(x => x.ChunkID == readObjd.BaseGraphicID);

            foreach (var (zoom, zoomName) in SpriteAssembler.Zooms)
                foreach (var (dirBit, dirName) in SpriteAssembler.Directions)
                {
                    var image = readDgrp.Images.First(i => i.Direction == dirBit && i.Zoom == zoom);
                    var sprite = image.Sprites[0];
                    var spr2 = readBack.Get<SPR2>((ushort)sprite.SpriteID);
                    var frame = spr2.Frames[sprite.SpriteFrameIndex];
                    frame.DecodeIfRequired(false);

                    var nonBg = frame.ZBufferData.Where(b => b != 255).ToArray();
                    var zmin = nonBg.Length > 0 ? nonBg.Min() : (byte)0;
                    var zmax = nonBg.Length > 0 ? nonBg.Max() : (byte)0;
                    Console.WriteLine($"  [{zoomName,-6} {dirName,-10}] W={frame.Width} H={frame.Height} zRange=[{zmin}-{zmax}]");

                    var pngPath = Path.Combine(outDir, $"{name}_{zoomName}_{dirName}.png");
                    PngWriter.Write(pngPath, frame.PixelData, frame.Width, frame.Height);
                }
        }
    }
}
