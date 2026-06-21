using FSO.Client.UI.Archive;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Panels;
using FSO.Common;
using FSO.Common.Utils;
using FSO.Files.FSO;
using FSO.Server.Clients;
using Newtonsoft.Json;
using RestSharp;
using System.Diagnostics;
using System.Security.Cryptography;

namespace FSO.Client.Controllers
{
    public class UpdateController : IDisposable
    {
        private UIDialog _UpdaterAlert;
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

        public void ShowUpdateDialogNew(UpdatePathNew path, bool autoUpdate = false)
        {
            _UpdaterAlert = new UIUpdateDialog(path, autoUpdate);
            _UpdaterAlert.SetController(this);

            UIScreen.GlobalShowDialog(_UpdaterAlert, true);
        }

        public DownloadItem[] BuildFilesNew(UpdatePathNew path)
        {
            Directory.CreateDirectory("PatchFiles");
            File.WriteAllText($"PatchFiles/path.json", JsonConvert.SerializeObject(path));

            var result = new List<DownloadItem>();
            for (int i = 0; i < path.Path.Count; i++)
            {
                var item = path.Path[i];
                var toDownload = ((i == 0 && path.FullZipStart) ? item.full : item.delta)?.CurrentPlatform();
                result.Add(new DownloadItem()
                {
                    Url = toDownload.zip,
                    DestPath = $"PatchFiles/path{i}.zip",
                    Name = item.id,

                    Size = toDownload.size,
                    Hash = toDownload.hash
                });

                // The old update method used to have a manifest included with each step, but it's only included in delta zips right now.
                // The information is in delta.json (this name can't be used by the freeso client files - it's deleted after patching completes)
                // The raw path information is in path.json to ensure the patcher knows which zips are delta before extracting.
            }

            return result.ToArray();
        }

        public void AcceptUpdate(UpdatePathNew path)
        {
            UIScreen.RemoveDialog(_UpdaterAlert);

            try
            {
                if (path.FullZipStart)
                {
                    System.IO.File.WriteAllText("PatchFiles/clean.txt", "CLEAN");
                }
                else
                {
                    System.IO.File.Delete("PatchFiles/clean.txt");
                }
            }
            catch
            {

            }

            var downloader = new UIWebDownloaderDialog(GameFacade.Strings.GetString("f101", "1"), BuildFilesNew(path));
            downloader.OnComplete += (bool success, string failedFile = null) => {
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
                    _UpdaterAlert = UIScreen.GlobalShowAlert(new UIAlertOptions
                    {
                        Title = GameFacade.Strings.GetString("f101", "30"),
                        Message = GameFacade.Strings.GetString("f101", "28", [ failedFile ]),
                        Buttons = UIAlertButton.Ok(y =>
                        {
                            UIScreen.RemoveDialog(_UpdaterAlert);
                            Continue(false);
                        })
                    }, true);
                }
            };
            GameThread.NextUpdate(y => UIScreen.GlobalShowDialog(downloader, true));
        }

