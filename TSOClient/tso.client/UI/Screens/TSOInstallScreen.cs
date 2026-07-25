using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Panels;
using FSO.Client.Utils;
using FSO.Common.Utils;
using FSO.UI.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.Client.UI.Screens
{
    internal class UITSOInstallSettingsDialog : UIDialog
    {
        private static readonly string TSO_DOWNLOAD_URL = "https://freeso.org/redirect/TheSimsOnline"; //"https://archive.org/download/TheSimsOnline_201802/TSO.zip";

        private UIVBoxContainer RootBox;
        private UILabel DescriptionLabel;
        private UITextBox PathBox;
        private UITextBox DownloadUrlBox;
        private UIButton QuitButton;
        private UIButton DownloadButton;
        public Texture2D FreeSOLogoImage;
        public UIImage FreeSOLogo;

        public delegate void BeginDownloadDelegate(string path, string url);
        public event BeginDownloadDelegate OnBeginDownload;

        private bool _hasExisting;

        public UITSOInstallSettingsDialog() : base(UIDialogStyle.Standard, true)
        {
            Caption = "";
            RootBox = new UIVBoxContainer()
            {
                HorizontalAlignment = UIContainerHorizontalAlignment.Center,
            };

            RootBox.Add(new UISpacer(25));

            RootBox.Add(DescriptionLabel = new UILabel()
            {
                Caption = GameFacade.Strings.GetString("f131", "2"),
                Size = new Vector2(400, 190),
                Wrapped = true
            });

            RootBox.Add(new UILabel()
            {
                Caption = GameFacade.Strings.GetString("f131", "3"),
                Size = new Vector2(400, 16),
                Alignment = TextAlignment.Left,
                Wrapped = true
            });
            RootBox.Add(PathBox = new UITextBox() { Size = new Vector2(400, 25) });

            RootBox.Add(new UISpacer(0));

            RootBox.Add(new UILabel()
            {
                Caption = GameFacade.Strings.GetString("f131", "4"),
                Size = new Vector2(400, 16),
                Alignment = TextAlignment.Left,
                Wrapped = true
            });
            RootBox.Add(DownloadUrlBox = new UITextBox() { Size = new Vector2(400, 25) });

            RootBox.Add(new UISpacer(5));

            var buttonsBox = new UIHBoxContainer() { Spacing = 30 };

            buttonsBox.Add(QuitButton = new UIButton() { Caption = GameFacade.Strings.GetString("f131", "5") });
            buttonsBox.Add(DownloadButton = new UIButton() { Caption = GameFacade.Strings.GetString("f131", "6") });

            RootBox.Add(buttonsBox);

            Add(RootBox);

            // Path should be without TSO client.
            var fullPath = Path.GetFullPath(Path.Combine(GlobalSettings.Default.StartupPath, ".."));
            var currentDir = Directory.GetCurrentDirectory();

            PathBox.CurrentText = fullPath.StartsWith(currentDir) ? Path.GetRelativePath(currentDir, fullPath) : fullPath;
            DownloadUrlBox.CurrentText = TSO_DOWNLOAD_URL;

            PathBox.OnChange += PathBox_OnChange;

            QuitButton.OnButtonClick += QuitButton_OnButtonClick;
            DownloadButton.OnButtonClick += DownloadButton_OnButtonClick;

            RootBox.AutoSize();
            RootBox.Position = new Vector2(25, 40);
            SetSize((int)RootBox.Size.X + 50, (int)RootBox.Size.Y + 60);

            var ui = Content.Content.Get().CustomUI;

            FreeSOLogoImage = ui.Get("archive_logo_1x.png").Get(GameFacade.GraphicsDevice);

            FreeSOLogo = new UIImage(FreeSOLogoImage)
            {
                Position = new Vector2((Width - FreeSOLogoImage.Width) / 2, -31)
            };

            DynamicOverlay.Add(FreeSOLogo);
        }

        private void PathBox_OnChange(UIElement element)
        {
            bool newExisting = false;

            try
            {
                newExisting = File.Exists(Path.Combine(PathBox.CurrentText, "TSOClient/tuning.dat"));
            }
            catch
            {
                // Just ignore if the path is invalid.
            }

            if (newExisting != _hasExisting)
            {
                DownloadUrlBox.Opacity = newExisting ? 0.5f : 1f;
                DownloadUrlBox.Mode = newExisting ? UITextEditMode.ReadOnly : UITextEditMode.Editor;

                DownloadButton.Caption = newExisting ?
                    GameFacade.Strings.GetString("f131", "26") :
                    GameFacade.Strings.GetString("f131", "6");

                RootBox.AutoSize();

                _hasExisting = newExisting;
            }
        }

        private void DownloadButton_OnButtonClick(UIElement button)
        {
            OnBeginDownload?.Invoke(PathBox.CurrentText, DownloadUrlBox.CurrentText);
        }

        private void QuitButton_OnButtonClick(UIElement button)
        {
            GameFacade.Kill();
        }
    }

    internal class TSOInstallScreen : UIScreen
    {
        private const long WARNING_SPACE = 1024L * 1024L * 1024L * 3L;
        private UISetupBackground Background;
        private UIDialog ActiveDialog;

        private string DestPath;
        private string InstallerPath;
        private string InstallerFolderPath;

        public TSOInstallScreen() : base()
        {
            Background = new UISetupBackground();
            Add(Background);

            GameThread.NextUpdate((state) =>
            {
                Settings();
            });
        }

        private void Settings()
        {
            var dialog = new UITSOInstallSettingsDialog();
            dialog.OnBeginDownload += BeginDownload;

            ShowDialog(dialog, true);
            ActiveDialog = dialog;
        }

        private void CheckDiskSpace(string path, string url)
        {
            var info = new DriveInfo(Path.GetFullPath(path));

            if (info.AvailableFreeSpace < WARNING_SPACE)
            {
                UIAlert alert = null;

                alert = new UIAlert(new()
                {
                    Title = "",
                    Message = GameFacade.Strings.GetString("f131", "18"),
                    Buttons = [
                        new UIAlertButton(UIAlertButtonType.Yes, (btn) =>
                        {
                            RemoveDialog(alert);

                            BeginDownloadInternal(path, url);
                        }, GameFacade.Strings.GetString("f131", "22")),

                        new UIAlertButton(UIAlertButtonType.No, (btn) =>
                        {
                            RemoveDialog(alert);

                            Settings();
                        }, GameFacade.Strings.GetString("f131", "27"))
                    ]
                });

                ActiveDialog = alert;
                ShowDialog(alert, true);
            }
            else
            {
                BeginDownloadInternal(path, url);
            }
        }

        private void BeginDownloadInternal(string path, string url)
        {
            string installerPath = Path.Combine(path, "installer.zip");

            DestPath = path;
            InstallerPath = installerPath;
            InstallerFolderPath = Path.Combine(DestPath, "installer");

            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                // Check that we can write here.
                File.Create(Path.Combine(path, "dummy.txt")).Close();
                File.Delete(Path.Combine(path, "dummy.txt"));
            }
            catch
            {
                ShowErrorDialog(GameFacade.Strings.GetString("f131", "16")); // Permissions error?
                return;
            }

            var downloader = new UIWebDownloaderDialog(GameFacade.Strings.GetString("f131", "7"), [
                new DownloadItem() {
                    DestPath = installerPath,
                    Url = url,
                    Name = "TSO"
                }
                ]);

            downloader.OnComplete += DownloadComplete;

            ActiveDialog = downloader;
            ShowDialog(downloader, true);
        }

        private void BeginDownload(string path, string url)
        {
            DestPath = path;
            InstallerFolderPath = null;
            RemoveDialog(ActiveDialog);

            bool alreadyInstalled;

            try
            {
                alreadyInstalled = File.Exists(Path.Combine(path, "TSOClient/tuning.dat"));
            }
            catch
            {
                ShowErrorDialog(GameFacade.Strings.GetString("f131", "16")); // Permissions error?
                return;
            }

            if (alreadyInstalled)
            {
                UIAlert alert = null;
                alert = GlobalShowAlert(new UIAlertOptions()
                {
                    Title = GameFacade.Strings.GetString("f131", "25"),
                    Message = GameFacade.Strings.GetString("f131", "19"),
                    Buttons = [
                        new UIAlertButton(UIAlertButtonType.Yes,  (btn) => { RemoveDialog(alert); UncabComplete(true, null); }, GameFacade.Strings.GetString("f131", "22")),
                        new UIAlertButton(UIAlertButtonType.No, (btn) => { RemoveDialog(alert); CheckDiskSpace(path, url); }, GameFacade.Strings.GetString("f131", "23")),
                        new UIAlertButton(UIAlertButtonType.Cancel, (btn) => { RemoveDialog(alert); Settings(); }, GameFacade.Strings.GetString("f131", "24")),
                    ]
                }, true);
            }
            else
            {
                CheckDiskSpace(path, url);
            }
        }

        private void ShowErrorDialog(string message)
        {
            // Show alert, return to config.

            UIAlert alert = null;

            alert = new UIAlert(new()
            {
                Title = "",
                Message = message,
                Buttons = [
                    new UIAlertButton(UIAlertButtonType.OK, (btn) =>
                    {
                        RemoveDialog(alert);

                        Settings();
                    }, GameFacade.Strings.GetString("f131", "15"))
                ]
            });

            ActiveDialog = alert;
            ShowDialog(alert, true);
        }

        private void DownloadComplete(bool success, string failedFile = null)
        {
            RemoveDialog(ActiveDialog);
            if (success)
            {
                // Move onto unzipping the installer.

                if (!Directory.Exists(InstallerFolderPath))
                {
                    try
                    {
                        Directory.CreateDirectory(InstallerFolderPath);
                    }
                    catch
                    {
                        ShowErrorDialog(GameFacade.Strings.GetString("f131", "16")); // Permissions error?
                        return;
                    }
                }

                var unzip = new UIZipExtractDialog(GameFacade.Strings.GetString("f131", "8"), InstallerPath, InstallerFolderPath);

                unzip.OnComplete += InstallerUnzipped;

                unzip.Start<MultithreadedZipExtractor>();
                ActiveDialog = unzip;
                ShowDialog(unzip, true);
            }
            else
            {
                ShowErrorDialog(GameFacade.Strings.GetString("f131", "11"));
            }
        }

        private void InstallerUnzipped(bool success, Exception error)
        {
            RemoveDialog(ActiveDialog);

            if (success)
            {
                // Delete the installer zip, start extracting from the cab files.
                try
                {
                    File.Delete(InstallerPath);
                }
                catch
                {
                    // Not really fatal, but it is a huge waste of space.
                }

                var uncab = new UIZipExtractDialog(GameFacade.Strings.GetString("f131", "9"), Path.Combine(InstallerFolderPath, "Data1.cab"), DestPath);

                uncab.OnComplete += UncabComplete;

                uncab.Start<CabExtractor>();
                ActiveDialog = uncab;
                ShowDialog(uncab, true);
            }
            else
            {
                // TODO: special message for out of disk space?
                ShowErrorDialog(GameFacade.Strings.GetString("f131", "12", [error.Message]));
            }
        }

        private void UncabComplete(bool success, Exception error)
        {
            RemoveDialog(ActiveDialog);

            if (success)
            {
                if (InstallerFolderPath != null)
                {
                    try
                    {
                        Directory.Delete(InstallerFolderPath, true);
                    }
                    catch
                    {
                        // Not really fatal, but it is a huge waste of space.
                    }
                }

                GlobalSettings.Default.StartupPath = Path.Combine(DestPath, "TSOClient");
                GlobalSettings.Default.Save();
                // on windows, save to the registry?

                UIAlert alert = null;

                alert = new UIAlert(new()
                {
                    Title = "",
                    Message = GameFacade.Strings.GetString("f131", "14"),
                    Buttons = [
                        new UIAlertButton(UIAlertButtonType.OK, (btn) =>
                    {
                        FSOFacade.RestartGame();
                    }, GameFacade.Strings.GetString("f131", "15"))
                    ]
                });

                ActiveDialog = alert;
                ShowDialog(alert, true);
            }
            else
            {
                // TODO: special message for out of disk space?
                ShowErrorDialog(GameFacade.Strings.GetString("f131", "13", [error.Message]));
            }
        }
    }
}
