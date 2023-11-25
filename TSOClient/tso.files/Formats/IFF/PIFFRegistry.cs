using FSO.Files.Formats.IFF.Chunks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FSO.Files.Formats.IFF
{
    public static class PIFFRegistry
    {
        private static Dictionary<string, List<IffFile>> PIFFsByName = new Dictionary<string, List<IffFile>>();
        private static Dictionary<string, string> OtfRewrite = new Dictionary<string, string>();
        private static Dictionary<string, bool> IsPIFFUser = new Dictionary<string, bool>(); //if a piff is User, all other piffs for that file are ignored.
        private static HashSet<string> OBJDAdded = new HashSet<string>();

        public static void Init(string basePath)
        {
            if (!Directory.Exists(basePath)) return;
            string[] paths = Directory.GetFiles(basePath, "*.piff", SearchOption.AllDirectories);
            for (int i = 0; i < paths.Length; i++)
            {
                string entry = paths[i].Replace('\\', '/');
                bool user = entry.Contains("User/");
                string filename = Path.GetFileName(entry);

                PIFF piff;
                IffFile piffFile;
                try
                {
                    piffFile = new IffFile(entry);
                    piff = piffFile.List<PIFF>()[0];
                }
                catch (Exception)
                {
                    continue;
                }

                if (piff.Version < 2)
                {
                    piff.AppendAddedChunks(piffFile);
                }

                if (IsPIFFUser.ContainsKey(piff.SourceIff))
                {
                    var old = IsPIFFUser[piff.SourceIff];
                    if (old != user)
                    {
                        if (user)
                        {
                            //remove old piffs, as they have been overwritten by this user piff.
                            PIFFsByName[piff.SourceIff].Clear();
                            IsPIFFUser[piff.SourceIff] = true;
                        }
                        else continue; //a user piff exists. ignore these ones.
                    }
                }
                else IsPIFFUser.Add(piff.SourceIff, user);

                if (!PIFFsByName.ContainsKey(piff.SourceIff)) PIFFsByName.Add(piff.SourceIff, new List<IffFile>());
                PIFFsByName[piff.SourceIff].Add(piffFile);
            }

            string[] otfs = Directory.GetFiles(basePath, "*.otf", SearchOption.AllDirectories);

            foreach (var otf in otfs)
            {
                string entry = otf.Replace('\\', '/');
                OtfRewrite[Path.GetFileName(entry)] = entry;
            }

            foreach (var piffs in PIFFsByName)
            {
                foreach (var piff in piffs.Value)
                {
                    var addedOBJD = piff.List<OBJD>();
                    if (addedOBJD != null)
                    {
                        OBJDAdded.Add(piffs.Key);
                        continue;
                    }

                    var pChunk = piff.List<PIFF>()?.FirstOrDefault();
                    if (pChunk != null && pChunk.Entries.Any(x => x.Type == "OBJD"))
                    {
                        OBJDAdded.Add(piffs.Key); 
                        continue;
                    }
                }
            }
        }

        public static HashSet<string> GetOBJDRewriteNames()
        {
            return OBJDAdded;
        }

        public static string GetOTFRewrite(string srcFile)
        {
            string result = null;
            OtfRewrite.TryGetValue(srcFile, out result);
            return result;
        }

        public static List<IffFile> GetPIFFs(string srcFile)
        {
            List<IffFile> result = null;
            PIFFsByName.TryGetValue(srcFile, out result);
            return result;
        }
    }
}
