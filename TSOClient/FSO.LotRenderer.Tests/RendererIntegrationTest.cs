// Integration test: renders lot 2 from the live FSO server and asserts a valid PNG is written.
//
// Requirements:
//   FSO server up at FSO_RENDERER_API_URL (default http://workshop:9000)
//   Game assets at FSO_GAME_LOCATION (default /home/baron/projects/freeso-experiment/GameAssets/)
//   SDL_VIDEODRIVER=offscreen (or Xvfb display)
//
// Run with:
//   SDL_VIDEODRIVER=offscreen dotnet test FSO.LotRenderer.Tests --logger:"console;verbosity=detailed"

using System;
using System.Diagnostics;
using System.IO;
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
