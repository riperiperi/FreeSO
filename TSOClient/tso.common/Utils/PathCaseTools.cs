using System.IO;
using System.Linq;

namespace FSO.Common.Utils
{
    public static class PathCaseTools
    {
        public static string Insensitive(string file)
        {
            var dir = Directory.GetFiles(Path.GetDirectoryName(file));
            return dir.FirstOrDefault(x => x.ToLowerInvariant().Replace('\\', '/') == file.ToLowerInvariant().Replace('\\', '/'));
        }
    }
}
