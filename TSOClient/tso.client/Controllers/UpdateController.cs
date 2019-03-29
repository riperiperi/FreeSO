using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Panels;
using FSO.Common;
using FSO.Common.Utils;
using FSO.Server.Clients;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.Controllers
{
    public class UpdateController : IDisposable
    {
        private UIAlert _UpdaterAlert;
        public ApiClient Api;
        private Action<bool> Continue;

        public UpdateController(Action<bool> continueFunc)
        {
            Api = new ApiClient(ApiClient.CDNUrl ?? GlobalSettings.Default.GameEntryUrl);
            Continue = continueFunc;
        }

        public void Dispose()
        {

        }

        public string GetPathString(UpdatePath path)
        {
            var result = "";
            for (int i = 0; i < path.Path.Count; i++)
            {
                var item = path.Path[i];
                if (i == 0)
                {
                    if (path.FullZipStart)
                    {
                        result += "=> " + GameFacade.Strings.GetString("f101", path.MissingInfo ? "25" : "24")
                            + item.version_name + ((path.Path.Count == 1) ? "" : "       \n");
                    }
                    else
                    {
                        result += GameFacade.Strings.GetString("f101", "26") + GlobalSettings.Default.ClientVersion + "       \n";
                    }
                }
                if (i != 0 || !path.FullZipStart)
                {
                    result += "       -> ";
                    result += GameFacade.Strings.GetString("f101", "23");
                    result += item.version_name + "\n";
                }
            }
            return result;
        }

        public void ShowUpdateDialog(UpdatePath path)
        {
            var targVer = path.Path.Last();
            _UpdaterAlert = UIScreen.GlobalShowAlert(new UIAlertOptions
            {
                Title = GameFacade.Strings.GetString("f101", "21"),
                Message = GameFacade.Strings.GetString("f101", "22", new string[] { targVer.version_name, GlobalSettings.Default.ClientVersion, GetPathString(path) }),
                Width = 500,
                Buttons = UIAlertButton.YesNo(x =>
                {
                    AcceptUpdate(path);
                },
                x =>
                {
                    RejectUpdate();
                })
            }, true);
        }

        public DownloadItem[] BuildFiles(UpdatePath path)
        {
            var result = new List<DownloadItem>();
            for (int i=0; i<path.Path.Count; i++)
            {
                var item = path.Path[i];
                result.Add(new DownloadItem()
                {
                    Url = (i == 0 && path.FullZipStart) ? item.full_zip : item.incremental_zip,
                    DestPath = $"PatchFiles/path{i}.zip",
                    Name = item.version_name
                });
                if (item.manifest_url != null)
                {
                    result.Add(new DownloadItem()
                    {
                        Url = item.manifest_url,
                        DestPath = $"PatchFiles/path{i}.json",
                        Name = item.version_name + GameFacade.Strings.GetString("f101", "29")
                    });
                }
            }

            return result.ToArray();
        }

        public void AcceptUpdate(UpdatePath path)
        {
            UIScreen.RemoveDialog(_UpdaterAlert);
            var downloader = new UIWebDownloaderDialog(GameFacade.Strings.GetString("f101", "1"), BuildFiles(path));
            downloader.OnComplete += (bool success) => {
                UIScreen.RemoveDialog(downloader);
                if (success)
                {
                    _UpdaterAlert = UIScreen.GlobalShowAlert(new UIAlertOptions
                    {
                        Title = GameFacade.Strings.GetString("f101", "3"),
                        Message = GameFacade.Strings.GetString("f101", "13"),
                        Buttons = UIAlertButton.Ok(y =>
                        {
                            UIScreen.RemoveDialog(_UpdaterAlert);
                            RestartGamePatch();
                        })
                    }, true);
                }
                else
                {
                    UIScreen.GlobalShowAlert(new UIAlertOptions
                    {
                        Title = GameFacade.Strings.GetString("f101", "30"),
                        Message = GameFacade.Strings.GetString("f101", "28"),
                        Buttons = UIAlertButton.Ok(y =>
                        {
                            Continue(false);
                        })
                    }, true);
                }
            };
            GameThread.NextUpdate(y => UIScreen.GlobalShowDialog(downloader, true));
        }

        public void RejectUpdate()
        {
            GameThread.NextUpdate(state =>
            {
                UIScreen.RemoveDialog(_UpdaterAlert);
                if (state.ShiftDown)
                {
                    _UpdaterAlert = UIScreen.GlobalShowAlert(new UIAlertOptions
                    {
                        Title = GameFacade.Strings.GetString("f101", "11"),
                        Message = GameFacade.Strings.GetString("f101", "12"),
                        Width = 500,
                        Buttons = UIAlertButton.Ok(y =>
                        {
                            //Regulator.AsyncTransition("AvatarData");
                            UIScreen.RemoveDialog(_UpdaterAlert);
                            Continue(true);
                            //View.LoginDialog.Visible = true;
                            //View.LoginProgress.Visible = true;
                        })
                    }, true);
                }
                else
                {
                    Continue(false);
                    //View.LoginDialog.Visible = true;
                    //View.LoginProgress.Visible = true;
                    //Regulator.AsyncReset();
                }
            });
        }

        public void DoUpdate(string versionName, string url)
        {
            var str = GlobalSettings.Default.ClientVersion;

            var split = str.LastIndexOf('-');
            int verNum = 0;
            string curBranch = str;
            if (split != -1)
            {
                int.TryParse(str.Substring(split + 1), out verNum);
                curBranch = str.Substring(0, split);
            }

            _UpdaterAlert = UIScreen.GlobalShowAlert(new UIAlertOptions()
            {
                Title = "",
                Message = GameFacade.Strings.GetString("f101", "27"),
                Buttons = new UIAlertButton[0]
            }, true);

            Api.GetUpdateList((updates) =>
            {
                UIScreen.RemoveDialog(_UpdaterAlert);
                GameThread.InUpdate(() =>
                {
                    UpdatePath path = null;
                    if (updates != null)
                    {
                        path = UpdatePath.FindPath(updates.ToList(), str, versionName);
                    }
                    else
                    {
                        path = new UpdatePath(new List<ApiUpdate>() { new ApiUpdate() { version_name = versionName, full_zip = url } }, true);
                        path.MissingInfo = true;
                    }
                    ShowUpdateDialog(path);
                });
            });
        }

        public void RestartGamePatch()
        {
            try
            {
                if (FSOEnvironment.Linux)
                {
                    var fsoargs = FSOEnvironment.Args;
                    if (fsoargs.Length > 0) fsoargs = " " + fsoargs;
                    var args = new ProcessStartInfo("mono", "update.exe" + fsoargs);
                    args.UseShellExecute = false;
                    System.Diagnostics.Process.Start(args);
                }
                else
                {
                    var args = new ProcessStartInfo(".\\update.exe", FSOEnvironment.Args);
                    try
                    {
                        System.Diagnostics.Process.Start(args);
                    }
                    catch (Exception)
                    {
                        args.FileName = "update.exe";
                        System.Diagnostics.Process.Start(args);
                    }
                }
                GameFacade.Kill();
            }
            catch (Exception e)
            {
                //something terrible happened :(
                _UpdaterAlert = UIScreen.GlobalShowAlert(new UIAlertOptions
                {
                    Title = GameFacade.Strings.GetString("f101", "30"),
                    Message = GameFacade.Strings.GetString("f101", "31", new string[] { e.Message }),
                    Buttons = UIAlertButton.Ok(y =>
                    {
                        UIScreen.RemoveDialog(_UpdaterAlert);
                        Continue(false);
                    })
                }, true);
            }
        }
    }
}
