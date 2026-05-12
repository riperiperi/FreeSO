using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Net;

namespace FSO.Patcher
{
    public class CLIPatcher
    {
        private string[] Args;
        private List<string> Path;
        private int PathProgress = 0;
        private ReversiblePatcher CurrentPatcher;
        private bool AllowMonogameMod;
        private bool CleanPatch;
        public CLIPatcher(List<string> extractPath, string[] args)
        {
            Path = extractPath;
            Args = args;
        }

        private void FSONotClosed()
        {
            Console.WriteLine("Could not update FreeSO as write access could not be gained to the game files. Try running update.exe as an administrator.");
            Cleanup();
            Environment.Exit(0);
        }

        private void FileMissing(string path)
        {
            Console.WriteLine($"A file has been removed while advancing through the update chain ({path}). The update must now be aborted.");
            Cleanup();
            Environment.Exit(0);
        }

        private void FileCorrupt(string path)
        {
            Console.WriteLine($"An update archive was corrupt({ path}). The update must now be aborted.");
            Cleanup();
            Environment.Exit(0);
        }

        private void Cleanup()
        {
            try
            {
                if (File.Exists("FreeSO.exe.old"))
                    File.Move("FreeSO.exe.old", "FreeSO.exe");
            }
            catch (Exception)
            {

            }
        }

