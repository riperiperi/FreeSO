// Integration test: renders lot 2 from the live FSO server and asserts a valid PNG is written.
//
// Requirements:
//   FSO server up at FSO_RENDERER_API_URL (default http://workshop:9000)
//   Game assets at FSO_GAME_LOCATION (default /home/baron/projects/freeso-experiment/GameAssets/)
//   SDL_VIDEODRIVER=offscreen (or Xvfb display)
//
// Run with:
//   SDL_VIDEODRIVER=offscreen dotnet test FSO.LotRenderer.Tests --logger:"console;verbosity=detailed"
// Run only the PerFloorRotation test:
//   SDL_VIDEODRIVER=offscreen dotnet test FSO.LotRenderer.Tests --filter "PerFloorRotation"

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;

namespace FSO.LotRenderer.Tests
{
    public class RendererIntegrationTest
    {
        /// <summary>
        /// Renders lot 2 via the freeso-renderer binary and asserts that the output PNG:
        ///   (a) exists and is at least 10 KB
        ///   (b) has a valid PNG header (first 8 bytes)
        /// </summary>
        [Fact]
        public void RenderLot2_ProducesValidPng()
        {
            var outPath  = Path.Combine(Path.GetTempPath(), $"renderer-test-{Guid.NewGuid():N}.png");
            var apiUrl   = Environment.GetEnvironmentVariable("FSO_RENDERER_API_URL")   ?? "http://workshop:9000";
            var user     = Environment.GetEnvironmentVariable("FSO_RENDERER_USER")      ?? "baron";
            var password = Environment.GetEnvironmentVariable("FSO_RENDERER_PASS")      ?? "test1234";
            var gamePath = Environment.GetEnvironmentVariable("FSO_GAME_LOCATION")
                           ?? "/home/baron/projects/freeso-experiment/GameAssets/";

            // Locate the renderer binary (built alongside the test).
            var rendererBin = FindRendererBinary();
            Assert.True(File.Exists(rendererBin),
                $"freeso-renderer binary not found at: {rendererBin}\n" +
                "Run 'dotnet build' on FSO.LotRenderer first.");

            var psi = new ProcessStartInfo
            {
                FileName               = rendererBin,
                // 16318812 = MapCoordinates.Pack(249, 348) — baron's Main lot (lot_id=2).
                // --debug-lot takes the packed map location, NOT the lot_id.
                Arguments              = $"--api-url {apiUrl} --user {user} --password {password} " +
                                         $"--game-path \"{gamePath}\" --debug-lot 16318812 --out \"{outPath}\"",
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
            };

            // Prefer SDL offscreen; fall back gracefully if DISPLAY is set.
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY")) &&
                string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SDL_VIDEODRIVER")))
            {
                psi.Environment["SDL_VIDEODRIVER"] = "offscreen";
            }

            psi.Environment["FSO_GAME_LOCATION"] = gamePath;

            var proc = Process.Start(psi);
            Assert.NotNull(proc);

            string stdout = proc.StandardOutput.ReadToEnd();
            string stderr = proc.StandardError.ReadToEnd();
            bool finished = proc.WaitForExit(TimeSpan.FromMinutes(5));

            if (!string.IsNullOrEmpty(stdout)) Console.WriteLine("=== stdout ===\n" + stdout);
            if (!string.IsNullOrEmpty(stderr)) Console.WriteLine("=== stderr ===\n" + stderr);

            Assert.True(finished, "freeso-renderer did not finish within 5 minutes.");
            Assert.Equal(0, proc.ExitCode);

            Assert.True(File.Exists(outPath), $"Output PNG not written to {outPath}");

            var bytes = File.ReadAllBytes(outPath);
            Assert.True(bytes.Length >= 10_240,
                $"PNG is too small ({bytes.Length} bytes — expected >= 10 KB). " +
                "Likely blank or corrupt render.");

            // PNG magic bytes: 89 50 4E 47 0D 0A 1A 0A
            byte[] pngMagic = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            for (int i = 0; i < pngMagic.Length; i++)
            {
                Assert.Equal(pngMagic[i], bytes[i]);
            }
        }

