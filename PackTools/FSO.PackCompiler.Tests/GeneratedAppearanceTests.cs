using System.IO;
using System.Linq;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FSO.PackCompiler.Tests
{
    /// <summary>
    /// Covers appearance.generated (parametric art from ArtGen), the counterpart to
    /// AppearanceCloneTests' coverage of appearance.clone_from_guid. Unlike cloning, this
    /// path needs no base-game content directory, so all these tests run unconditionally.
    /// </summary>
    public class GeneratedAppearanceTests
    {
        private static JObject BasePack(JObject appearance) => BasePack(appearance, "generated_chair", "0x6B4F0F02", "Generated Chair");

        private static JObject BasePack(JObject appearance, string id, string guid, string name)
        {
            return new JObject
            {
                ["schema"] = "fso-pack/0.1",
                ["engine"] = "tso",
                ["pack"] = new JObject { ["id"] = "generated-test", ["name"] = "Generated Test", ["version"] = "1.0.0" },
                ["objects"] = new JArray
                {
                    new JObject
                    {
                        ["id"] = id,
                        ["guid"] = guid,
                        ["name"] = name,
                        ["price"] = 100,
                        ["category"] = "seating",
                        ["appearance"] = appearance,
                        ["trees"] = new JObject
                        {
                            ["main_loop"] = new JObject
                            {
                                ["args"] = new JArray(),
                                ["locals"] = new JArray(),
                                ["nodes"] = new JArray
                                {
                                    new JObject
                                    {
                                        ["id"] = "idle",
                                        ["prim"] = "idle_for_input",
                                        ["ticks_param"] = 0,
                                        ["allow_push"] = false,
                                        ["then"] = "idle",
                                        ["else"] = "idle",
                                    },
                                },
                            },
                        },
                        ["entry_points"] = new JObject { ["main"] = "main_loop" },
                    },
                },
            };
        }

        /// <summary>
        /// Confirms every DGRP/SPR2 chunk in the file resolves inline (same check
        /// ChairGenerator_ProducesDgrpAndSpr2ChunksInline does), reusable across generators.
        /// </summary>
        private static void AssertChunksResolveInline(IffFile iff)
        {
            var dgrps = iff.List<DGRP>();
            Assert.NotNull(dgrps);
            Assert.NotEmpty(dgrps);
            Assert.NotEmpty(iff.List<SPR2>());
            Assert.NotEmpty(iff.List<PALT>());

            var objd = iff.List<OBJD>().Single();
            Assert.True(objd.BaseGraphicID > 0);
            Assert.NotNull(iff.Get<DGRP>(objd.BaseGraphicID));

            foreach (var dgrp in dgrps)
                foreach (var image in dgrp.Images)
                    foreach (var sprite in image.Sprites)
                        Assert.NotNull(iff.Get<SPR2>((ushort)sprite.SpriteID));
        }

        /// <summary>
        /// Regression guard for the cross-frame palette bug fixed this session: each frame of
        /// a multi-material object used to quantize to its own independent local palette, but
        /// only one frame's palette got captured as the shared PALT — any other frame needing
        /// a color outside that one captured set decoded against the wrong slot, including
        /// unused padding slots (0,0,0,0 — opaque black once decoded, since only index 255 is
        /// the transparency sentinel). Every material color this file's generators use is lit
        /// via ambient(0.10) + diffuse against a non-zero base color, so (0,0,0,255) exactly
        /// is not a legitimate lit shade — seeing it means a frame decoded against the wrong
        /// palette.
        /// </summary>
        private static void AssertNoBlackPaletteCorruption(IffFile iff)
        {
            var objd = iff.List<OBJD>().Single();
            var dgrp = iff.Get<DGRP>(objd.BaseGraphicID);
            foreach (var image in dgrp.Images)
                foreach (var sprite in image.Sprites)
                {
                    var spr2 = iff.Get<SPR2>((ushort)sprite.SpriteID);
                    var frame = spr2.Frames[sprite.SpriteFrameIndex];
                    frame.DecodeIfRequired(false);
                    Assert.DoesNotContain(frame.PixelData, px => px.A == 255 && px.R == 0 && px.G == 0 && px.B == 0);
                }
        }

        private static string WritePack(JObject pack)
        {
            var dir = TestPaths.TempDir();
            var path = Path.Combine(dir, "pack.json");
            File.WriteAllText(path, pack.ToString());
            return path;
        }

        [Fact]
        public void ChairGenerator_ProducesDgrpAndSpr2ChunksInline()
        {
            var appearance = new JObject
            {
                ["generated"] = new JObject
                {
                    ["generator"] = "chair",
                    ["params"] = new JObject { ["arms"] = true },
                },
            };
            var packPath = WritePack(BasePack(appearance));
            var outDir = TestPaths.TempDir();

            var result = PackCompilerApi.Build(packPath, outDir);
            Assert.True(result.Success, string.Join("\n", result.Diagnostics.Errors));

            var iff = new IffFile(Path.Combine(outDir, "generated_chair.iff"));

            var dgrps = iff.List<DGRP>();
            Assert.NotNull(dgrps);
            Assert.NotEmpty(dgrps);
            Assert.NotEmpty(iff.List<SPR2>());
            Assert.NotEmpty(iff.List<PALT>());

            var objd = iff.List<OBJD>().Single();
            Assert.True(objd.BaseGraphicID > 0);
            Assert.NotNull(iff.Get<DGRP>(objd.BaseGraphicID));

            // Every sprite a draw group references must resolve inside this file — the
            // constraint DGRP.GetTexture enforces via its own ChunkParent, no cross-file
            // fallback.
            foreach (var dgrp in dgrps)
                foreach (var image in dgrp.Images)
                    foreach (var sprite in image.Sprites)
                        Assert.NotNull(iff.Get<SPR2>((ushort)sprite.SpriteID));

            Assert.Contains(result.Report.Objects.Single().Notes, n => n.Contains("appearance generated by \"chair\""));
        }

        [Fact]
        public void BothCloneAndGenerated_IsError()
        {
            var appearance = new JObject
            {
                ["clone_from_guid"] = "0xC14849AC",
                ["generated"] = new JObject { ["generator"] = "chair" },
            };
            var result = PackCompilerApi.Build(WritePack(BasePack(appearance)), TestPaths.TempDir());

            Assert.False(result.Success);
            Assert.Contains(result.Diagnostics.Errors, e => e.Contains("mutually exclusive"));
        }

        [Fact]
        public void UnknownGenerator_IsError()
        {
            // "sofa" used to be this test's example unknown name — it's a real generator now,
            // so this uses "ottoman" instead (also not implemented).
            var appearance = new JObject
            {
                ["generated"] = new JObject { ["generator"] = "ottoman" },
            };
            var result = PackCompilerApi.Build(WritePack(BasePack(appearance)), TestPaths.TempDir());

            Assert.False(result.Success);
            Assert.Contains(result.Diagnostics.Errors, e => e.Contains("unknown generator \"ottoman\""));
        }

        [Fact]
        public void NonPositiveParam_IsError()
        {
            var appearance = new JObject
            {
                ["generated"] = new JObject
                {
                    ["generator"] = "chair",
                    ["params"] = new JObject { ["seat_width"] = 0 },
                },
            };
            var result = PackCompilerApi.Build(WritePack(BasePack(appearance)), TestPaths.TempDir());

            Assert.False(result.Success);
            Assert.Contains(result.Diagnostics.Errors, e => e.Contains("seat_width") && e.Contains("must be > 0"));
        }

        [Fact]
        public void UnknownParamField_IsError()
        {
            var appearance = new JObject
            {
                ["generated"] = new JObject
                {
                    ["generator"] = "chair",
                    ["params"] = new JObject { ["sparkle"] = true },
                },
            };
            var result = PackCompilerApi.Build(WritePack(BasePack(appearance)), TestPaths.TempDir());

            Assert.False(result.Success);
            Assert.Contains(result.Diagnostics.Errors, e => e.Contains("sparkle") && e.Contains("unknown field"));
        }

        [Fact]
        public void TableGenerator_RoundPedestal_ProducesResolvableChunks()
        {
            // Round + pedestal is the rotationally-symmetric case (SymmetricAssembler) —
            // exercises that path through the real compiler, not just FourLeg/Rectangular.
            var appearance = new JObject
            {
                ["generated"] = new JObject
                {
                    ["generator"] = "table",
                    ["params"] = new JObject { ["top_shape"] = "round", ["base_style"] = "pedestal" },
                },
            };
            var packPath = WritePack(BasePack(appearance, "generated_table", "0x6B4F0F03", "Generated Table"));
            var outDir = TestPaths.TempDir();
            var result = PackCompilerApi.Build(packPath, outDir);
            Assert.True(result.Success, string.Join("\n", result.Diagnostics.Errors));

            var iff = new IffFile(Path.Combine(outDir, "generated_table.iff"));
            AssertChunksResolveInline(iff);
            Assert.Contains(result.Report.Objects.Single().Notes, n => n.Contains("appearance generated by \"table\""));
        }

        [Fact]
        public void BedGenerator_ProducesResolvableChunks()
        {
            var appearance = new JObject
            {
                ["generated"] = new JObject { ["generator"] = "bed", ["params"] = new JObject { ["footboard"] = true } },
            };
            var packPath = WritePack(BasePack(appearance, "generated_bed", "0x6B4F0F04", "Generated Bed"));
            var outDir = TestPaths.TempDir();
            var result = PackCompilerApi.Build(packPath, outDir);
            Assert.True(result.Success, string.Join("\n", result.Diagnostics.Errors));

            var iff = new IffFile(Path.Combine(outDir, "generated_bed.iff"));
            AssertChunksResolveInline(iff);
            Assert.Contains(result.Report.Objects.Single().Notes, n => n.Contains("appearance generated by \"bed\""));
        }

        [Fact]
        public void LampGenerator_ProducesResolvableChunks()
        {
            // Always rotationally symmetric — exercises SymmetricAssembler like the round table.
            var appearance = new JObject { ["generated"] = new JObject { ["generator"] = "lamp" } };
            var packPath = WritePack(BasePack(appearance, "generated_lamp", "0x6B4F0F05", "Generated Lamp"));
            var outDir = TestPaths.TempDir();
            var result = PackCompilerApi.Build(packPath, outDir);
            Assert.True(result.Success, string.Join("\n", result.Diagnostics.Errors));

            var iff = new IffFile(Path.Combine(outDir, "generated_lamp.iff"));
            AssertChunksResolveInline(iff);
            Assert.Contains(result.Report.Objects.Single().Notes, n => n.Contains("appearance generated by \"lamp\""));
        }

        [Fact]
        public void SofaGenerator_ProducesResolvableChunksWithoutPaletteCorruption()
        {
            // Wood + upholstery + seam = 3 distinct materials — another multi-color case for
            // the palette regression guard, and the width/arm_width relationship validated.
            var appearance = new JObject
            {
                ["generated"] = new JObject
                {
                    ["generator"] = "sofa",
                    ["params"] = new JObject
                    {
                        ["cushion_count"] = 3,
                        ["wood_color"] = new JArray(88, 56, 32),
                        ["upholstery_color"] = new JArray(204, 128, 74),
                        ["seam_color"] = new JArray(158, 96, 54),
                    },
                },
            };
            var packPath = WritePack(BasePack(appearance, "generated_sofa", "0x6B4F0F0B", "Generated Sofa"));
            var outDir = TestPaths.TempDir();
            var result = PackCompilerApi.Build(packPath, outDir);
            Assert.True(result.Success, string.Join("\n", result.Diagnostics.Errors));

            var iff = new IffFile(Path.Combine(outDir, "generated_sofa.iff"));
            AssertChunksResolveInline(iff);
            AssertNoBlackPaletteCorruption(iff);
            Assert.Contains(result.Report.Objects.Single().Notes, n => n.Contains("appearance generated by \"sofa\""));
        }

        [Fact]
        public void SofaGenerator_WidthNotGreaterThanTwiceArmWidth_IsError()
        {
            var appearance = new JObject
            {
                ["generated"] = new JObject
                {
                    ["generator"] = "sofa",
                    ["params"] = new JObject { ["width"] = 1.0, ["arm_width"] = 0.6 },
                },
            };
            var result = PackCompilerApi.Build(WritePack(BasePack(appearance, "generated_sofa_bad", "0x6B4F0F0C", "Generated Sofa Bad")), TestPaths.TempDir());

            Assert.False(result.Success);
            Assert.Contains(result.Diagnostics.Errors, e => e.Contains("width") && e.Contains("2x arm_width"));
        }

        [Fact]
        public void StorageGenerator_Dresser_ProducesResolvableChunksWithoutPaletteCorruption()
        {
            // Dresser: carcass + accent drawer bands = several distinct materials/shaded
            // colors — the multi-color case that actually exposed the palette bug this
            // session (the chair alone never had enough colors to trigger it).
            var appearance = new JObject
            {
                ["generated"] = new JObject
                {
                    ["generator"] = "storage",
                    ["params"] = new JObject
                    {
                        ["kind"] = "dresser",
                        ["sections"] = 3,
                        ["carcass_color"] = new JArray(150, 128, 96),
                        ["accent_color"] = new JArray(60, 52, 44),
                    },
                },
            };
            var packPath = WritePack(BasePack(appearance, "generated_dresser", "0x6B4F0F06", "Generated Dresser"));
            var outDir = TestPaths.TempDir();
            var result = PackCompilerApi.Build(packPath, outDir);
            Assert.True(result.Success, string.Join("\n", result.Diagnostics.Errors));

            var iff = new IffFile(Path.Combine(outDir, "generated_dresser.iff"));
            AssertChunksResolveInline(iff);
            AssertNoBlackPaletteCorruption(iff);
        }

        [Fact]
        public void PrimitivesGenerator_MultiPartMultiColor_ProducesResolvableChunksWithoutPaletteCorruption()
        {
            var appearance = new JObject
            {
                ["generated"] = new JObject
                {
                    ["generator"] = "primitives",
                    ["params"] = new JObject
                    {
                        ["symmetric"] = false,
                        ["parts"] = new JArray
                        {
                            new JObject { ["type"] = "cone", ["pos"] = new JArray(0, 0.95, 0), ["size"] = new JArray(0.32, 0.4, 0), ["color"] = new JArray(180, 40, 40) },
                            new JObject { ["type"] = "sphere", ["pos"] = new JArray(0, 0.6, 0), ["size"] = new JArray(0.28, 0.28, 0.28), ["color"] = new JArray(230, 195, 150) },
                            new JObject { ["type"] = "cylinder", ["pos"] = new JArray(0, 0.28, 0), ["size"] = new JArray(0.3, 0.56, 0.24), ["color"] = new JArray(40, 80, 160) },
                            new JObject { ["type"] = "sphere", ["pos"] = new JArray(0, 0.42, 0.2), ["size"] = new JArray(0.2, 0.22, 0.14), ["color"] = new JArray(245, 245, 245) },
                        },
                    },
                },
            };
            var packPath = WritePack(BasePack(appearance, "generated_gnome", "0x6B4F0F07", "Generated Gnome"));
            var outDir = TestPaths.TempDir();
            var result = PackCompilerApi.Build(packPath, outDir);
            Assert.True(result.Success, string.Join("\n", result.Diagnostics.Errors));

            var iff = new IffFile(Path.Combine(outDir, "generated_gnome.iff"));
            AssertChunksResolveInline(iff);
            AssertNoBlackPaletteCorruption(iff);
            Assert.Contains(result.Report.Objects.Single().Notes, n => n.Contains("appearance generated by \"primitives\""));
        }

        [Fact]
        public void PrimitivesGenerator_UnknownPartType_IsError()
        {
            var appearance = new JObject
            {
                ["generated"] = new JObject
                {
                    ["generator"] = "primitives",
                    ["params"] = new JObject
                    {
                        ["parts"] = new JArray
                        {
                            new JObject { ["type"] = "torus", ["pos"] = new JArray(0, 0, 0), ["size"] = new JArray(1, 1, 1), ["color"] = new JArray(0, 0, 0) },
                        },
                    },
                },
            };
            var result = PackCompilerApi.Build(WritePack(BasePack(appearance, "generated_bad", "0x6B4F0F08", "Generated Bad")), TestPaths.TempDir());

            Assert.False(result.Success);
            Assert.Contains(result.Diagnostics.Errors, e => e.Contains("unknown part type \"torus\""));
        }

        [Fact]
        public void PrimitivesGenerator_NonPositivePartSize_IsError()
        {
            var appearance = new JObject
            {
                ["generated"] = new JObject
                {
                    ["generator"] = "primitives",
                    ["params"] = new JObject
                    {
                        ["parts"] = new JArray
                        {
                            new JObject { ["type"] = "box", ["pos"] = new JArray(0, 0, 0), ["size"] = new JArray(1, 0, 1), ["color"] = new JArray(0, 0, 0) },
                        },
                    },
                },
            };
            var result = PackCompilerApi.Build(WritePack(BasePack(appearance, "generated_bad2", "0x6B4F0F09", "Generated Bad 2")), TestPaths.TempDir());

            Assert.False(result.Success);
            Assert.Contains(result.Diagnostics.Errors, e => e.Contains("size[1]") && e.Contains("must be > 0"));
        }

        [Fact]
        public void PrimitivesGenerator_EmptyPartsList_IsError()
        {
            var appearance = new JObject
            {
                ["generated"] = new JObject { ["generator"] = "primitives", ["params"] = new JObject() },
            };
            var result = PackCompilerApi.Build(WritePack(BasePack(appearance, "generated_bad3", "0x6B4F0F0A", "Generated Bad 3")), TestPaths.TempDir());

            Assert.False(result.Success);
            Assert.Contains(result.Diagnostics.Errors, e => e.Contains("at least one part is required"));
        }
    }
}
