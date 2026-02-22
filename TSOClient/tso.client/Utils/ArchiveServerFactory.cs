using FSO.Client.Controllers;
using FSO.Client.Model.Archive;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Panels;
using FSO.Common;
using FSO.Common.Utils;
using System.IO.Compression;

namespace FSO.Client.Utils
{
    internal class ArchiveServerFactory
    {
        private readonly ArchiveConfiguration Config;
        private readonly ConnectArchiveController Controller;
        private Action<bool> OnResult;

        public ArchiveServerFactory(ArchiveConfiguration config, ConnectArchiveController controller)
        {
            Config = config;
            Controller = controller;
        }

        public static ArchiveConfiguration GetQuickStartConfig()
        {
            var clientConfig = ClientArchiveConfiguration.Default;
            var config = clientConfig.ToHostConfig();

            config.Flags |= ArchiveConfigFlags.QuickStartDesirable;
            config.Flags &= ~ArchiveConfigFlags.QuickStartUndesirable;

            config.CityPort = 33101;
            config.LotPort = 34101;

            return config;
        }

        public ArchiveConfiguration GetConfig()
        {
            return Config;
        }

        private bool ValidateData(ArchiveManifest manifest, out string dir)
        {
            // Database should exist, Data directory should exist.
            // Doesn't validate that they make any sense right now...

            var dataFolder = Path.Combine(Path.GetDirectoryName(manifest.ActivePath), manifest.LocalDir);

            dir = null;

            if (dataFolder == null || dataFolder == "")
            {
                // Try the data/ subfolder.

                dataFolder = Path.Combine(Path.GetDirectoryName(manifest.ActivePath), "data");
            }

            if (dataFolder == null || !Directory.Exists(dataFolder))
            {
                return false;
            }

            dir = dataFolder;

            var dbFile = Path.Combine(dataFolder, "fsoarchive.db");

            return File.Exists(dbFile);
        }

        private bool ZipDataPresent(ArchiveManifest manifest, out string path)
        {
            var folder = Path.GetDirectoryName(manifest.ActivePath);

            path = Path.Combine(folder, "archive.zip");

            try
            {
                using (var file = ZipFile.OpenRead(path))
                {

                }
            }
            catch
            {
                return false;
            }

            // TODO: validate hash?

            return File.Exists(path);
        }

        private void ExtractArchive(ArchiveManifest manifest, string path, Action<bool> onResult)
        {
            string extractPath = Path.Combine(Path.GetDirectoryName(manifest.ActivePath), "data/");
            var extractor = new UIZipExtractDialog(null, path, extractPath);

            extractor.OnComplete += (result) =>
            {
                if (result)
                {
                    UIScreen.RemoveDialog(extractor);

                    manifest.LocalDir = "data/";
                    manifest.Save();

                    Config.ArchiveDataDirectory = extractPath;
                    onResult(true);
                }
                else
                {
                    UIScreen.RemoveDialog(extractor);
                    onResult(false);
                }
            };

            extractor.Start();
            UIScreen.GlobalShowDialog(extractor, true);
        }

        private void RequestDownload(ArchiveManifest manifest, Action<bool> onResult)
        {
            var basePath = Path.GetDirectoryName(manifest.ActivePath);
            var downloadPath = Path.Combine(basePath, "archive.zip");

            Uri uri;
            try
            {
                uri = new Uri(manifest.ZipLocation);
            }
            catch
            {
                // TODO: dialog?
                onResult(false);
                return;
            }

            var size = $"{int.Parse(manifest.Size) / (1024f * 1024f)} MB";

            UIAlert alert = null;

            alert = UIScreen.GlobalShowAlert(new UIAlertOptions
            {
                Title = GameFacade.Strings.GetString("f128", "1"),
                Message = GameFacade.Strings.GetString("f128", "2", new string[] { manifest.Name, uri.Host, size }),
                Width = 500,
                Buttons = UIAlertButton.YesNo(x =>
                {
                    UIScreen.RemoveDialog(alert);
                    var downloader = new UIWebDownloaderDialog(GameFacade.Strings.GetString("f128", "5"), new DownloadItem[]
                    {
                        new DownloadItem {
                            Url = manifest.ZipLocation,
                            DestPath = downloadPath,
                            Name = manifest.Name
                        }
                    });
                    downloader.OnComplete += (bool success) => {
                        UIScreen.RemoveDialog(downloader);

                        if (success && ZipDataPresent(manifest, out string _))
                        {
                            ExtractArchive(manifest, downloadPath, onResult);
                        }
                        else
                        {
                            UIScreen.GlobalShowAlert(new UIAlertOptions
                            {
                                Title = GameFacade.Strings.GetString("f128", "10"),
                                Message = GameFacade.Strings.GetString("f128", "11"),
                                Buttons = UIAlertButton.Ok()
                            }, true);

                            onResult(false);
                        }
                    };
                    GameThread.NextUpdate(y => UIScreen.GlobalShowDialog(downloader, true));
                },
                x =>
                {
                    GameThread.NextUpdate(state =>
                    {
                        UIScreen.RemoveDialog(alert);
                        onResult(false);
                    });
                })
            }, true);
        }