        /// <summary>
        /// Regression test for trailing slash normalization (freesoexperiment-944).
        ///
        /// Runs the renderer with FSO_GAME_PATH without a trailing slash and asserts
        /// success + valid PNG output. Before the fix, Path.Combine on Linux would treat
        /// relative FAR3 paths as absolute when the prefix lacked a separator, causing
        /// Content.Init to fail. After the fix, GamePath is normalized to always end with
        /// a separator before being passed to Path.Combine.
        /// </summary>
        [Fact]
        public void RenderLot2_WithGamePathNoTrailingSlash_ProducesValidPng()
        {
            var outPath  = Path.Combine(Path.GetTempPath(), $"renderer-notrail-{Guid.NewGuid():N}.png");
            var apiUrl   = Environment.GetEnvironmentVariable("FSO_RENDERER_API_URL")   ?? "http://workshop:9000";
            var user     = Environment.GetEnvironmentVariable("FSO_RENDERER_USER")      ?? "baron";
            var password = Environment.GetEnvironmentVariable("FSO_RENDERER_PASS")      ?? "test1234";
            // Deliberately omit trailing slash to test normalization.
            var gamePathNoTrail = Environment.GetEnvironmentVariable("FSO_GAME_LOCATION")
                           ?? "/home/baron/projects/freeso-experiment/GameAssets";

            var rendererBin = FindRendererBinary();
            Assert.True(File.Exists(rendererBin),
                $"freeso-renderer binary not found at: {rendererBin}\n" +
                "Run 'dotnet build' on FSO.LotRenderer first.");

            var psi = new ProcessStartInfo
            {
                FileName               = rendererBin,
                Arguments              = $"--api-url {apiUrl} --user {user} --password {password} " +
                                         $"--game-path \"{gamePathNoTrail}\" --debug-lot 16318812 --out \"{outPath}\"",
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
            };

            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY")) &&
                string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SDL_VIDEODRIVER")))
            {
                psi.Environment["SDL_VIDEODRIVER"] = "offscreen";
            }

            psi.Environment["FSO_GAME_LOCATION"] = gamePathNoTrail;

            var proc = Process.Start(psi);
            Assert.NotNull(proc);

            string stdout = proc.StandardOutput.ReadToEnd();
            string stderr = proc.StandardError.ReadToEnd();
            bool finished = proc.WaitForExit(TimeSpan.FromMinutes(5));

            if (!string.IsNullOrEmpty(stdout)) Console.WriteLine("=== stdout ===\n" + stdout);
            if (!string.IsNullOrEmpty(stderr)) Console.WriteLine("=== stderr ===\n" + stderr);

            Assert.True(finished, "freeso-renderer did not finish within 5 minutes.");
            Assert.Equal(0, proc.ExitCode);

            Assert.True(File.Exists(outPath), $"Output PNG not written to {outPath}");

            var bytes = File.ReadAllBytes(outPath);
            Assert.True(bytes.Length >= 10_240,
                $"PNG is too small ({bytes.Length} bytes — expected >= 10 KB). " +
                "Likely blank or corrupt render.");

            // PNG magic bytes: 89 50 4E 47 0D 0A 1A 0A
            byte[] pngMagic = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            for (int i = 0; i < pngMagic.Length; i++)
            {
                Assert.Equal(pngMagic[i], bytes[i]);
            }
        }

