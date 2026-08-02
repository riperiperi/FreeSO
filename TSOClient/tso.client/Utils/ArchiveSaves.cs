using FSO.Client.Model.Archive;
using FSO.Common;

namespace FSO.Client.Utils
{
    internal static class ArchiveSaves
    {
        public static List<ArchiveManifest> ListManifests(bool template = false)
        {
            string[] dirs = Directory.GetDirectories(Path.Combine(FSOEnvironment.ContentDir, "ArchiveCities"));

            var manifests = new List<ArchiveManifest>();

            foreach (string dir in dirs)
            {
                if (File.Exists(Path.Combine(dir, "archive.ini")))
                {
                    try
                    {
                        var manifest = new ArchiveManifest(Path.Combine(dir, "archive.ini"));

                        if (string.IsNullOrEmpty(manifest.LocalDir))
                        {
                            // Try correct it to the default directory `data/` if it exists.

                            if (File.Exists(Path.Combine(dir, "data/fsoarchive.db")))
                            {
                                manifest.LocalDir = "data/";
                            }
                        }

                        if (manifest.Template == template && (manifest.LocalDir != "" || manifest.ZipLocation != ""))
                        {
                            manifests.Add(manifest);
                        }
                    }
                    catch (Exception)
                    {
                        // Just ignore it.
                    }
                }
            }

            return manifests;
        }
    }
}