        public void AcceptUpdate(UpdatePath path)
        {
            UIScreen.RemoveDialog(_UpdaterAlert);

            try
            {
                if (path.FullZipStart)
                {
                    System.IO.File.WriteAllText("PatchFiles/clean.txt", "CLEAN");
                } else
                {
                    System.IO.File.Delete("PatchFiles/clean.txt");
                }
            } catch
            {

            }

            var downloader = new UIWebDownloaderDialog(GameFacade.Strings.GetString("f101", "1"), BuildFiles(path));
            downloader.OnComplete += (bool success, string failedFile = null) => {
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
                    _UpdaterAlert = UIScreen.GlobalShowAlert(new UIAlertOptions
                    {
                        Title = GameFacade.Strings.GetString("f101", "30"),
                        Message = GameFacade.Strings.GetString("f101", "28", [ failedFile ]),
                        Buttons = UIAlertButton.Ok(y =>
                        {
                            UIScreen.RemoveDialog(_UpdaterAlert);
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
                            UIScreen.RemoveDialog(_UpdaterAlert);
                            Continue(true);
                        })
                    }, true);
                }
                else
                {
                    Continue(false);
                }
            });
        }

        private static FSOUpdateChannel TryGetChannel(FSOUpdateResponse response, FSOVersionInfo targetVersion)
        {
            return response.channels.FirstOrDefault(x => x.channel == targetVersion.channel && x.publicKey == targetVersion.publicKey);
        }

        private static RSA TryGetCrypto(string publicKey)
        {
            try
            {
                var rsa = RSA.Create();

                rsa.ImportFromPem(publicKey.Replace('^', '\n'));

                return rsa;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void PromptUpdate(FSOVersionInfo targetVersion)
        {
            // If the channel is different or the target version is a downgrade, show a warning before we fetch the changelog

            var current = FSOVersionInfo.Current;

            bool sameChannel = current.channel == targetVersion.channel;
            bool sameUrl = current.channelUrl == targetVersion.channelUrl;
            bool wasFsoCrypto = current.publicKey == FSOVersionInfo.FreeSOPublicKey;
            bool fsoCrypto = targetVersion.publicKey == FSOVersionInfo.FreeSOPublicKey;
            bool sameCrypto = current.publicKey == targetVersion.publicKey;
            bool warnNoCrypto = !wasFsoCrypto && string.IsNullOrEmpty(targetVersion.publicKey);

            if (!sameUrl || !sameCrypto || !sameChannel || !fsoCrypto || warnNoCrypto)
            {
                // Warn the user before they've even fetched the data.
                string message = GameFacade.Strings.GetString("f101", "40", 
                    [
                        BBCodeParser.SanitizeBB(targetVersion.channel),
                        BBCodeParser.SanitizeBB(targetVersion.id)
                    ]);

                if (string.IsNullOrEmpty(targetVersion.channelUrl))
                {
                    message += GameFacade.Strings.GetString("f101", "42");

                    _UpdaterAlert = UIScreen.GlobalShowAlert(new UIAlertOptions()
                    {
                        Title = GameFacade.Strings.GetString("f101", "21"),
                        Message = message,
                        AllowBB = true,
                        Buttons = [
                        new UIAlertButton(UIAlertButtonType.OK, (btn) =>
                        {
                            RejectUpdate();
                        }),
                    ]
                    }, true);

                    return;
                }
                else if (warnNoCrypto)
                {
                    // No crypto, not on official update
                    message += GameFacade.Strings.GetString("f101", "57");
                }
                else if (!sameCrypto)
                {
                    // Different provider
                    message += (wasFsoCrypto && !fsoCrypto) ? GameFacade.Strings.GetString("f101", "50") : GameFacade.Strings.GetString("f101", "56");
                }
                else if (!sameUrl)
                {
                    // Different update source
                    message += GameFacade.Strings.GetString("f101", "58");
                }
                else if (!sameChannel)
                {
                    // Different channel
                    message += GameFacade.Strings.GetString("f101", "51");
                }

                _UpdaterAlert = UIScreen.GlobalShowAlert(new UIAlertOptions()
                {
                    Title = GameFacade.Strings.GetString("f101", "21"),
                    Message = message,
                    AllowBB = true,
                    Buttons = [
                        new UIAlertButton(UIAlertButtonType.Cancel, (btn) =>
                        {
                            RejectUpdate();
                        }, GameFacade.Strings.GetString("f101", "44")),
                        new UIAlertButton(UIAlertButtonType.Yes, (btn) =>
                        {
                            UIScreen.RemoveDialog(_UpdaterAlert);
                            DoUpdateNew(targetVersion);
                        }, GameFacade.Strings.GetString("f101", "37")),
                    ]
                }, true);
            }
            else
            {
                DoUpdateNew(targetVersion);
            }
        }

        private static FSOVersionInfo GetVersionInfo(string url, FSOUpdateChannel channel, FSOUpdateMetadata update)
        {
            return new FSOVersionInfo()
            {
                id = update.id,
                channelUrl = url,
                channel = channel.channel,
                publicKey = channel.publicKey,
            };
        }

        public static void TryGetAutoUpdate(Action<bool, FSOVersionInfo, UpdatePathNew> onResult)
        {
            var current = FSOVersionInfo.Current;

            RSA crypto = null;
            if (current.publicKey.Length > 0)
            {
                crypto = TryGetCrypto(current.publicKey);
            }

            var client = new RestClient();
            client.GetAsync(new RestRequest(current.channelUrl)).ContinueWith((x) =>
            {
                if (!x.IsFaulted && !x.IsCanceled && x.Result.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<FSOUpdateResponse>(x.Result.Content);

                    FSOUpdateChannel channel;
                    if (result != null && (channel = TryGetChannel(result, current)) != null)
                    {
                        var target = channel.updates.FirstOrDefault(x => x.full?.CurrentPlatform() != null);

                        if (target == null)
                        {
                            // No eligible version to update to. Just assume we're on the latest.
                            onResult(true, null, null);
                            return;
                        }

                        var targetVersion = GetVersionInfo(current.channelUrl, channel, target);
                        var path = UpdatePathNew.FindPath(channel, current, targetVersion);

                        if (path != null)
                        {
                            if (crypto != null)
                            {
                                // Validate signatures of the hashes for each part of the path.

                                bool first = true;
                                foreach (var step in path.Path)
                                {
                                    var file = ((path.FullZipStart && first) ? step.full : step.delta)?.CurrentPlatform();

                                    if (file == null || !crypto.VerifyHash(Convert.FromBase64String(file.hash), Convert.FromBase64String(file.signature), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
                                    {
                                        // The hash's signature doesn't match.
                                        onResult(false, null, null);
                                    }

                                    first = false;
                                }
                            }

                            if (path.Path.Count == 0)
                            {
                                // Currently on the latest version.
                                onResult(true, null, null);
                                return;
                            }


                            onResult(true, targetVersion, path);
                            return;
                        }
                    }
                }

                onResult(false, null, null);
            });
        }

        public void DoUpdateNew(FSOVersionInfo targetVersion)
        {
            var current = FSOVersionInfo.Current;

            // Temporary dialog shown while getting update data.
            _UpdaterAlert = UIScreen.GlobalShowAlert(new UIAlertOptions()
            {
                Title = "",
                Message = GameFacade.Strings.GetString("f101", "27"),
                Buttons = []
            }, true);

            RSA crypto = null;
            if (targetVersion.publicKey.Length > 0)
            {
                crypto = TryGetCrypto(targetVersion.publicKey);
            }

            var client = new RestClient();
            client.GetAsync(new RestRequest(targetVersion.channelUrl)).ContinueWith((x) =>
            {
                string failReason = GameFacade.Strings.GetString("f101", "32", [ targetVersion.channelUrl ]);
                if (!x.IsFaulted && !x.IsCanceled && x.Result.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<FSOUpdateResponse>(x.Result.Content);

                    FSOUpdateChannel channel;
                    if (result != null && (channel = TryGetChannel(result, targetVersion)) != null)
                    {
                        var path = UpdatePathNew.FindPath(channel, current, targetVersion);

                        if (path != null)
                        {
                            bool success = true;
                            if (crypto != null)
                            {
                                // Validate signatures of the hashes for each part of the path.

                                bool first = true;
                                foreach (var step in path.Path)
                                {
                                    var file = ((path.FullZipStart && first) ? step.full : step.delta)?.CurrentPlatform();

                                    if (file == null || !crypto.VerifyHash(Convert.FromBase64String(file.hash), Convert.FromBase64String(file.signature), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
                                    {
                                        // The hash's signature doesn't match.
                                        failReason = GameFacade.Strings.GetString("f101", "54");
                                        success = false;
                                    }

                                    first = false;
                                }
                            }

                            if (path.Path.Count == 0)
                            {
                                failReason = GameFacade.Strings.GetString("f101", "42");
                                success = false;
                            }

                            if (success)
                            {
                                GameThread.InUpdate(() =>
                                {
                                    UIScreen.RemoveDialog(_UpdaterAlert);
                                    ShowUpdateDialogNew(path);
                                });
                                return;
                            }
                        }
                    }
                }

                GameThread.InUpdate(() =>
                {
                    UIScreen.RemoveDialog(_UpdaterAlert);

                    UIAlert.Alert(
                        GameFacade.Strings.GetString("f101", "30"), // Updater failed
                        failReason,
                        true
                        );

                    Continue(false);
                });
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
                    if (path == null)
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
                if (FSOEnvironment.Linux) Environment.Exit(0); //we're serious
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
