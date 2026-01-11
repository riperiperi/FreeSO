using System;
using System.IO;
using System.Linq;

namespace FSO.Common.Utils
{
    public static class PathCaseTools
    {
        /// <summary>
        /// Resolves a file path case-insensitively on Linux/macOS.
        /// On Windows, simply checks if the file exists.
        /// </summary>
        public static string Insensitive(string file)
        {
            if (string.IsNullOrEmpty(file))
                return null;

            // On Windows, file system is case-insensitive, just check existence
            if (OperatingSystem.IsWindows())
                return File.Exists(file) ? file : null;

            file = file.Replace('\\', '/');

            string[] parts;
            string resolved;

            if (file.StartsWith("/"))
            {
                parts = file.Substring(1).Split('/');
                resolved = "/";
            }
            else
            {
                parts = file.Split('/');
                resolved = "";
            }

            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part))
                    continue;

                var searchPath = string.IsNullOrEmpty(resolved) ? "." : resolved;

                if (!Directory.Exists(searchPath))
                    return null;

                try
                {
                    var entries = Directory.GetFileSystemEntries(searchPath);
                    var match = entries.FirstOrDefault(e =>
                        Path.GetFileName(e).Equals(part, StringComparison.OrdinalIgnoreCase));

                    if (match == null)
                        return null;

                    resolved = match;
                }
                catch
                {
                    return null;
                }
            }

            return File.Exists(resolved) ? resolved : null;
        }
    }
}