        /// <summary>
        /// Regression test for CWD sensitivity (freesoexperiment-785).
        ///
        /// Runs the renderer with WorkingDirectory = /tmp (NOT the binary directory) and asserts
        /// success + valid PNG output.  Before the fix, the renderer used AppContext.BaseDirectory
        /// (which on Linux self-contained binaries returns the process CWD) so it would crash with
        /// DirectoryNotFoundException on "Content/" when invoked from any directory other than
        /// publish/linux-x64/.  After the fix it uses Environment.ProcessPath (the binary's own
        /// directory) so it works from any CWD.
        /// </summary>
        [Fact]
        public void RenderLot2_FromArbitraryCwd_ProducesValidPng()
        {
            var outPath  = Path.Combine(Path.GetTempPath(), $"renderer-cwd-test-{Guid.NewGuid():N}.png");
            var apiUrl   = Environment.GetEnvironmentVariable("FSO_RENDERER_API_URL")   ?? "http://workshop:9000";
            var user     = Environment.GetEnvironmentVariable("FSO_RENDERER_USER")      ?? "baron";
            var password = Environment.GetEnvironmentVariable("FSO_RENDERER_PASS")      ?? "test1234";
            var gamePath = Environment.GetEnvironmentVariable("FSO_GAME_LOCATION")
                           ?? "/home/baron/projects/freeso-experiment/GameAssets/";

            var rendererBin = FindRendererBinary();
            Assert.True(File.Exists(rendererBin),
                $"freeso-renderer binary not found at: {rendererBin}\n" +
                "Run 'dotnet build' on FSO.LotRenderer first.");

            var psi = new ProcessStartInfo
            {
                FileName               = rendererBin,
                Arguments              = $"--api-url {apiUrl} --user {user} --password {password} " +
                                         $"--game-path \"{gamePath}\" --debug-lot 16318812 --out \"{outPath}\"",
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                // Intentionally NOT the binary directory — this is what the regression tests.
                WorkingDirectory       = Path.GetTempPath(),
            };

            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY")) &&
                string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SDL_VIDEODRIVER")))
            {
                psi.Environment["SDL_VIDEODRIVER"] = "offscreen";
            }

            psi.Environment["FSO_GAME_LOCATION"] = gamePath;

            var proc = Process.Start(psi);
            Assert.NotNull(proc);

            string stdout = proc.StandardOutput.ReadToEnd();
            string stderr = proc.StandardError.ReadToEnd();
            bool finished = proc.WaitForExit(TimeSpan.FromMinutes(5));

            if (!string.IsNullOrEmpty(stdout)) Console.WriteLine("=== stdout ===\n" + stdout);
            if (!string.IsNullOrEmpty(stderr)) Console.WriteLine("=== stderr ===\n" + stderr);

            Assert.True(finished, "freeso-renderer did not finish within 5 minutes.");
            Assert.Equal(0, proc.ExitCode);

            Assert.True(File.Exists(outPath), $"Output PNG not written to {outPath}");

            var bytes = File.ReadAllBytes(outPath);
            Assert.True(bytes.Length >= 10_240,
                $"PNG is too small ({bytes.Length} bytes — expected >= 10 KB). " +
                "Likely blank or corrupt render.");

            // PNG magic bytes: 89 50 4E 47 0D 0A 1A 0A
            byte[] pngMagic = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            for (int i = 0; i < pngMagic.Length; i++)
            {
                Assert.Equal(pngMagic[i], bytes[i]);
            }
        }

