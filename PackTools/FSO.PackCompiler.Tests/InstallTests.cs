using System.IO;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FSO.PackCompiler.Tests
{
    /// <summary>
    /// Install() deploys into an actual game, so unlike Build()/Validate() (where compiling a
    /// clone_from_guid appearance without a game content dir is an intentional, documented
    /// headless-testing mode, SCHEMA.md), installing an object that will render as invisible
    /// is never acceptable — it used to succeed silently with only a printed note. Covers the
    /// fix: GraphicsMissing objects make Install() fail loud, before anything is written.
    /// </summary>
    public class InstallTests
    {
        [Fact]
        public void NoTsoDir_CloneFromGuid_FailsLoud_WritesNothing()
        {
            var gameDir = TestPaths.TempDir();

            // gossip-gnome.json uses clone_from_guid and no tsoContentDir is passed here.
            var result = PackCompilerApi.Install(TestPaths.Example("gossip-gnome.json"), gameDir);

            Assert.False(result.Success);
            Assert.Contains(result.Diagnostics.Errors, e =>
                e.Contains("invisible") && e.Contains("--tso-dir"));

            // Fail-loud means fail-before-write: no Objects dir, no .iff, nothing installed.
            Assert.False(Directory.Exists(Path.Combine(gameDir, "Objects")));
        }

        [Fact]
        public void GeneratedAppearance_NoTsoDirNeeded_InstallSucceeds()
        {
            var gameDir = TestPaths.TempDir();
            var pack = JObject.Parse(File.ReadAllText(TestPaths.Example("gossip-gnome.json")));
            pack["objects"][0]["appearance"] = new JObject
            {
                ["generated"] = new JObject { ["generator"] = "chair" },
            };
            var packPath = Path.Combine(TestPaths.TempDir(), "pack.json");
            File.WriteAllText(packPath, pack.ToString());

            var result = PackCompilerApi.Install(packPath, gameDir);

            Assert.True(result.Success, string.Join("\n", result.Diagnostics.Errors));
            Assert.True(File.Exists(Path.Combine(gameDir, "Objects", "gossip_gnome.iff")));
        }

        [Fact]
        public void WithRealTsoDir_CloneFromGuid_InstallSucceeds()
        {
            var tsoDir = AppearanceCloneTestsGameDir();
            if (tsoDir == null) return; // no TSO content on this machine — skip

            var gameDir = TestPaths.TempDir();
            var result = PackCompilerApi.Install(TestPaths.Example("gossip-gnome.json"), gameDir, tsoDir);

            Assert.True(result.Success, string.Join("\n", result.Diagnostics.Errors));
            Assert.True(File.Exists(Path.Combine(gameDir, "Objects", "gossip_gnome.iff")));
        }

        // Same resolution AppearanceCloneTests.GameDir() uses — kept local since that helper
        // is private to that class.
        private static string AppearanceCloneTestsGameDir()
        {
            var dir = System.Environment.GetEnvironmentVariable("FSO_VM_GAME_LOCATION")
                ?? Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
                    "Library/Application Support/The Sims Online/TSOClient");
            return Directory.Exists(dir) ? dir : null;
        }

        // Covers the CLI gap this class's other tests don't reach: `build` previously had no
        // way at all to supply a sprite-source directory (no flag, no env var read, despite
        // SCHEMA.md documenting FSO_VM_GAME_LOCATION), so a bare CLI build of a clone_from_guid
        // pack was structurally invisible regardless of what was on disk. Only exercises the
        // explicit --tso-dir flag, not the env var fallback — mutating FSO_VM_GAME_LOCATION in
        // a test would race against AppearanceCloneTests/InstallTests reading the same
        // process-global env var if xunit runs test classes in parallel.
        [Fact]
        public void Cli_Build_WithTsoDirFlag_ClonesRealSprites()
        {
            var tsoDir = AppearanceCloneTestsGameDir();
            if (tsoDir == null) return; // no TSO content on this machine — skip

            var outDir = TestPaths.TempDir();
            var exitCode = Program.Main(new[]
            {
                "build", TestPaths.Example("gossip-gnome.json"), "-o", outDir, "--tso-dir", tsoDir,
            });

            Assert.Equal(0, exitCode);
            var iffPath = Path.Combine(outDir, "gossip_gnome.iff");
            Assert.True(File.Exists(iffPath));

            var iff = new FSO.Files.Formats.IFF.IffFile(iffPath);
            Assert.NotEmpty(iff.List<FSO.Files.Formats.IFF.Chunks.DGRP>());
            Assert.NotEmpty(iff.List<FSO.Files.Formats.IFF.Chunks.SPR2>());
        }
    }
}
