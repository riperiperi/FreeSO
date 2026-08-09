using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FSO.PackCompiler.Tests
{
    /// <summary>
    /// The property that matters: compile -> decompile -> compile again must be lossless for
    /// appearance, not just "a field is present" after one decompile. Before provenance,
    /// Decompiler fabricated a placeholder appearance.generated (chair) for every object,
    /// silently changing a clone_from_guid object into a generated one on round-trip.
    /// </summary>
    public class AppearanceProvenanceTests
    {
        private static string GameDir()
        {
            var dir = Environment.GetEnvironmentVariable("FSO_VM_GAME_LOCATION")
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Library/Application Support/The Sims Online/TSOClient");
            return Directory.Exists(dir) ? dir : null;
        }

        [Fact]
        public void GeneratedChair_SurvivesCompileDecompileCompile()
        {
            var pack = new JObject
            {
                ["schema"] = "fso-pack/0.1",
                ["engine"] = "tso",
                ["pack"] = new JObject { ["id"] = "provenance-test", ["name"] = "Provenance Test", ["version"] = "1.0.0" },
                ["objects"] = new JArray
                {
                    new JObject
                    {
                        ["id"] = "provenance_chair",
                        ["guid"] = "0x6B4F0F10",
                        ["name"] = "Provenance Chair",
                        ["price"] = 100,
                        ["category"] = "seating",
                        ["appearance"] = new JObject
                        {
                            ["generated"] = new JObject
                            {
                                ["generator"] = "chair",
                                ["params"] = new JObject { ["arms"] = true, ["seat_width"] = 1.9 },
                            },
                        },
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
                                        ["id"] = "idle", ["prim"] = "idle_for_input", ["ticks_param"] = 0,
                                        ["allow_push"] = true, ["then"] = "idle", ["else"] = "idle",
                                    },
                                },
                            },
                        },
                        ["entry_points"] = new JObject { ["main"] = "main_loop" },
                    },
                },
            };

            var dir1 = TestPaths.TempDir();
            var packPath1 = Path.Combine(dir1, "pack.json");
            File.WriteAllText(packPath1, pack.ToString());

            var build1 = PackCompilerApi.Build(packPath1, dir1);
            Assert.True(build1.Success, string.Join("\n", build1.Diagnostics.Errors));

            var decompiledPath = Path.Combine(dir1, "decompiled.json");
            var decompile = PackCompilerApi.Decompile(Path.Combine(dir1, "provenance_chair.iff"), decompiledPath);
            Assert.True(decompile.Success, string.Join("\n", decompile.Diagnostics.Errors));

            // The point of provenance: no placeholder warning, because the real appearance
            // was recovered rather than fabricated.
            Assert.DoesNotContain(decompile.Diagnostics.Warnings, w => w.Contains("placeholder"));

            var decompiledJson = JObject.Parse(File.ReadAllText(decompiledPath));
            var decompiledObj = (JObject)decompiledJson["objects"][0];
            var appearance = (JObject)decompiledObj["appearance"];
            Assert.NotNull(appearance["generated"]);
            Assert.Equal("chair", (string)appearance["generated"]["generator"]);
            var recoveredParams = (JObject)appearance["generated"]["params"];
            Assert.True((bool)recoveredParams["arms"]);
            Assert.Equal(1.9, (double)recoveredParams["seat_width"], 6);

            // Recompile the decompiled pack and assert the SECOND .iff's rendered appearance
            // matches the first — the actual property, not just "a field survived decompile".
            var dir2 = TestPaths.TempDir();
            var build2 = PackCompilerApi.Build(decompiledPath, dir2);
            Assert.True(build2.Success, string.Join("\n", build2.Diagnostics.Errors));

            var iff1 = new FSO.Files.Formats.IFF.IffFile(Path.Combine(dir1, "provenance_chair.iff"));
            var iff2 = new FSO.Files.Formats.IFF.IffFile(Path.Combine(dir2, "provenance_chair.iff"));
            var dgrp1 = iff1.List<FSO.Files.Formats.IFF.Chunks.DGRP>();
            var dgrp2 = iff2.List<FSO.Files.Formats.IFF.Chunks.DGRP>();
            Assert.Equal(dgrp1.Count, dgrp2.Count);
            Assert.NotEmpty(dgrp1);
        }

        [Fact]
        public void ClonedAppearance_SurvivesCompileDecompileCompile()
        {
            if (GameDir() == null) return; // needs real TSO content to clone sprites from

            const string gardenGnomeGuid = "0xC14849AC";
            var pack = new JObject
            {
                ["schema"] = "fso-pack/0.1",
                ["engine"] = "tso",
                ["pack"] = new JObject { ["id"] = "provenance-clone-test", ["name"] = "Provenance Clone Test", ["version"] = "1.0.0" },
                ["objects"] = new JArray
                {
                    new JObject
                    {
                        ["id"] = "provenance_gnome",
                        ["guid"] = "0x6B4F0F11",
                        ["name"] = "Provenance Gnome",
                        ["price"] = 50,
                        ["category"] = "decorative",
                        ["appearance"] = new JObject { ["clone_from_guid"] = gardenGnomeGuid },
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
                                        ["id"] = "idle", ["prim"] = "idle_for_input", ["ticks_param"] = 0,
                                        ["allow_push"] = true, ["then"] = "idle", ["else"] = "idle",
                                    },
                                },
                            },
                        },
                        ["entry_points"] = new JObject { ["main"] = "main_loop" },
                    },
                },
            };

            var dir = TestPaths.TempDir();
            var packPath = Path.Combine(dir, "pack.json");
            File.WriteAllText(packPath, pack.ToString());

            var build = PackCompilerApi.Build(packPath, dir, GameDir());
            Assert.True(build.Success, string.Join("\n", build.Diagnostics.Errors));

            var decompiledPath = Path.Combine(dir, "decompiled.json");
            var decompile = PackCompilerApi.Decompile(Path.Combine(dir, "provenance_gnome.iff"), decompiledPath);
            Assert.True(decompile.Success, string.Join("\n", decompile.Diagnostics.Errors));
            Assert.DoesNotContain(decompile.Diagnostics.Warnings, w => w.Contains("placeholder"));

            var decompiledJson = JObject.Parse(File.ReadAllText(decompiledPath));
            var decompiledObj = (JObject)decompiledJson["objects"][0];
            Assert.Equal(gardenGnomeGuid, (string)decompiledObj["appearance"]["clone_from_guid"]);

            // Chunk existence and a recovered guid are NOT proof the object renders —
            // StampProvenance re-reads and rewrites the .iff after PackBuilder writes it,
            // which once silently zeroed every sprite's dimensions on the way back out (an
            // undecoded SPR2Frame serializes as 0x0 with no error). Decode a fresh frame and
            // check real pixels, the same shape of check AppearanceCloneTests uses for the
            // clone step itself.
            var iff = new FSO.Files.Formats.IFF.IffFile(Path.Combine(dir, "provenance_gnome.iff"));
            var objd = iff.List<FSO.Files.Formats.IFF.Chunks.OBJD>().Single();
            var dgrp = iff.Get<FSO.Files.Formats.IFF.Chunks.DGRP>(objd.BaseGraphicID);
            var sprite = dgrp.Images[0].Sprites[0];
            var frame = iff.Get<FSO.Files.Formats.IFF.Chunks.SPR2>((ushort)sprite.SpriteID).Frames[sprite.SpriteFrameIndex];
            frame.DecodeIfRequired(false);
            Assert.True(frame.Width > 0 && frame.Height > 0,
                $"frame decoded to {frame.Width}x{frame.Height} after provenance stamping — StampProvenance's rewrite zeroed it");
            Assert.True(frame.PixelData.Count(p => p.A != 0) > 10, "frame decoded to real dimensions but essentially no opaque pixels");
        }

        [Fact]
        public void ObjectPredatingProvenance_FallsBackToPlaceholder_WithHonestWarning()
        {
            // Compile normally, then strip the provenance chunk to simulate an .iff built
            // before this feature existed — the fallback path, not a bug in Read().
            var dir = TestPaths.TempDir();
            var build = PackCompilerApi.Build(TestPaths.Example("gossip-gnome.json"), dir);
            Assert.True(build.Success);

            var iffPath = Path.Combine(dir, "gossip_gnome.iff");
            var iff = new FSO.Files.Formats.IFF.IffFile(iffPath);
            var chunk = iff.Get<FSO.Files.Formats.IFF.Chunks.STR>(AppearanceProvenance.CHUNK_ID);
            Assert.NotNull(chunk); // sanity: StampProvenance actually wrote it
            iff.FullRemoveChunk(chunk);
            using (var stream = new FileStream(iffPath, FileMode.Create)) iff.Write(stream);

            var decompiledPath = Path.Combine(dir, "decompiled.json");
            var decompile = PackCompilerApi.Decompile(iffPath, decompiledPath);
            Assert.True(decompile.Success);

            var warning = decompile.Diagnostics.Warnings.FirstOrDefault(w => w.Contains("placeholder"));
            Assert.NotNull(warning);
            // The wording matters: this must read as "predates the feature", not "recovery
            // failed" — those imply different things about whether newer objects are trustworthy.
            Assert.Contains("predates", warning);
        }
    }
}