        /// <summary>
        /// PerFloorRotation — S2 integration test.
        ///
        /// Renders lot 2 at representative combinations of --level / --angle / --zoom,
        /// then asserts:
        ///   (a) each output PNG is >= 10 KB
        ///   (b) pairwise byte-diff between any two outputs is > 1%
        ///       (i.e. each combination produces a visibly distinct image)
        ///
        /// Combination matrix rendered (15 combos):
        ///   Levels:  1, 2          (distinct floors on lot 2 which has 3 stories)
        ///   Angles:  iso-ne, iso-nw, iso-se, iso-sw  (all 4 rotations)
        ///   Zooms:   far, med, near                  (all 3 zoom levels)
        ///
        /// All 4 angles at zoom=far level=1 (4 combos)
        /// All 3 zooms  at angle=iso-ne level=1 (2 additional combos for med + near)
        /// Level 2 at angle=iso-ne zoom=far (1 additional combo for floor variation)
        ///
        /// This covers: all angles, all zooms, multi-level — 7 combos total.
        /// Rationale: running all 4×3×4=48 combos would take ~30 min for one test;
        /// 7 combos cover every parameter dimension at least once.
        ///
        /// Run: dotnet test FSO.LotRenderer.Tests --filter "PerFloorRotation"
        ///
        /// Note (8f1): xUnit 2.x only supports Timeout on async Task methods.  This
        /// test is synchronous (it shells out to the renderer binary and blocks on
        /// Process.WaitForExit) so [Fact(Timeout = ...)] would fail immediately with
        /// "Tests marked with Timeout are only supported for async tests."  Plain [Fact]
        /// is correct here — each individual Process.WaitForExit already has a 5-minute
        /// timeout, and CI's overall job timeout is the outer bound.
        /// </summary>
        [Fact]
        public void PerFloorRotation_AllCombinations_ProduceDistinctValidPngs()
        {
            var rendererBin = FindRendererBinary();
            Assert.True(File.Exists(rendererBin),
                $"freeso-renderer binary not found at: {rendererBin}\n" +
                "Run 'dotnet build' on FSO.LotRenderer first.");

            var apiUrl   = Environment.GetEnvironmentVariable("FSO_RENDERER_API_URL")   ?? "http://workshop:9000";
            var user     = Environment.GetEnvironmentVariable("FSO_RENDERER_USER")      ?? "baron";
            var password = Environment.GetEnvironmentVariable("FSO_RENDERER_PASS")      ?? "test1234";
            var gamePath = Environment.GetEnvironmentVariable("FSO_GAME_LOCATION")
                           ?? "/home/baron/projects/freeso-experiment/GameAssets/";

            // Combination matrix — see doc comment.
            var combos = new (int Level, string Angle, string Zoom)[]
            {
                (1, "iso-ne", "far"),
                (1, "iso-nw", "far"),
                (1, "iso-se", "far"),
                (1, "iso-sw", "far"),
                (1, "iso-ne", "med"),
                (1, "iso-ne", "near"),
                (2, "iso-ne", "far"),
            };

            var tmpDir = Path.Combine(Path.GetTempPath(), $"renderer-perfloor-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tmpDir);

            // Render each combination sequentially (renderer is stateful; each invocation
            // is its own process, so this is safe but not fast).
            var results = new Dictionary<string, byte[]>();
            foreach (var (level, angle, zoom) in combos)
            {
                var key     = $"L{level}_{angle}_{zoom}";
                var outPath = Path.Combine(tmpDir, $"{key}.png");

                Console.WriteLine($"[test] Rendering combo: {key}");
                var psi = new ProcessStartInfo
                {
                    FileName  = rendererBin,
                    Arguments = $"--api-url {apiUrl} --user {user} --password {password} " +
                                $"--game-path \"{gamePath}\" --debug-lot 16318812 " +
                                $"--level {level} --angle {angle} --zoom {zoom} --out \"{outPath}\"",
                    UseShellExecute        = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError  = true,
                };

                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY")) &&
                    string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SDL_VIDEODRIVER")))
                {
                    psi.Environment["SDL_VIDEODRIVER"] = "offscreen";
                }
                psi.Environment["FSO_GAME_LOCATION"] = gamePath;

                var proc = Process.Start(psi);
                Assert.NotNull(proc);

                string stdout = proc.StandardOutput.ReadToEnd();
                string stderr = proc.StandardError.ReadToEnd();
                bool finished = proc.WaitForExit(TimeSpan.FromMinutes(5));

                if (!string.IsNullOrEmpty(stdout)) Console.WriteLine($"[{key}] stdout:\n" + stdout);
                if (!string.IsNullOrEmpty(stderr)) Console.WriteLine($"[{key}] stderr:\n" + stderr);

                Assert.True(finished,  $"[{key}] renderer did not finish within 5 minutes.");
                Assert.Equal(0, proc.ExitCode);
                Assert.True(File.Exists(outPath), $"[{key}] output PNG not written to {outPath}");

                var bytes = File.ReadAllBytes(outPath);
                Assert.True(bytes.Length >= 10_240,
                    $"[{key}] PNG too small ({bytes.Length} bytes — expected >= 10 KB).");

                results[key] = bytes;
            }

            // Pairwise byte-diff check: each pair must differ by > 1% of the larger file's byte count.
            var keys = results.Keys.ToArray();
            var failures = new List<string>();
            for (int i = 0; i < keys.Length - 1; i++)
            {
                for (int j = i + 1; j < keys.Length; j++)
                {
                    var ka = keys[i]; var kb = keys[j];
                    var ba = results[ka]; var bb = results[kb];
                    int compareLen = Math.Min(ba.Length, bb.Length);
                    int diffCount  = 0;
                    for (int k = 0; k < compareLen; k++)
                        if (ba[k] != bb[k]) diffCount++;
                    // Count bytes beyond the shorter array as all-different.
                    diffCount += Math.Abs(ba.Length - bb.Length);
                    double maxLen   = Math.Max(ba.Length, bb.Length);
                    double diffFrac = diffCount / maxLen;
                    if (diffFrac <= 0.01)
                        failures.Add($"Pair ({ka}, {kb}): byte-diff = {diffFrac:P2} — images appear identical.");
                }
            }
            Assert.True(failures.Count == 0,
                "Some rendering combinations produced near-identical output:\n" +
                string.Join("\n", failures));
        }

        /// <summary>
        /// Regression test for freesoexperiment-68d: roofless parameter must produce a
        /// visually different PNG from the with-roof render.
        ///
        /// Before the fix, RenderAsync did not pass 'roofless' to RenderFSOFAt, so both
        /// roofless=true and roofless=false produced identical PNGs (with-roof always).
        /// The cache key already differed, so no cache collision — just wrong images.
        ///
        /// This test renders the same lot+level+angle+zoom twice via --serve HTTP:
        ///   (a) roofless=false — with roof
        ///   (b) roofless=true  — without roof
        /// and asserts that the resulting PNGs have different MD5 hashes.
        ///
        /// Runs against a live renderer (FSO_RENDERER_URL) and live FSO server.
        /// Skip via FSO_SKIP_ROOFLESS_TEST=1 if renderer is not available.
        /// </summary>
        [Fact]
        public async Task Roofless_ProducesDifferentPngFromWithRoof()
        {
            if (Environment.GetEnvironmentVariable("FSO_SKIP_ROOFLESS_TEST") == "1")
            {
                Assert.Skip("FSO_SKIP_ROOFLESS_TEST=1");
            }

            var rendererUrl = Environment.GetEnvironmentVariable("FSO_RENDERER_URL") ?? "http://127.0.0.1:9101";

            // We need the renderer to be running as a --serve instance.
            // Try a health check first; skip if it's not running.
            try
            {
                using var hc = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(3) };
                var healthResp = await hc.GetAsync($"{rendererUrl}/health");
                if (!healthResp.IsSuccessStatusCode)
                {
                    Assert.Skip($"Renderer health check failed ({healthResp.StatusCode}). Set FSO_RENDERER_URL or start: freeso-renderer --serve --port 9101");
                }
            }
            catch (Exception ex)
            {
                Assert.Skip($"Renderer not reachable at {rendererUrl}: {ex.Message}. Set FSO_RENDERER_URL or start: freeso-renderer --serve --port 9101");
            }

            // Use lot 2 (baron's Main). Provide lot_location directly to avoid DB dependency.
            // 16318812 = MapCoordinates.Pack(249, 348)
            const string basePayload = @"""shard"":""Alphaville"",""lot_id"":2,""lot_location"":16318812,""level"":1,""angle"":""iso-ne"",""zoom"":""far""";

            using var client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromMinutes(5) };

