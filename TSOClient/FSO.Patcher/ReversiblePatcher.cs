using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Patcher
{
    public class ReversiblePatcher
    {
        public List<string> FileChanges;
        public HashSet<string> Extracted;
        public HashSet<ZipArchiveEntry> ToExtract;
        public List<string> Errors;

        public ZipArchive Archive;
        public event Action<string, float> OnStatus;

        public int Total;

        public ReversiblePatcher(ZipArchive zip)
        {
            Archive = zip;
            ToExtract = new HashSet<ZipArchiveEntry>(zip.Entries);
            Total = ToExtract.Count;
            Extracted = new HashSet<string>();

            try
            {
                Directory.Delete("updateBackup/", true);
            }
            catch (Exception)
            {

            }
            Directory.CreateDirectory("updateBackup/");
        }

        public static HashSet<string> IgnoreFiles = new HashSet<string>()
        {
            //"updater.exe",
            "Content/config.ini",
            "NLog.config",
            "update.pdb"
        };

        public static HashSet<string> UnimportantFiles = new HashSet<string>()
        {
            "discord-rpc.dll"
        };

        public static HashSet<string> DeferredUpdate = new HashSet<string>()
        {

        };

        private void Status(string message)
        {
            OnStatus?.Invoke(message, 1f - (ToExtract.Count / (float)Total));
        }

        public List<string> GetIncompleteFiles()
        {
            return ToExtract.Select(file => file.FullName).Where(name => !UnimportantFiles.Contains(name)).ToList();
        }

        public async Task<bool> ExtractEntry(ZipArchiveEntry entry, int tryNum)
        {
            var name = (entry.FullName == "update.exe") ? "update2.exe" : entry.FullName;
            var targPath = Path.Combine("./", name);
            Directory.CreateDirectory(Path.GetDirectoryName(targPath));
            try
            {
                if (File.Exists(targPath) && tryNum == 0)
                {
                    //copy to backup folder
                    var backupPath = Path.Combine("updateBackup/", targPath);
                    Directory.CreateDirectory(Path.GetDirectoryName(backupPath));
                    File.Copy(targPath, backupPath, true);
                }
                entry.ExtractToFile(targPath, true);
                Status(name + " Extracted...");
                Extracted.Add(targPath);
                return true;
            }
            catch (Exception e)
            {
                if (e is DirectoryNotFoundException) return true;
                if (tryNum++ > 3)
                {
                    Status($"Could not replace {targPath}!");
                    Errors.Add($"{targPath}: {e.Message}");
                    return false;
                }
                else
                {
                    Status($"Waiting for {name} ({tryNum}/4)... {e.ToString()}");
                    await Task.Delay(3000);
                    return await ExtractEntry(entry, tryNum);
                }

            }
        }

        public static int RENAME_MAX_ATTEMPTS = 5;

        public async Task<bool> AttemptRename(int renameRetry)
        {
            try
            {
                File.Delete("FreeSO.exe.old");
                if (File.Exists("FreeSO.exe"))  //shouldn't be in use, unless the user has incorrectly renamed and run the freeso executable
                    File.Move("FreeSO.exe", "FreeSO.exe.old");
            }
            catch (Exception)
            {
                if (renameRetry++ < RENAME_MAX_ATTEMPTS)
                {
                    Status($"Waiting for FreeSO to Close ({renameRetry}/{RENAME_MAX_ATTEMPTS})...");
                    await Task.Delay(2000);
                    return await AttemptRename(renameRetry);
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        public async Task AttemptExtract()
        {
            Errors = new List<string>();
            //file being replaced? 
            var clone = ToExtract.ToList();
            foreach (var entry in clone)
            {
                if (IgnoreFiles.Contains(entry.FullName))
                {
                    ToExtract.Remove(entry);
                    continue;
                }
                var result = await ExtractEntry(entry, 0);
                if (result)
                {
                    ToExtract.Remove(entry);
                }
            }
        }

        public bool Revert()
        {
            bool success = true;
            foreach (var file in Extracted)
            {
                var backupPath = Path.Combine("updateBackup/", file);
                try
                {
                    Status($"Restoring backup for {file}...");
                    File.Copy(backupPath, file, true);
                }
                catch (FileNotFoundException)
                {
                    Status($"Backup for {file} not found, skipping...");
                }
                catch (Exception e)
                {
                    Status($"Could not restore backup for {file}: {e.Message}");
                    Errors.Add($"{file}: {e.Message}");
                    success = false;
                }
            }
            if (success)
            {
                try
                {
                    Directory.Delete("updateBackup/", true);
                }
                catch
                {
                    //can't delete backup for some reason. just ignore.
                }
            }
            return success;
        }

        public void Final()
        {
            try
            {
                Directory.Delete("updateBackup/", true);
            }
            catch
            {
                //can't delete backup for some reason. just ignore.
            }
            Archive.Dispose();
        }
    }
}
