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

        public static void Init(string basePath)
        {
            //@"Content\Patch\"
            PIFFsByName = new Dictionary<string, List<IffFile>>();

            string[] paths = Directory.GetFiles(basePath, "*.piff", SearchOption.AllDirectories);
            for (int i = 0; i < paths.Length; i++)
            {
                string entry = paths[i];
                string filename = Path.GetFileName(entry);
                IffFile piffFile = new IffFile(entry);
                PIFF piff = piffFile.List<PIFF>()[0];
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