        public void Start(Action<bool> onResult)
        {
            var manifests = ArchiveSaves.ListManifests();

            var clientConfig = ClientArchiveConfiguration.Default;
            var name = clientConfig.SelectedArchiveName;
            var selected = manifests.FirstOrDefault((item) => item.Name == name);

            if (selected == null)
            {
                // Nothing to start?
                onResult(false);
            }
            else
            {
                Start(selected, onResult);
            }
        }

        public void Start(ArchiveManifest manifest, Action<bool> onResult)
        {
            Prepare(manifest, (success) =>
            {
                if (!success)
                {
                    onResult(false);
                    return;
                }

                StartWithConfig(onResult);
            });
        }

        public void Prepare(ArchiveManifest manifest, Action<bool> onResult)
        {
            if (ValidateData(manifest, out string dir))
            {
                Config.ArchiveDataDirectory = dir;
                Config.LoadEvents();
                onResult(true);
            }
            else
            {
                if (ZipDataPresent(manifest, out string zipPath))
                {
                    ExtractArchive(manifest, zipPath, onResult);
                }
                else
                {
                    // Don't have anything - need to ask the user to download.

                    RequestDownload(manifest, onResult);
                }
            }
        }

        private async Task<bool> TryUPnP()
        {
            var cityNat = new NatPuncher("FreeSO Archive City Server");

            var cityResult = await cityNat.NatPunch(33101, 1, 10);

            if (cityResult == 0 || cityResult == ushort.MaxValue)
            {
                return false;
            }

            var lotNat = new NatPuncher("FreeSO Archive Lot Server", cityNat);

            var lotResult = await lotNat.NatPunch(34101, 1, 10);

            if (lotResult == 0 || lotResult == ushort.MaxValue)
            {
                cityNat.Dispose();
                return false;
            }

            Config.CityPort = cityResult;
            Config.LotPort = lotResult;
            Config.Disposables = new IDisposable[] { cityNat, lotNat };

            return true;
        }

        private void StartWithConfig(Action<bool> onResult)
        {
            if (Config.Flags.HasFlag(ArchiveConfigFlags.UPnP) && !Config.Flags.HasFlag(ArchiveConfigFlags.Offline))
            {
                var alert = UIScreen.GlobalShowAlert(new UIAlertOptions
                {
                    Title = GameFacade.Strings.GetString("f128", "14"),
                    Message = GameFacade.Strings.GetString("f128", "15"),
                    Buttons = new UIAlertButton[0]
                }, true);

                Task.Run(TryUPnP).ContinueWith(x =>
                {
                    bool result = x.Result;

                    GameThread.NextUpdate(state =>
                    {
                        UIScreen.RemoveDialog(alert);
                        if (result)
                        {
                            Controller.CreateServer(Config);
                        }
                        else
                        {
                            // UPnP failed. Get the user to disable it.
                            onResult(false);
                            UIAlert.Alert(GameFacade.Strings.GetString("f128", "16"), GameFacade.Strings.GetString("f128", "17"), true);
                        }
                    });
                });
            }
            else
            {
                Controller.CreateServer(Config);
            }
        }
    }
}
