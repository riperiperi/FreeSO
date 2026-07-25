namespace FSO.Common.Utils
{
    public static class PathUtils
    {
        private static bool PathIsChild(string parent, string child)
        {
            return Path.GetFullPath(child).StartsWith(Path.GetFullPath(parent));
        }

        public static string SafeCombine(string basePath, string relative)
        {
            var result = Path.Join(basePath, relative);

            if (!PathIsChild(basePath, result))
            {
                throw new UnauthorizedAccessException($"Path '{relative}' is not a child directory.");
            }

            return result;
        }
    }
}
