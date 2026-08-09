using System;
using System.Diagnostics;
using System.IO;
using FSO.Content;
using FSO.Content.Interfaces;
using FSO.Files.Formats.IFF;

namespace FSO.Client.UI.Panels
{
    /// <summary>
    /// Registers a just-compiled object into the running game, so a player who asked for
    /// something can place it without restarting.
    ///
    /// Two registrations are needed and they are separate systems:
    ///   * ChangeManager.RegisterObjects — makes the GUID resolvable by the VM and renderer.
    ///   * WorldObjectCatalog.AddLive — puts it in Buy Mode, which builds its category lists
    ///     once at startup and would otherwise never see an object compiled after boot.
    ///
    /// Registering only the first produces an object that exists but cannot be bought;
    /// only the second, a catalog entry that fails when placed.
    /// </summary>
    public static class LiveObjectRegistrar
    {
        /// <summary>
        /// Returns false when the object could not be registered, having logged why. Callers
        /// should tell the player nothing arrived rather than claim success — a catalog entry
        /// the game can't place is worse than an honest failure.
        /// </summary>
        public static bool Register(uint guid, string iffPath, string name, sbyte category, uint price)
        {
            if (string.IsNullOrEmpty(iffPath) || !File.Exists(iffPath))
            {
                System.Diagnostics.Debug.WriteLine($"[live] no .iff at \"{iffPath}\" for 0x{guid:X8}");
                return false;
            }

            try
            {
                var iff = new IffFile(iffPath);
                iff.InitHash();
                iff.RuntimeInfo.Path = iffPath;
                // Standalone: loaded from a loose file rather than a FAR archive, so sprites
                // must resolve from within this .iff — which is why the compiler inlines them.
                iff.RuntimeInfo.State = IffRuntimeState.Standalone;

                Content.Content.Get().Changes.RegisterObjects(iff);

                WorldObjectCatalog.AddLive(new ObjectCatalogItem
                {
                    GUID = guid,
                    Category = category,
                    Price = price,
                    Name = name ?? "",
                    Tags = "",
                });

                System.Diagnostics.Debug.WriteLine($"[live] registered 0x{guid:X8} \"{name}\" from {iffPath}");
                return true;
            }
            catch (Exception e)
            {
                // A malformed object must not take the game down mid-session.
                System.Diagnostics.Debug.WriteLine($"[live] failed to register 0x{guid:X8}: {e}");
                return false;
            }
        }
    }
}
