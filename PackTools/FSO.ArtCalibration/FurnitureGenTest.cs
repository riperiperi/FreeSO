using System;
using System.IO;
using System.Linq;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using FSO.PackCompiler.ArtGen;

namespace FSO.ArtCalibration
{
    /// <summary>
    /// Generates table/bed/lamp/storage pieces through the full pipeline, same discipline as
    /// ChairGenTest.cs: real renderer, real SPR2FrameEncoder, real .iff on disk, read back
    /// through the real decode path, PNG dumps per frame so output can be eyeballed.
    /// </summary>
    public static class FurnitureGenTest
    {
        public static void Run(string outDir)
        {
            Directory.CreateDirectory(outDir);

            GenAsymmetric("table_rect", TableGenerator.Build(new TableGenerator.Params
            {
                TopShape = TableGenerator.TopShapeType.Rectangular,
                BaseStyle = TableGenerator.BaseStyleType.FourLeg,
            }), 0x00000010, 9200, outDir);

            GenSymmetric("table_round", TableGenerator.Build(new TableGenerator.Params
            {
                TopShape = TableGenerator.TopShapeType.Round,
                BaseStyle = TableGenerator.BaseStyleType.Pedestal,
                TopDiameter = 1.4,
                Height = 1.1,
            }), 0x00000011, 9300, outDir);

            GenAsymmetric("bed", BedGenerator.Build(new BedGenerator.Params()), 0x00000012, 9400, outDir);

            GenAsymmetric("bed_footboard", BedGenerator.Build(new BedGenerator.Params { Footboard = true }), 0x00000013, 9500, outDir);

            GenSymmetric("lamp", LampGenerator.Build(new LampGenerator.Params()), 0x00000014, 9600, outDir);

            GenAsymmetric("bookshelf", StorageGenerator.Build(new StorageGenerator.Params
            {
                Kind = StorageGenerator.KindType.Bookshelf,
            }), 0x00000015, 9700, outDir);

            GenAsymmetric("dresser", StorageGenerator.Build(new StorageGenerator.Params
            {
                Kind = StorageGenerator.KindType.Dresser,
                Width = 1.2,
                Depth = 0.5,
                Height = 0.85,
                Sections = 3,
                CarcassColor = (150, 128, 96),
                AccentColor = (60, 52, 44),
            }), 0x00000016, 9800, outDir);

            Console.WriteLine();
            Console.WriteLine("PNGs written to: " + outDir);
        }

        static void GenAsymmetric(string name, Mesh mesh, uint guid, ushort chunkId, string outDir)
        {
            Console.WriteLine($"{name}: {mesh.Faces.Count} faces");
            FSO.PackCompiler.ArtGen.SimpleQuantizer.Reset(); // fresh palette per object — don't leak colors across builds
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
