using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Manifestation
{
    /// <summary>
    /// A manifest file is a file that has a version and references a bunch of patch files.
    /// </summary>
    class ManifestFile
    {
        public ManifestFile(string Path, string Version, List<PatchFile> PatchFiles)
        {
            bool HasURLs = false;
            BinaryWriter Writer = new BinaryWriter(File.Create(Path));
            Writer.Write((string)Version);
            Writer.Write((int)PatchFiles.Count);

            if (PatchFiles[0].URL != "")
                HasURLs = true;

            foreach (PatchFile PFile in PatchFiles)
            {
                if (!HasURLs)
                    Writer.Write((string)PFile.Address + "," + PFile.FileHash);
                else
                    Writer.Write((string)PFile.Address + "," + PFile.FileHash + "," + PFile.URL);
            }

            Writer.Flush();
            Writer.Close();
        }
    }
}
