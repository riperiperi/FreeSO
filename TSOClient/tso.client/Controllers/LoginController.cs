using FSO.Client.Regulators;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Panels;
using FSO.Client.UI.Screens;
using FSO.Common;
using FSO.Common.Utils;
using FSO.Server.Protocol.CitySelector;
using System;
using System.Diagnostics;

namespace FSO.Client.Controllers
{
    public class LoginController : IDisposable
    {
        private LoginScreen View;
        private LoginRegulator Regulator;
        private UIAlert _UpdaterAlert;

        public LoginController(LoginScreen view, LoginRegulator reg)
        {
            View = view;
            Regulator = reg;
            Regulator.OnTransition += Regulator_OnTransition;
        }

        private void Regulator_OnTransition(string transition, object data)
        {
            switch (transition)
            {
                case "UpdateRequired":
                    var info = (UserAuthorized)data;
                    View.LoginDialog.Visible = false;
                    View.LoginProgress.Visible = false;
                    var controller = new UpdateController(ContinueFromUpdate);
                    controller.DoUpdate((info.FSOBranch ?? "") + "-" + (info.FSOVersion ?? ""), info.FSOUpdateUrl ?? "");
                    break;
            }
        }

        private void ContinueFromUpdate(bool toSAS)
        {
            if (toSAS)
            {
                Regulator.AsyncTransition("AvatarData");
                View.LoginDialog.Visible = true;
                View.LoginProgress.Visible = true;
            }
            else
            {
                View.LoginDialog.Visible = true;
                View.LoginProgress.Visible = true;
                Regulator.AsyncReset();
            }
        }

        public void DoUpdate(string branch, string version, string url)
        {
            View.LoginDialog.Visible = false;
            View.LoginProgress.Visible = false;

            var str = GlobalSettings.Default.ClientVersion;

            var split = str.LastIndexOf('-');
            int verNum = 0;
            string curBranch = str;
            if (split != -1)
            {
                int.TryParse(str.Substring(split + 1), out verNum);
                curBranch = str.Substring(0, split);
            }

            _UpdaterAlert = UIScreen.GlobalShowAlert(new UIAlertOptions
            {
                Title = GameFacade.Strings.GetString("f101", "3"),
                Message = GameFacade.Strings.GetString("f101", "4", new string[] { version, branch, verNum.ToString(), curBranch }),
                Width = 500,
                Buttons = UIAlertButton.YesNo(x =>
                {
                    UIScreen.RemoveDialog(_UpdaterAlert);
                    var downloader = new UIWebDownloaderDialog(GameFacade.Strings.GetString("f101", "1"), new DownloadItem[]
                    {
                        new DownloadItem {
                            Url = url,
                            DestPath = "PatchFiles/patch.zip",
                            Name = GameFacade.Strings.GetString("f101", "10")
                        }
                    });
                    downloader.OnComplete += (bool success) => {
                        UIScreen.RemoveDialog(downloader);
                        UIScreen.GlobalShowAlert(new UIAlertOptions
                        {
                            Title = GameFacade.Strings.GetString("f101", "3"),
                            Message = GameFacade.Strings.GetString("f101", "13"),
                            Buttons = UIAlertButton.Ok(y =>
                            {
                                RestartGamePatch();
                            })
                        }, true);
                    };
                    GameThread.NextUpdate(y => UIScreen.GlobalShowDialog(downloader, true));
                },
                x =>
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
                                    Regulator.AsyncTransition("AvatarData");
                                    UIScreen.RemoveDialog(_UpdaterAlert);
                                    View.LoginDialog.Visible = true;
                                    View.LoginProgress.Visible = true;
                                })
                            }, true);
                        }
                        else
                        {
                            View.LoginDialog.Visible = true;
                            View.LoginProgress.Visible = true;
                            Regulator.AsyncReset();
                        }
                    });
                })
            }, true);
        }

        public void RestartGamePatch()
        {
            if (FSOEnvironment.Linux)
            {
                System.Diagnostics.Process.Start("mono", "update.exe "+FSOEnvironment.Args);
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

        public void Dispose()
        {
            View.Dispose();
            Regulator.OnTransition -= Regulator_OnTransition;
        }
    }
}
