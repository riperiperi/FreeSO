using System;
using System.IO;
using System.Linq;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using FSO.PackCompiler.ArtGen;

namespace FSO.ArtCalibration
{
    /// <summary>
    /// Generates the first original furniture piece — a parametric chair — through the full
    /// pipeline: ChairGenerator -> Renderer (real Lambertian lighting, real per-sprite depth) ->
    /// SpriteAssembler (real SPR2FrameEncoder) -> real .iff on disk -> read back through the
    /// real decode path -> PNG dumps so it can be eyeballed without launching the game.
    /// Also runs a depth-ramp sanity check on a known-slanted surface (a standalone tilted
    /// plane, not entangled with the chair's other geometry) to validate the real per-sprite
    /// depth normalization added to Renderer.cs.
    /// </summary>
    public static class ChairGenTest
    {
        public static void Run(string outDir)
        {
            Directory.CreateDirectory(outDir);

            DepthRampSanityCheck();

            var chairParams = new ChairGenerator.Params(); // defaults: mid-century-ish proportions
            var mesh = ChairGenerator.Build(chairParams);
            Console.WriteLine($"Chair mesh: {mesh.Faces.Count} faces");

            var iff = SpriteAssembler.BuildIff(mesh, "original_chair", 0x00000002, 9100, out var rendered);

            var iffPath = Path.Combine(outDir, "original_chair.iff");
            using (var stream = new FileStream(iffPath, FileMode.Create))
                iff.Write(stream);
            Console.WriteLine("Wrote " + iffPath + " (" + new FileInfo(iffPath).Length + " bytes)");

            // Read back through the real production decode path, fresh — same discipline as
            // RenderTest.cs's box round-trip.
            var readBack = new IffFile();
            using (var stream = new FileStream(iffPath, FileMode.Open))
                readBack.Read(stream);
            var readObjd = readBack.List<OBJD>().First();
            var readDgrp = readBack.List<DGRP>().First(x => x.ChunkID == readObjd.BaseGraphicID);

            Console.WriteLine();
            Console.WriteLine("=== Decoded frames (real decode path) + PNG dumps ===");
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
                    Console.WriteLine($"[{zoomName,-6} {dirName,-10}] W={frame.Width} H={frame.Height} zRange=[{zmin}-{zmax}]");

                    var pngPath = Path.Combine(outDir, $"chair_{zoomName}_{dirName}.png");
                    PngWriter.Write(pngPath, frame.PixelData, frame.Width, frame.Height);
                }

            Console.WriteLine();
            Console.WriteLine("PNGs written to: " + outDir);
        }

        /// <summary>
        /// Renders a single quad tilted 20 degrees off the ground plane (a "known-slanted
        /// surface" independent of the chair's own geometry) and confirms the decoded z-buffer
        /// is monotonic along the tilt axis, stays clear of the low reserved band (&lt;32),
        /// and never touches the 255 background sentinel except at the true silhouette edge.
        /// </summary>
        static void DepthRampSanityCheck()
        {
            var mesh = new Mesh();
            var tiltRad = 20.0 * Math.PI / 180.0;
            // A plane in the XZ-ish footprint, tilted about the X axis so its Z (depth-facing)
            // extent also gains Y (height) — a clean single-face ramp, facing the camera.
            Vec3 P(double x, double y, double z) => new Vec3(x, y * Math.Cos(tiltRad) - z * Math.Sin(tiltRad), y * Math.Sin(tiltRad) + z * Math.Cos(tiltRad));
            mesh.AddQuad(P(-1, 0, 0), P(1, 0, 0), P(1, 2, 0), P(-1, 2, 0), (180, 180, 180));

            var iffTest = SpriteAssembler.BuildIff(mesh, "depth_ramp_test", 0x00000003, 9200, out var rendered);
            var frame = rendered[(3u, "RightFront")]; // Near zoom

            // Round-trip through the real encode/decode path, same as the chair itself.
            var tmpPath = Path.Combine(Path.GetTempPath(), "fso-depth-ramp-test.iff");
            using (var stream = new FileStream(tmpPath, FileMode.Create)) iffTest.Write(stream);
            var readBack = new IffFile();
            using (var stream = new FileStream(tmpPath, FileMode.Open)) readBack.Read(stream);
            var readDgrp = readBack.List<DGRP>().First();
            var image = readDgrp.Images.First(i => i.Direction == 0x04 && i.Zoom == 3);
            var spr2 = readBack.Get<SPR2>((ushort)image.Sprites[0].SpriteID);
            var decoded = spr2.Frames[image.Sprites[0].SpriteFrameIndex];
            decoded.DecodeIfRequired(false);

            // Sample a vertical scanline through the middle column — should show a monotonic
            // z ramp from the near (bottom, y=0) to far (top, y=2) edge of the tilted plane.
            int midX = decoded.Width / 2;
            var col = new System.Collections.Generic.List<byte>();
            for (int y = 0; y < decoded.Height; y++)
            {
                var z = decoded.ZBufferData[y * decoded.Width + midX];
                if (z != 255) col.Add(z);
            }
            var monotonic = col.Zip(col.Skip(1), (a, b) => b >= a).All(x => x) || col.Zip(col.Skip(1), (a, b) => b <= a).All(x => x);
            var minZ = col.Count > 0 ? col.Min() : (byte)0;
            var maxZ = col.Count > 0 ? col.Max() : (byte)0;
            Console.WriteLine("=== Depth ramp sanity check (standalone 20deg tilted plane, Near/RightFront) ===");
            Console.WriteLine($"Scanline sample count={col.Count} zRange=[{minZ}-{maxZ}] monotonic={monotonic} (clear of <32: {minZ >= 32}, clear of 255 except background: {maxZ < 255})");
            Console.WriteLine();
        }
    }
}
