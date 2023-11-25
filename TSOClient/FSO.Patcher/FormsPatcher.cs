using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FSO.Patcher
{
    public partial class FormsPatcher : Form
    {
        private string[] Args;
        private List<string> Path;
        private int PathProgress = 0;
        private ReversiblePatcher CurrentPatcher;
        private bool CleanPatch;
        public FormsPatcher(List<string> extractPath, string[] args)
        {
            InitializeComponent();
            Path = extractPath;
            Args = args;
        }

        private void FSONotClosed()
        {
            Invoke(new Action(() => {
                var result = MessageBox.Show("Could not update FreeSO as write access could not be gained to the game files. Try running update.exe as an administrator.", "Error", MessageBoxButtons.RetryCancel);
                if (result == DialogResult.Cancel)
                {
                    Cleanup();
                    Application.Exit();
                }
                else
                {
                    Task.Run(() => AdvanceExtract());
                }
                return;
            }));
        }

        private void FileMissing(string path)
        {
            Invoke(new Action(() => {
                var result = MessageBox.Show($"A file has been removed while advancing through the update chain ({path}). The update must now be aborted.", "Error");
                Cleanup();
                Application.Exit();
            }));
        }


        private void FileCorrupt(string path)
        {
            Invoke(new Action(() => {
                var result = MessageBox.Show($"An update archive was corrupt ({path}). The update must now be aborted.", "Error");
                Cleanup();
                Application.Exit();
            }));
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
                try
                {
                    Directory.Delete("PatchFiles/", true);
                } catch (Exception e)
                {

                }
                Invoke(new Action(() => StartFreeSO()));
            }
            else
            {
                //extract next zip
                var path = Path[PathProgress++];
                Invoke(new Action(() =>
                {
                    OverallProgress.Value = (100 * (PathProgress - 1)) / Path.Count;
                    OverallNum.Text = PathProgress + "/" + Path.Count;
                    OverallStatus.Text = "Extracting " + path;
                }));
                if (File.Exists(path)) {
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
                        try
                        {
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
                        } catch (Exception)
                        {

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
                                Invoke(new Action(() => StartFreeSO()));
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
                } else
                {
                    FileMissing(path);
                }
            }
            await AdvanceExtract();
        }

        private async Task<int> ShowErrors(List<string> remaining)
        {
            var dialogResponse = new TaskCompletionSource<int>();
            Invoke(new Action(() => {
                string fileList;
                if (remaining.Count > 10)
                {
                    fileList = string.Join("\r\n", remaining.Take(9));
                    fileList += $"\r\n    ...and {remaining.Count - 9} more.";
                }
                else fileList = string.Join("\r\n", remaining);
                var dresult = MessageBox.Show("Couldn't write one or more files. Make sure you are not running an instance of FreeSO! \r\nFiles:\r\n\r\n" + fileList,
                    "Error", MessageBoxButtons.AbortRetryIgnore);

                if (dresult == DialogResult.Abort) dialogResponse.SetResult(0);
                else if (dresult == DialogResult.Retry) dialogResponse.SetResult(1);
                else if (dresult == DialogResult.Cancel) dialogResponse.SetResult(2);
            }));
            return await dialogResponse.Task;
        }
        

        private void Patcher_OnStatus(string message, float percent)
        {
            Invoke(new Action(() =>
            {
                SingleProgress.Value = (int)(percent * 100);
                SingleNum.Text = (int)Math.Round(percent * CurrentPatcher.Total) + "/" + CurrentPatcher.Total;
                SingleStatus.Text = message;
            }));
        }

        public void StartFreeSO()
        {
            try
            {
                if (!File.Exists("FreeSO.exe")) File.Copy("FreeSO.exe.old", "FreeSO.exe", true);
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
            } catch (Exception)
            {

            }
            Application.Exit();
        }

        private void EmergencyDownload()
        {
            var result = MessageBox.Show("You've started the patcher without any updates queued. If you wish to update the game, log into the game server and it will tell you what to do.\r\n\r\n" +
                "If you opened this application because FreeSO is unopenable, you can attempt to redownload a neutral client version using this application. Do you want to reinstall FreeSO this way?",
                "Reinstall", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                //download the file then set it as our path
                var client = new WebClient();
                Directory.CreateDirectory("PatchFiles/");
                client.DownloadProgressChanged += (obj, evt) =>
                {
                    Invoke(new Action(() =>
                    {
                        OverallProgress.Value = evt.ProgressPercentage;
                        OverallNum.Text = "1/1";
                        OverallStatus.Text = "Downloading patch.zip";
                    }));
                };

                client.DownloadFileCompleted += (obj, evt) =>
                {
                    Path.Add("PatchFiles/patch.zip");
                    CleanPatch = true;
                    Task.Run(() => AdvanceExtract());
                };

                client.DownloadFileAsync(new Uri("http://servo.freeso.org/guestAuth/repository/download/FreeSO_TsoClient/.lastSuccessful/client-<>.zip?branch=master"), "PatchFiles/patch.zip");
            }
            else
            {
                StartFreeSO();
            }
        }

        private void FormsPatcher_Load(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
            this.Show();
            this.WindowState = FormWindowState.Normal;
            if (Path.Count == 0)
            {
                EmergencyDownload();
            }
            else
            {
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
                Task.Run(() => AdvanceExtract());
            }
        }
    }
}
