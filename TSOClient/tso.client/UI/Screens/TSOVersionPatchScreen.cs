using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Panels;
using FSO.Common;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Utils;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using TSOVersionPatcher;

namespace FSO.Client.UI.Screens
{
    public class TSOVersionPatchScreen : UIScreen
    {
        private UISetupBackground Background;
        private string Version;
        private bool CanUpdate;
        private string Patchable = "1.1239.1.0";
        private bool Updating;

        public TSOVersionPatchScreen() : base()
        {
            Background = new UISetupBackground();
            Add(Background);

            Version = Content.Content.Get().VersionString;
            CanUpdate = Version == Patchable;

            GameThread.NextUpdate((state) =>
            {
                UIAlert.Alert(GameFacade.Strings.GetString("f101", "14"),
                    GameFacade.Strings.GetString("f101", "15", new string[] {
                        Version, GameFacade.Strings.GetString("f101", CanUpdate ? "16" : "17")
                    }),
                    true);
            });
        }

        public override void Update(UpdateState state)
        {
            if (!Updating)
            {
                if (Children.Count == 1)
                {
                    if (CanUpdate)
                    {
                        BeginUpdate();
                    }
                    else
                    {
                        GameFacade.Game.Exit();
                    }
                }
                else if (state.CtrlDown && state.ShiftDown && state.NewKeys.Contains(Microsoft.Xna.Framework.Input.Keys.C))
                {
                    FSOFacade.Controller.StartLoading();
                }
            }
            base.Update(state);
        }

        public void BeginUpdate()
        {
            Updating = true;
            var progress = new UILoginProgress()
            {
                Caption = GameFacade.Strings.GetString("f101", "18")
            };
            GlobalShowDialog(progress, true);

            var file = File.Open("Content/Patch/1239toNI.tsop", FileMode.Open, FileAccess.Read, FileShare.Read);
            TSOp patch = new TSOp(file);

            var content = Content.Content.Get();
            var patchPath = Path.GetFullPath(Path.Combine(content.BasePath, "../"));
            Task.Run(() => patch.Apply(patchPath, patchPath, (string message, float pct) =>
            {
                GameThread.InUpdate(() =>
                {
                    if (pct == -1)
                    {
                        UIScreen.GlobalShowAlert(new UIAlertOptions
                        {
                            Title = GameFacade.Strings.GetString("f101", "19"),
                            Message = GameFacade.Strings.GetString("f101", "20", new string[] { message }),
                            Buttons = UIAlertButton.Ok(y =>
                            {
                                RestartGame();
                            })
                        }, true);
                    }
                    else
                    {
                        progress.Progress = pct * 100;
                        progress.ProgressCaption = message;
                    }
                });
            })).ContinueWith((task) =>
            {
                GameThread.InUpdate(() =>
                {
                    UIScreen.RemoveDialog(progress);
                    UIScreen.GlobalShowAlert(new UIAlertOptions
                    {
                        Title = GameFacade.Strings.GetString("f101", "3"),
                        Message = GameFacade.Strings.GetString("f101", "13"),
                        Buttons = UIAlertButton.Ok(y =>
                        {
                            RestartGame();
                        })
                    }, true);
                });
            });
        }

        public void RestartGame()
        {
            try
            {
                if (FSOEnvironment.Linux)
                {
                    System.Diagnostics.Process.Start("mono", "FreeSO.exe " + FSOEnvironment.Args);
                }
                else
                {
                    var args = new ProcessStartInfo(".\\FreeSO.exe", FSOEnvironment.Args);
                    try
                    {

                        System.Diagnostics.Process.Start(args);
                    }
                    catch (Exception)
                    {
                        args.FileName = "FreeSO.exe";
                        System.Diagnostics.Process.Start(args);
                    }
                }
            } catch
            {

            }

            GameFacade.Kill();
        }
    }
}