        // Apply a release's CLEANUP.txt — one relative path per line, each
        // gets unlinked from the local install before extraction proceeds.
        // Used to nuke files that were previously installed but are no
        // longer part of the build (e.g. removed catalog objects, dead
        // patches). Quiet no-op if the file isn't in the zip.
        private static void ApplyCleanupManifest(ZipArchive archive)
        {
            var entry = archive.GetEntry("CLEANUP.txt");
            if (entry == null) return;
            int removed = 0, skipped = 0;
            using (var sr = new StreamReader(entry.Open()))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.Length == 0 || line.StartsWith("#")) continue;
                    // Refuse absolute paths and parent-dir escapes so a
                    // malformed manifest can't reach outside the install dir.
                    if (line.StartsWith("/") || line.StartsWith("\\") ||
                        line.Contains("..")) { skipped++; continue; }
                    var target = System.IO.Path.Combine("./", line);
                    try
                    {
                        if (File.Exists(target))      { File.Delete(target); removed++; }
                        else if (Directory.Exists(target)) { Directory.Delete(target, true); removed++; }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"CLEANUP: could not remove {line}: {e.Message}");
                    }
                }
            }
            Console.WriteLine($"===== Cleanup: removed {removed} stale path(s), {skipped} unsafe entries skipped =====");
        }

        private async Task AdvanceExtract()
        {
            if (PathProgress >= Path.Count)
            {
                //done
                StartFreeSO();
            }
            else
            {
                //extract next zip
                var path = Path[PathProgress++];
                Console.WriteLine($"===== Extracting {path} ({PathProgress}/{Path.Count}) =====");
                if (File.Exists(path))
                {
                    ZipArchive archive;
                    try
                    {
                        archive = ZipFile.OpenRead(path);
                    } catch (Exception)
                    {
                        FileCorrupt(path);
                        return;
                    }
                    // Apply CLEANUP.txt FIRST so orphans go before we extract
                    // anything from this release. Idempotent — re-applying
                    // an already-applied cleanup is a no-op (rm of missing
                    // files is fine).
                    ApplyCleanupManifest(archive);
                    var patcher = new ReversiblePatcher(archive);
                    if (path.Contains("extra") && AllowMonogameMod)
                    {
                        patcher.IgnoreFiles.RemoveWhere(x => x.Contains("MonoGame"));
                    }
                    CurrentPatcher = patcher;
                    patcher.OnStatus += Patcher_OnStatus;
                    if (PathProgress == 1)
                    {
                        //first patch
                        if (CleanPatch)
                        {
                            foreach (var file in Directory.GetFiles("Content/Patch/"))
                            {
                                //delete any stray patch files. Don't delete user or subfolders (eg. translations) because they might be important
                                try
                                {
                                    File.Delete(file);
                                }
                                catch (Exception)
                                {

                                }
                            }
                        }
                        var worked = await patcher.AttemptRename(8);
                        if (!worked)
                        {
                            PathProgress--;
                            FSONotClosed();
                            return;
                        }
                    }
                    while (patcher.ToExtract.Count > 0)
                    {
                        await patcher.AttemptExtract();
                        var remaining = patcher.GetIncompleteFiles();
                        if (remaining.Count > 0)
                        {
                            //dilemma!
                            var arc = await ShowErrors(remaining);
                            if (arc == 0)
                            {
                                //abort.
                                patcher.Revert();
                                Cleanup();
                                StartFreeSO();
                                return;
                            }
                            else if (arc == 1)
                            {
                                //retry
                            }
                            else if (arc == 2)
                            {
                                //ignore
                                patcher.Final();
                                File.Delete(path);
                                break;
                            }
                        }
                        else
                        {
                            Console.WriteLine($"===== Completed {path} =====");
                            patcher.Final();
                            File.Delete(path);
                            await AdvanceExtract();
                        }
                    }
                }
                else
                {
                    FileMissing(path);
                }
            }
        }

        private async Task<int> ShowErrors(List<string> remaining)
        {
            var dialogResponse = new TaskCompletionSource<int>();
            string fileList;
            if (remaining.Count > 10)
            {
                fileList = string.Join("\r\n", remaining.Take(9));
                fileList += $"\r\n    ...and {remaining.Count - 9} more.";
            }
            else fileList = string.Join("\r\n", remaining);
            Console.WriteLine("Couldn't write one or more files. Make sure you are not running an instance of FreeSO! \r\nFiles:\r\n\r\n" + fileList);
            return 0;
        }


        private void Patcher_OnStatus(string message, float percent)
        {
            Console.WriteLine(message);
        }

        public void StartFreeSO()
        {
            // Write version.txt explicitly from the --target-version arg
            // if one was passed. Belt-and-suspenders against the periodic
            // bug where the file inside the patch zip fails to overwrite
            // the running client's version.txt (Linux/mono observed it
            // not extracting; root cause is file-locking / mono
            // ExtractToFile quirks). With the explicit write, version.txt
            // is correct after the update regardless of whether the zip
            // extraction succeeded for that specific file.
            WriteTargetVersionIfPresent();

            if (!File.Exists("FreeSO.exe")) File.Copy("FreeSO.exe.old", "FreeSO.exe", true);
            if (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX)
            {
                Console.WriteLine($"===== Starting FreeSO... Please wait! =====");
                var args = string.Join(" ", FilterPatcherArgs(Args));
                if (args.Length > 0) args = " " + args;
                var startArgs = new ProcessStartInfo("mono", "FreeSO.exe" + args);
                startArgs.UseShellExecute = false;
                System.Diagnostics.Process.Start(startArgs);
            }
            else
            {
                System.Diagnostics.Process.Start("FreeSO.exe", string.Join(" ", FilterPatcherArgs(Args)));
            }
            Environment.Exit(0);
        }

        // Scans Args for `--target-version <ver>` and writes that string to
        // ./version.txt. Quiet no-op if the flag wasn't passed (older
        // clients launching the new patcher) — we don't want to clobber
        // the file in that case since we'd have no signal what to write.
        private void WriteTargetVersionIfPresent()
        {
            try
            {
                for (int i = 0; i + 1 < Args.Length; i++)
                {
                    if (Args[i] == "--target-version")
                    {
                        var ver = Args[i + 1];
                        if (!string.IsNullOrEmpty(ver))
                        {
                            File.WriteAllText("version.txt", ver);
                            Console.WriteLine($"===== version.txt → {ver} =====");
                        }
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to write version.txt: {e.Message}");
            }
        }

        // Strip patcher-only flags before forwarding the remaining args to
        // FreeSO.exe. Otherwise FSOProgram tries to parse e.g.
        // "--target-version" as a game flag and logs a warning.
        private static IEnumerable<string> FilterPatcherArgs(IEnumerable<string> args)
        {
            bool skipNext = false;
            foreach (var a in args)
            {
                if (skipNext) { skipNext = false; continue; }
                if (a == "--target-version") { skipNext = true; continue; }
                if (a == "--client" || a == "--extras") continue;
                yield return a;
            }
        }

        public async Task DownloadAndAdvance()
        {
            Console.WriteLine("Downloading archives:");
            //download the file then set it as our path
            var client = new WebClient();
            Directory.CreateDirectory("PatchFiles/");

            int i = 0;
            foreach (var file in ToDownload) {
                try
                {
                    Console.WriteLine($"Downloading {file}...");
                    await client.DownloadFileTaskAsync(new Uri(file), $"PatchFiles/extra{i}.zip");
                    Path.Add($"PatchFiles/extra{i}.zip");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Could not download {file}: {e.Message}");
                }
                i++;
            }
            await AdvanceExtract();
        }

        public List<string> ToDownload = new List<string>();

        public void Begin()
        {
            Console.WriteLine("===== FreeSO Patcher CLI - 2019 =====");
            Console.WriteLine(Path.Count + " update(s) to apply.");

            if (Args.Contains("--client"))
            {
                Console.WriteLine("FreeSO client requested. Downloading from servo.freeso.org.");
                ToDownload.Add("https://fso-builds.riperiperi.workers.dev/");
            }

            if (Args.Contains("--extras"))
            {
                Console.WriteLine("Unix Extras requested. Downloading from FreeSO.org.");
                ToDownload.Add("http://freeso.org/stuff/macextras.zip");
                AllowMonogameMod = true;
            }

            if (ToDownload.Count > 0)
            {
                CleanPatch = true;
                Task.Run(() => DownloadAndAdvance()).Wait();
            }
            else {
                CleanPatch = File.Exists("PatchFiles/clean.txt");
                if (CleanPatch)
                {
                    try
                    {
                        File.Delete("PatchFiles/clean.txt");
                    }
                    catch
                    {

                    }
                }
                Task.Run(() => AdvanceExtract()).Wait();
            }
        }
    }
}
