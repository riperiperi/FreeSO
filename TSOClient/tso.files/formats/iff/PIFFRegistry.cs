using FSO.Files.Formats.IFF.Chunks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FSO.Files.Formats.IFF
{
    public static class PIFFRegistry
    {
        private static Dictionary<string, List<IffFile>> PIFFsByName;
        private static Dictionary<string, bool> IsPIFFUser; //if a piff is User, all other piffs for that file are ignored.

        public static void Init(string basePath)
        {
            PIFFsByName = new Dictionary<string, List<IffFile>>();
            IsPIFFUser = new Dictionary<string, bool>();

            //Directory.CreateDirectory(basePath);
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
        }

        public static List<IffFile> GetPIFFs(string srcFile)
        {
            List<IffFile> result = null;
            PIFFsByName.TryGetValue(srcFile, out result);
            return result;
        }
    }
}
