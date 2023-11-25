using System;
using System.Collections.Generic;
using System.IO;
using xxHashSharp;

namespace FSO.Files.Utils
{
    public class DiffGenerator
    {
        public HashSet<string> SourceFiles; //aka before
        public HashSet<string> DestFiles; //aka after

        public HashSet<string> AddFiles;
        public HashSet<string> RemovedFiles;
        public HashSet<string> SameFiles;

        public string SourcePath;
        public string DestPath;

        public static List<FileDiff> GetDiffs(string sourcePath, string destPath)
        {
            var sourceFiles = new HashSet<string>();
            RecursiveDirectoryScan(sourcePath, sourceFiles, sourcePath);

            var destFiles = new HashSet<string>();
            RecursiveDirectoryScan(destPath, destFiles, destPath);

            var addFiles = new HashSet<string>(destFiles);
            addFiles.ExceptWith(sourceFiles);

            var removedFiles = new HashSet<string>(sourceFiles);
            removedFiles.ExceptWith(destFiles);

            var sameFiles = new HashSet<string>(sourceFiles);
            sameFiles.IntersectWith(destFiles);

            var diffs = new List<FileDiff>();

            foreach (var removed in removedFiles)
            {
                var bytes = GetFileBytes(Path.Combine(sourcePath, removed));
                var hash = xxHash.CalculateHash(bytes).ToString("x8");
                diffs.Add(new FileDiff(FileDiffType.Remove, removed, hash, null));
            }

            foreach (var added in addFiles)
            {
                var bytes = GetFileBytes(Path.Combine(destPath, added));
                var hash = xxHash.CalculateHash(bytes).ToString("x8");
                diffs.Add(new FileDiff(FileDiffType.Add, added, null, hash));
            }

            foreach (var same in sameFiles)
            {
                var bytesBefore = GetFileBytes(Path.Combine(sourcePath, same));
                var bytesAfter = GetFileBytes(Path.Combine(destPath, same));
                var hashBefore = xxHash.CalculateHash(bytesBefore).ToString("x8");
                var hashAfter = xxHash.CalculateHash(bytesAfter).ToString("x8");
                diffs.Add(new FileDiff(
                    (hashBefore == hashAfter) ? FileDiffType.Unchanged : FileDiffType.Modify, 
                    same, hashBefore, hashAfter));
            }

            return diffs;
        }

        private static byte[] GetFileBytes(string path)
        {
            using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var memory = new MemoryStream();
                stream.CopyTo(memory);
                var bytes = memory.ToArray();
                return bytes;
            }
        }

        private static void RecursiveDirectoryScan(string folder, HashSet<string> fileNames, string basePath)
        {
            var files = Directory.GetFiles(folder);
            foreach (var file in files)
            {
                fileNames.Add(GetRelativePath(basePath, file));
            }

            var dirs = Directory.GetDirectories(folder);
            foreach (var dir in dirs)
            {
                RecursiveDirectoryScan(dir, fileNames, basePath);
            }
        }

        private static string GetRelativePath(string relativeTo, string path)
        {
            if (!(relativeTo.EndsWith("/") || relativeTo.EndsWith("\\"))) relativeTo += "/";
            var uri = new Uri(relativeTo);

            var rel = Uri.UnescapeDataString(uri.MakeRelativeUri(new Uri(path)).ToString()).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            if (rel.Contains(Path.DirectorySeparatorChar.ToString()) == false)
            {
                rel = $".{ Path.DirectorySeparatorChar }{ rel }";
            }
            return rel;
        }
    }

    public class FileDiff
    {
        public FileDiffType DiffType;
        public string Path;
        public string BeforeHash;
        public string AfterHash;

        public FileDiff(FileDiffType type, string path, string beforeHash, string afterHash)
        {
            DiffType = type;
            Path = path;
            BeforeHash = beforeHash;
            AfterHash = afterHash;
        }
    }

    public enum FileDiffType
    {
        Add, //after hash only
        Modify,
        Remove, //before hash only
        Unchanged
    }
}
