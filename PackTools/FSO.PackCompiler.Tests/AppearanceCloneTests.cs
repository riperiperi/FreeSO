using System;
using System.IO;
using System.Linq;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FSO.PackCompiler.Tests
{
    /// <summary>
    /// Covers appearance.clone_from_guid. The clone path needs a real TSO content install to
    /// read sprites out of the objiff/objspf FAR archives, so those tests no-op on a checkout
    /// without one; the "no game dir" behavior is asserted unconditionally, since that's the
    /// path CI and a fresh clone actually take.
    /// </summary>
    public class AppearanceCloneTests
    {
        // A real base-game object with sprites: "Garden Gnome" from the object table.
        private const string GardenGnomeGuid = "0xC14849AC";

        private static string GameDir()
        {
            var dir = Environment.GetEnvironmentVariable("FSO_VM_GAME_LOCATION")
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Library/Application Support/The Sims Online/TSOClient");
            return Directory.Exists(dir) ? dir : null;
        }

        private static string WritePack(string cloneFromGuid)
        {
            var pack = new JObject
            {
                ["schema"] = "fso-pack/0.1",
                ["engine"] = "tso",
                ["pack"] = new JObject { ["id"] = "clone-test", ["name"] = "Clone Test", ["version"] = "1.0.0" },
                ["objects"] = new JArray
                {
                    new JObject
                    {
                        ["id"] = "cloned_gnome",
                        ["guid"] = "0x6B4F0F01",
                        ["name"] = "Cloned Gnome",
                        ["price"] = 100,
                        ["category"] = "decorative",
                        ["appearance"] = new JObject { ["clone_from_guid"] = cloneFromGuid },
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
                                        ["allow_push"] = true,
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

            var dir = TestPaths.TempDir();
            var path = Path.Combine(dir, "pack.json");
            File.WriteAllText(path, pack.ToString());
            return path;
        }

        [Fact]
        public void WithoutGameDir_CompilesButEmitsNoGraphics()
        {
            var packPath = WritePack(GardenGnomeGuid);
            var outDir = TestPaths.TempDir();

            var result = PackCompilerApi.Build(packPath, outDir);
            Assert.True(result.Success, string.Join("\n", result.Diagnostics.Errors));

            var iff = new IffFile(Path.Combine(outDir, "cloned_gnome.iff"));
            var objd = iff.List<OBJD>().Single();
            Assert.Equal(0, objd.BaseGraphicID);
            Assert.Null(iff.List<DGRP>());

            var note = result.Report.Objects.Single().Notes.Single();
            Assert.Contains("INVISIBLE", note);
        }

        [Fact]
        public void WithGameDir_CopiesDrawGroupsAndSpritesInline()
        {
            var gameDir = GameDir();
            if (gameDir == null) return; // no TSO content on this machine — skip

            var packPath = WritePack(GardenGnomeGuid);
            var outDir = TestPaths.TempDir();

            var result = PackCompilerApi.Build(packPath, outDir, gameDir);
            Assert.True(result.Success, string.Join("\n", result.Diagnostics.Errors));

            var iff = new IffFile(Path.Combine(outDir, "cloned_gnome.iff"));

            // The draw groups and their sprites must both be in THIS file: DGRP.GetTexture
            // resolves SPR2 via its own ChunkParent, so a cross-file reference renders nothing.
            var dgrps = iff.List<DGRP>();
            Assert.NotNull(dgrps);
            Assert.NotEmpty(dgrps);
            Assert.NotEmpty(iff.List<SPR2>());

            // OBJD graphics fields must point at the copied draw groups, and BaseGraphicID
            // must be non-zero — ObjectComponent only looks up a DGRP when it is.
            var objd = iff.List<OBJD>().Single();
            Assert.True(objd.BaseGraphicID > 0);
            Assert.NotNull(iff.Get<DGRP>(objd.BaseGraphicID));

            // Every sprite a copied draw group references must resolve inside this file.
            foreach (var dgrp in dgrps)
                foreach (var image in dgrp.Images)
                    foreach (var sprite in image.Sprites)
                        Assert.NotNull(iff.Get<SPR2>((ushort)sprite.SpriteID));
        }

        [Fact]
        public void WithGameDir_UnknownSourceGuid_ReportsError()
        {
            var gameDir = GameDir();
            if (gameDir == null) return;

            var packPath = WritePack("0xDEADBEEF");
            var result = PackCompilerApi.Build(packPath, TestPaths.TempDir(), gameDir);

            Assert.False(result.Success);
            Assert.Contains(result.Diagnostics.Errors, e => e.Contains("not found in the base game object table"));
        }
    }
}
