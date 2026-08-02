using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using Mono.Unix;

namespace FSO.Patcher.Unix
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
                var fsoExe = GetFreeSOName();
                if (File.Exists(fsoExe+".old"))
                    File.Move(fsoExe+".old", fsoExe);
            }
            catch (Exception)
            {

            }
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
                        if (CleanPatch && Directory.Exists("Content/Patch/"))
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

            string errorText = "Couldn't write one or more files. Make sure you are not running an instance of FreeSO! \r\nFiles:\r\n\r\n" + fileList;

            Console.WriteLine(errorText);

            try
            {
                File.WriteAllText("updateError.txt", errorText);
            }
            catch
            {
                // Not urgent if we can't write the error message.
            }

            return 0;
        }


        private void Patcher_OnStatus(string message, float percent)
        {
            Console.WriteLine(message);
        }

        private string GetFreeSOName()
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX) {
                return "FreeSO";
            } else {
                return "FreeSO.exe";
            }
        }

        private bool ChmodX(string path) {
            try
            {
                var fileInfo = new UnixFileInfo(path);

                fileInfo.FileAccessPermissions = fileInfo.FileAccessPermissions | FileAccessPermissions.UserExecute | FileAccessPermissions.GroupExecute | FileAccessPermissions.OtherExecute;

                fileInfo.Refresh();

                return true;
            }
            catch
            {
                return false;
            }
        }

        private void ChmodAllExes(string basePath)
        {
            var files = Directory.GetFiles(basePath);

            foreach (var file in files)
            {
                if (System.IO.Path.GetFileName(file) == "update")
                {
                    continue;
                }

                var ext = System.IO.Path.GetExtension(file);

                if (ext.Length == 0 || ext == ".dylib")
                {
                    if (!ChmodX(file))
                    {
                        Console.WriteLine($" ! Failed to chmod '{file}' - FreeSO may fail to launch.");
                    }
                }
            }
        }

        public void StartFreeSO()
        {
            var fsoExe = GetFreeSOName();
            if (!File.Exists(fsoExe))
            {
                if (File.Exists(fsoExe + ".old"))
                {
                    File.Copy(fsoExe + ".old", fsoExe, true);
                }
                else
                {
                    Console.WriteLine($"FreeSO is not present. If you want to redownload the latest version of FreeSO, run with the --client argument.");
                    return;
                }
            }

            if (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX)
            {
                Console.WriteLine($"===== Starting FreeSO... Please wait! =====");
                ChmodAllExes("./");

                if (OperatingSystem.IsMacOS())
                {
                    var args = string.Join(" ", Args);
                    var startArgs = new ProcessStartInfo("open", $"../../ --args " + args);
                    startArgs.UseShellExecute = false;
                    System.Diagnostics.Process.Start(startArgs);
                }
                else
                {
                    var args = string.Join(" ", Args);
                    var startArgs = new ProcessStartInfo(fsoExe, args);
                    startArgs.UseShellExecute = false;
                    System.Diagnostics.Process.Start(startArgs);
                }
            }
            else
            {
                System.Diagnostics.Process.Start(fsoExe, string.Join(" ", Args));
            }
            Environment.Exit(0);
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
            Console.WriteLine("===== FreeSO Patcher CLI - 2026 =====");
            Console.WriteLine(Path.Count + " update(s) to apply.");

            if (Args.Contains("--client"))
            {
                Console.WriteLine("FreeSO client requested. Downloading from freeso.org.");
                ToDownload.Add("https://fso-archive-beta.riperiperi.workers.dev/");
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
