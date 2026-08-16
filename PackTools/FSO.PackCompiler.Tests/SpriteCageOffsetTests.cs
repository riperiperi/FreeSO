using System.IO;
using System.Linq;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using FSO.PackCompiler.ArtGen;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FSO.PackCompiler.Tests
{
    public class SpriteCageOffsetTests
    {
        [Fact]
        public void AssembledAppearance_UsesBaseGameCageOffsetAndZBand()
        {
            var dir = TestPaths.TempDir();
            File.WriteAllText(Path.Combine(dir, "box.obj"),
                "v -0.5 0  -0.5\nv  0.5 0  -0.5\nv  0.5 0   0.5\nv -0.5 0   0.5\n" +
                "v -0.5 1  -0.5\nv  0.5 1  -0.5\nv  0.5 1   0.5\nv -0.5 1   0.5\n" +
                "f 1 2 3 4\nf 5 6 7 8\nf 1 2 6 5\nf 2 3 7 6\nf 3 4 8 7\nf 4 1 5 8\n");
            var pack = new JObject
            {
                ["schema"] = "fso-pack/0.1",
                ["engine"] = "tso",
                ["pack"] = new JObject { ["id"] = "cage-test", ["name"] = "Cage", ["version"] = "1.0.0" },
                ["objects"] = new JArray
                {
                    new JObject
                    {
                        ["id"] = "cage_box",
                        ["guid"] = "0x6B4F0F30",
                        ["name"] = "Cage Box",
                        ["price"] = 10,
                        ["category"] = "misc",
                        ["appearance"] = new JObject
                        {
                            ["imported"] = new JObject
                            {
                                ["mesh"] = "box.obj",
                                ["height"] = 1.0,
                                ["provenance"] = new JObject
                                {
                                    ["source"] = "Test", ["license"] = "CC0",
                                    ["retrieved"] = "2026-08-13", ["model"] = "box",
                                },
                            },
                        },
                        ["trees"] = new JObject
                        {
                            ["main_loop"] = new JObject
                            {
                                ["args"] = new JArray(), ["locals"] = new JArray(),
                                ["nodes"] = new JArray
                                {
                                    new JObject
                                    {
                                        ["id"] = "idle", ["prim"] = "idle_for_input", ["ticks_param"] = 0,
                                        ["allow_push"] = false, ["then"] = "idle", ["else"] = "idle",
                                    },
                                },
                            },
                        },
                        ["entry_points"] = new JObject { ["main"] = "main_loop" },
                    },
                },
            };
            var packPath = Path.Combine(dir, "pack.json");
            File.WriteAllText(packPath, pack.ToString());
            var outDir = TestPaths.TempDir();
            var result = PackCompilerApi.Build(packPath, outDir);
            Assert.True(result.Success, string.Join("\n", result.Diagnostics.Errors));

            var iff = new IffFile(Path.Combine(outDir, "cage_box.iff"));
            var dgrp = iff.List<DGRP>().Single();
            foreach (var img in dgrp.Images)
            {
                var sp = img.Sprites.Single();
                var spr = iff.Get<SPR2>((ushort)sp.SpriteID);
                var fr = spr.Frames[(int)sp.SpriteFrameIndex];
                fr.DecodeIfRequired(false);

                var zf = SpriteAssembler.ZoomFactor(img.Zoom);
                var expectX = (int)((-68 * zf) + fr.Position.X);
                var expectY = (-348 * zf) + fr.Height + fr.Position.Y;
                Assert.Equal(expectX, sp.SpriteOffset.X);
                Assert.Equal(expectY, sp.SpriteOffset.Y);
                // Must not leave the default (0,0) that exploded Full-3D reconstruction.
                Assert.False(sp.SpriteOffset.X == 0 && sp.SpriteOffset.Y == 0);

                Assert.NotNull(fr.ZBufferData);
                byte zmin = 255, zmax = 0;
                foreach (var z in fr.ZBufferData)
                {
                    if (z == 255) continue;
                    if (z < zmin) zmin = z;
                    if (z > zmax) zmax = z;
                }
                Assert.InRange(zmin, 135, 210);
                Assert.InRange(zmax, 135, 210);
            }
        }
    }
}