            // Render with-roof first.
            Console.WriteLine("[test] Rendering with-roof (roofless=false)...");
            var withRoofResp = await client.PostAsync(
                $"{rendererUrl}/render",
                new System.Net.Http.StringContent(
                    $"{{{basePayload},\"roofless\":false}}",
                    System.Text.Encoding.UTF8, "application/json"));
            Assert.True(withRoofResp.IsSuccessStatusCode,
                $"with-roof render failed: {withRoofResp.StatusCode}\n{await withRoofResp.Content.ReadAsStringAsync()}");
            var withRoofJson = await withRoofResp.Content.ReadAsStringAsync();
            Console.WriteLine($"  with-roof response: {withRoofJson}");

            // Render roofless.
            Console.WriteLine("[test] Rendering roofless (roofless=true)...");
            var rooflessResp = await client.PostAsync(
                $"{rendererUrl}/render",
                new System.Net.Http.StringContent(
                    $"{{{basePayload},\"roofless\":true}}",
                    System.Text.Encoding.UTF8, "application/json"));
            Assert.True(rooflessResp.IsSuccessStatusCode,
                $"roofless render failed: {rooflessResp.StatusCode}\n{await rooflessResp.Content.ReadAsStringAsync()}");
            var rooflessJson = await rooflessResp.Content.ReadAsStringAsync();
            Console.WriteLine($"  roofless response: {rooflessJson}");

