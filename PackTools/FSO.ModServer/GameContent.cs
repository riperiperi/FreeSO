using System;
using System.IO;

namespace FSO.ModServer
{
    /// <summary>
    /// Locates the TSO content directory the compiler and VM harness both need. Kept in one
    /// place because getting it wrong fails silently in two different ways: the compiler
    /// emits sprite-less (invisible) objects, and the harness skips its run entirely.
    /// </summary>
    internal static class GameContent
    {
        /// <summary>
        /// Explicit override, then FSO_VM_GAME_LOCATION, then the default install path.
        /// Returns null when nothing exists on disk, so callers can decide whether that's
        /// fatal (running the VM) or merely degraded (compiling without sprites).
        /// </summary>
        public static string ResolveDir(string overrideDir = null)
        {
            var dir = overrideDir;
            if (string.IsNullOrEmpty(dir)) dir = Environment.GetEnvironmentVariable("FSO_VM_GAME_LOCATION");
            if (string.IsNullOrEmpty(dir))
                // UserProfile, not Personal: .NET 8 on macOS maps Personal to ~/Documents,
                // which silently misses ~/Library/Application Support.
                dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Library/Application Support/The Sims Online/TSOClient");
            return Directory.Exists(dir) ? dir : null;
        }

        public static string DefaultPathForMessage() =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library/Application Support/The Sims Online/TSOClient");
    }
}
