using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Patcher
{
    public class CLIPatcher
    {
        private string[] Args;
        private List<string> Path;
        private int PathProgress = 0;
        private ReversiblePatcher CurrentPatcher;
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
                    CurrentPatcher = patcher;
                    patcher.OnStatus += Patcher_OnStatus;
                    if (PathProgress == 1)
                    {
                        //first patch
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
                            patcher.Final();
                            File.Delete(path);
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
            if (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX)
            {
                var startArgs = new ProcessStartInfo("mono", "FreeSO.exe " + string.Join(" ", Args));
                startArgs.UseShellExecute = false;
                System.Diagnostics.Process.Start(startArgs);
            }
            else
            {
                System.Diagnostics.Process.Start("FreeSO.exe", string.Join(" ", Args));
            }
            Environment.Exit(0);
        }

        public void Begin()
        {
            Console.WriteLine("===== FreeSO Patcher CLI - 2019 =====");
            Console.WriteLine(Path.Count + " update(s) to apply.");
            Task.Run(() => AdvanceExtract()).Wait();
        }
    }
}
