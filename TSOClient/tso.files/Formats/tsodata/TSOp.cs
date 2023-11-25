using FSO.Files.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using deltaq;

namespace TSOVersionPatcher
{
    public class TSOp
    {
        public List<FileEntry> Patches;
        public List<FileEntry> Additions;
        public List<string> Deletions;

        private Stream Str;

        public TSOp(Stream str)
        {
            Str = str;
            using (var io = IoBuffer.FromStream(str, ByteOrder.LITTLE_ENDIAN))
            {
                var magic = io.ReadCString(4);
                if (magic != "TSOp")
                    throw new Exception("Not a TSO patch file!");
                var version = io.ReadInt32();

                var ips = io.ReadCString(4);
                if (ips != "IPS_")
                    throw new Exception("Invalid Patch Chunk!");

                var patchCount = io.ReadInt32();
                Patches = new List<FileEntry>();
                for (int i = 0; i < patchCount; i++)
                {
                    Patches.Add(new FileEntry()
                    {
                        FileTarget = io.ReadVariableLengthPascalString().Replace('\\', '/'),
                        Length = io.ReadInt32(),
                        Offset = str.Position
                    });
                    str.Seek(Patches.Last().Length, SeekOrigin.Current);
                }


                var add = io.ReadCString(4);
                if (add != "ADD_")
                    throw new Exception("Invalid Addition Chunk!");

                var addCount = io.ReadInt32();
                Additions = new List<FileEntry>();
                for (int i = 0; i < addCount; i++)
                {
                    Additions.Add(new FileEntry()
                    {
                        FileTarget = io.ReadVariableLengthPascalString().Replace('\\', '/'),
                        Length = io.ReadInt32(),
                        Offset = str.Position
                    });
                    str.Seek(Additions.Last().Length, SeekOrigin.Current);
                }

                var del = io.ReadCString(4);
                if (del != "DEL_")
                    throw new Exception("Invalid Deletion Chunk!");

                var delCount = io.ReadInt32();
                Deletions = new List<string>();
                for (int i = 0; i < delCount; i++)
                {
                    Deletions.Add(io.ReadVariableLengthPascalString());
                }
            }
        }

        private void RecursiveDirectoryScan(string folder, HashSet<string> fileNames, string basePath)
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

        private string GetRelativePath(string relativeTo, string path)
        {
            if (relativeTo.EndsWith("/") || relativeTo.EndsWith("\\")) relativeTo += "/";
            var uri = new Uri(relativeTo);
            var rel = Uri.UnescapeDataString(uri.MakeRelativeUri(new Uri(path)).ToString()).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            if (rel.Contains(Path.DirectorySeparatorChar.ToString()) == false)
            {
                rel = $".{ Path.DirectorySeparatorChar }{ rel }";
            }
            return rel;
        }

        public void Apply(string source, string dest, Action<string, float> progress)
        {
            try
            {
                if (source != dest)
                {
                    progress("Copying Unchanged Files...", 0);
                    //if our destination folder is different,
                    //copy unchanged files first.
                    var sourceFiles = new HashSet<string>();
                    RecursiveDirectoryScan(source, sourceFiles, source);

                    sourceFiles.ExceptWith(new HashSet<string>(Patches.Select(x => x.FileTarget)));
                    sourceFiles.ExceptWith(new HashSet<string>(Deletions));

                    foreach (var file in sourceFiles)
                    {
                        var destP = Path.Combine(dest, file);
                        Directory.CreateDirectory(Path.GetDirectoryName(destP));

                        File.Copy(Path.Combine(source, file), destP);
                    }
                }

                var reader = new BinaryReader(Str);
                int total = Patches.Count + Additions.Count + Deletions.Count;
                int fileNum = 0;
                foreach (var patch in Patches)
                {
                    progress($"Patching {patch.FileTarget}...", fileNum / (float)total);
                    var path = Path.Combine(source, patch.FileTarget);
                    var dpath = Path.Combine(dest, patch.FileTarget);
                    var data = File.ReadAllBytes(path);
                    Directory.CreateDirectory(Path.GetDirectoryName(dpath));

                    Str.Seek(patch.Offset, SeekOrigin.Begin);
                    var patchd = reader.ReadBytes(patch.Length);
                    BsPatch.Apply(data, patchd, File.Open(dpath, FileMode.Create, FileAccess.Write, FileShare.None));
                    fileNum++;
                }

                foreach (var add in Additions)
                {
                    progress($"Adding {add.FileTarget}...", fileNum / (float)total);
                    var dpath = Path.Combine(dest, add.FileTarget);
                    Directory.CreateDirectory(Path.GetDirectoryName(dpath));

                    Str.Seek(add.Offset, SeekOrigin.Begin);
                    var addData = reader.ReadBytes(add.Length);
                    File.WriteAllBytes(dpath, addData);
                    fileNum++;
                }

                foreach (var del in Deletions)
                {
                    try
                    {
                        progress($"Deleting {del}...", fileNum / (float)total);
                        File.Delete(Path.Combine(dest, del));
                        fileNum++;
                    }
                    catch
                    {
                        //file not found. not important - we wanted it deleted anyways.
                    }
                }
            }
            catch (Exception e)
            {
                progress(e.ToString(), -1f);
            }
        }
    }

    public class FileEntry
    {
        public string FileTarget;
        public long Offset;
        public int Length;
    }
}
