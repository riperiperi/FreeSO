using System;
using System.IO;
using System.Linq;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using FSO.PackCompiler.ArtGen;

namespace FSO.ArtCalibration
{
    /// <summary>
    /// Decodes every .iff in a directory (compiled pack output) through the real production
    /// decode path — fresh IffFile.Read, SPR2Frame.DecodeIfRequired, no shortcuts through
    /// in-memory render state — and dumps one PNG per (object, zoom, direction). This is the
    /// verification step ART-PIPELINE-CALIBRATION.md's discipline requires: a compiled .iff
    /// proves nothing about how it looks until it's decoded back out and looked at.
    /// </summary>
    public static class DecodePackDir
    {
        public static void Run(string packDir, string outDir)
        {
            Directory.CreateDirectory(outDir);

            foreach (var iffPath in Directory.GetFiles(packDir, "*.iff").OrderBy(x => x))
            {
                var name = Path.GetFileNameWithoutExtension(iffPath);
                var iff = new IffFile();
                using (var stream = new FileStream(iffPath, FileMode.Open))
                    iff.Read(stream);

                var objd = iff.List<OBJD>().FirstOrDefault();
                if (objd == null || objd.BaseGraphicID == 0)
                {
                    Console.WriteLine($"{name}: no appearance (BaseGraphicID=0) — skipped");
                    continue;
                }
                var dgrp = iff.Get<DGRP>(objd.BaseGraphicID);
                if (dgrp == null)
                {
                    Console.WriteLine($"{name}: BaseGraphicID={objd.BaseGraphicID} but no DGRP found — skipped");
                    continue;
                }

                Console.WriteLine($"{name}: {dgrp.Images.Length} images");
                foreach (var (zoom, zoomName) in SpriteAssembler.Zooms)
                    foreach (var (dirBit, dirName) in SpriteAssembler.Directions)
                    {
                        var image = dgrp.Images.FirstOrDefault(i => i.Direction == dirBit && i.Zoom == zoom);
                        if (image == null) continue;
                        var sprite = image.Sprites[0];
                        var spr2 = iff.Get<SPR2>((ushort)sprite.SpriteID);
                        var frame = spr2.Frames[sprite.SpriteFrameIndex];
                        frame.DecodeIfRequired(false);

                        var pngPath = Path.Combine(outDir, $"{name}_{zoomName}_{dirName}.png");
                        PngWriter.Write(pngPath, frame.PixelData, frame.Width, frame.Height);
                    }
            }

            Console.WriteLine();
            Console.WriteLine("PNGs written to: " + outDir);
        }
    }
}