            // Extract paths from JSON responses.
            var withRoofPath = System.Text.Json.JsonDocument.Parse(withRoofJson)
                .RootElement.GetProperty("path").GetString();
            var rooflessPath = System.Text.Json.JsonDocument.Parse(rooflessJson)
                .RootElement.GetProperty("path").GetString();

            Assert.True(File.Exists(withRoofPath!), $"with-roof PNG not found at {withRoofPath}");
            Assert.True(File.Exists(rooflessPath!), $"roofless PNG not found at {rooflessPath}");

            // Compute MD5 of each file.
            static string Md5(string path)
            {
                using var s = File.OpenRead(path);
                using var md5 = System.Security.Cryptography.MD5.Create();
                return BitConverter.ToString(md5.ComputeHash(s)).Replace("-", "").ToLowerInvariant();
            }

            var withRoofMd5 = Md5(withRoofPath!);
            var rooflessMd5 = Md5(rooflessPath!);
            var withRoofSize = new FileInfo(withRoofPath!).Length;
            var rooflessSize = new FileInfo(rooflessPath!).Length;

            Console.WriteLine($"  with-roof : {withRoofPath} ({withRoofSize} bytes, md5={withRoofMd5})");
            Console.WriteLine($"  roofless  : {rooflessPath} ({rooflessSize} bytes, md5={rooflessMd5})");

            // The two PNGs must be different — roofless removes the roof pixels.
            Assert.NotEqual(withRoofMd5, rooflessMd5);
            Console.WriteLine("[test] PASS: roofless and with-roof PNGs have different MD5 hashes.");
        }

        private static string FindRendererBinary()
        {
            // 1. Explicit override via env var (useful in CI).
            var envBin = Environment.GetEnvironmentVariable("FSO_RENDERER_BIN");
            if (!string.IsNullOrEmpty(envBin) && File.Exists(envBin)) return envBin;

            // 2. Search ancestor directories for a publish output (self-contained preferred).
            var testDir = Path.GetDirectoryName(typeof(RendererIntegrationTest).Assembly.Location)!;
            var dir = new DirectoryInfo(testDir);
            while (dir != null)
            {
                // Self-contained publish (linux-x64)
                var p = Path.Combine(dir.FullName, "FSO.LotRenderer", "publish", "linux-x64", "freeso-renderer");
                if (File.Exists(p)) return p;
                // Release build (framework-dependent, needs matching runtime)
                p = Path.Combine(dir.FullName, "FSO.LotRenderer", "bin", "Release", "net9.0", "freeso-renderer");
                if (File.Exists(p)) return p;
                p = Path.Combine(dir.FullName, "FSO.LotRenderer", "bin", "Debug", "net9.0", "freeso-renderer");
                if (File.Exists(p)) return p;
                dir = dir.Parent;
            }

            // 3. Fallback: binary next to test assembly (will fail the assertion with a meaningful message).
            return Path.Combine(testDir, "freeso-renderer");
        }
    }
}
