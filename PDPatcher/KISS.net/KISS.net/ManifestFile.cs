using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace KISS
{
    /// <summary>
    /// A manifest file is a file that has a version and references a bunch of patch files.
    /// </summary>
    public class ManifestFile
    {
        public string Version = "";
        public List<PatchFile> PatchFiles = new List<PatchFile>();

        public ManifestFile(string Path, string Version, List<PatchFile> PatchFiles)
        {
            bool HasURLs = false;
            BinaryWriter Writer = new BinaryWriter(File.Create(Path));
            Writer.Write((string)Version);

            if (PatchFiles[0].URL != "")
                HasURLs = true;

            foreach (PatchFile PFile in PatchFiles)
            {
                if (!HasURLs)
                    Writer.Write((string)PFile.Address + "," + PFile.FileHash);
                else
                    Writer.Write((string)PFile.Address + "," + PFile.FileHash + PFile.URL);
            }

            Writer.Flush();
            Writer.Close();
        }

        /// <summary>
        /// Creates a ManifestFile instance from a downloaded stream.
        /// </summary>
        /// <param name="ManifestStream"></param>
        public ManifestFile(Stream ManifestStream)
        {
            BinaryReader Reader = new BinaryReader(ManifestStream);
            Reader.BaseStream.Position = 0; //IMPORTANT!
            
            Version = Reader.ReadString();
            int NumFiles = Reader.ReadInt32();

            for(int i = 0; i < NumFiles; i++)
            {
                string PatchFileStr = Reader.ReadString();
                string[] SplitPatchFileStr = PatchFileStr.Split(",".ToCharArray());

                PatchFiles.Add(new PatchFile()
                {
                    Address = SplitPatchFileStr[0],
                    FileHash = SplitPatchFileStr[1],
                    URL = SplitPatchFileStr[2]
                });
            }

            Reader.Close();
        }
    }
}
