using FSO.Server.Api.Core.Models;
using FSO.Server.Database.DA.Updates;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FSO.Files.Utils;
using FSO.Server.Common;
using System.Diagnostics;

namespace FSO.Server.Api.Core.Services
{
    public class GenerateUpdateService
    {
        private static GenerateUpdateService _INSTANCE;
        public static GenerateUpdateService INSTANCE
        {
            get
            {
                if (_INSTANCE == null) _INSTANCE = new GenerateUpdateService();
                return _INSTANCE;
            }
        }

        private int LastTaskID = 0;
        public Dictionary<int, UpdateGenerationStatus> Tasks = new Dictionary<int, UpdateGenerationStatus>();

        public UpdateGenerationStatus GetTask(int id)
        {
            UpdateGenerationStatus result;
            lock (Tasks)
            {
                if (!Tasks.TryGetValue(id, out result)) return null;
            }
            return result;
        }

        public UpdateGenerationStatus CreateTask(UpdateCreateModel request)
        {
            UpdateGenerationStatus task;
            lock (Tasks)
            {
                task = new UpdateGenerationStatus(++LastTaskID, request);
                Tasks[LastTaskID] = task;
            }
            Task.Run(() => BuildUpdate(task));
            return task;
        }

        private void Exec(string cmd)
        {
            var escapedArgs = cmd.Replace("\"", "\\\"");

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "/bin/sh",
                    Arguments = $"-c \"{escapedArgs}\""
                }
            };

            process.Start();
            process.WaitForExit();
        }

        private void ClearFolderPermissions(string folder)
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
                Exec($"chmod -R 777 {folder}");
        }

        private string GetZipFolder(string path)
        {
            var directories = Directory.GetDirectories(path);
            if (directories.Length != 1) return path;
            var files = Directory.GetFiles(path);
            if (files.Length != 0) return path;
            return directories[0];
        }

        public async Task BuildUpdate(UpdateGenerationStatus status)
        {
            var request = status.Request;
            var api = Api.INSTANCE;

            try
            {
                status.UpdateStatus(UpdateGenerationStatusCode.PREPARING);
                using (var da = api.DAFactory.Get())
                {
                    var baseUpdateKey = "updates/";
                    var branch = da.Updates.GetBranch(status.Request.branchID);

                    //reserve update id. may cause race condition, but only one person can update anyways.
                    if (request.minorVersion) ++branch.minor_version_number;
                    else
                    {
                        ++branch.last_version_number;
                        branch.minor_version_number = 0;
                    }
                    var updateID = branch.last_version_number;
                    var minorChar = (branch.minor_version_number == 0) ? "" : ((char)('a' + (branch.minor_version_number - 1))).ToString();
                    var versionName = branch.version_format.Replace("#", updateID.ToString()).Replace("@", minorChar);
                    var versionText = versionName;

                    var result = new DbUpdate()
                    {
                        addon_id = branch.addon_id,
                        branch_id = branch.branch_id,
                        date = DateTime.UtcNow,
                        version_name = versionName,
                        deploy_after = Epoch.ToDate(status.Request.scheduledEpoch)
                    };

                    versionName = versionName.Replace('/', '-');

                    var client = new WebClient();
                    //fetch artifacts
                    //http://servo.freeso.org/guestAuth/repository/download/FreeSO_TsoClient/.lastSuccessful/client-<>.zip
                    //http://servo.freeso.org/guestAuth/repository/download/FreeSO_TsoClient/.lastSuccessful/server-<>.zip

                    int updateWorkID = status.TaskID;

                    var updateDir = "updateTemp/" + updateWorkID + "/";
                    try
                    {
                        Directory.Delete(updateDir, true);
                    }
                    catch (Exception) { }
                    Directory.CreateDirectory(updateDir);
                    Directory.CreateDirectory(updateDir + "client/");
                    Directory.CreateDirectory(updateDir + "server/");

                    string clientArti = null;
                    string serverArti = null;
                    if (branch.base_build_url != null)
                    {
                        status.UpdateStatus(UpdateGenerationStatusCode.DOWNLOADING_CLIENT);
                        await client.DownloadFileTaskAsync(new Uri(branch.base_build_url), updateDir + "client.zip");
                        clientArti = updateDir + "client.zip";
                    }
                    if (branch.base_server_build_url != null)
                    {
                        status.UpdateStatus(UpdateGenerationStatusCode.DOWNLOADING_SERVER);
                        await client.DownloadFileTaskAsync(new Uri(branch.base_server_build_url), updateDir + "server.zip");
                        serverArti = updateDir + "server.zip";
                    }

                    string clientAddon = null;
                    string serverAddon = null;

                    if (branch.addon_id != null)
                    {
                        var addon = da.Updates.GetAddon(branch.addon_id.Value);
                        if (addon.addon_zip_url != null)
                        {
                            status.UpdateStatus(UpdateGenerationStatusCode.DOWNLOADING_CLIENT_ADDON);
                            await client.DownloadFileTaskAsync(new Uri(addon.addon_zip_url), updateDir + "clientAddon.zip");
                            clientAddon = updateDir + "clientAddon.zip";
                        }
                        if (addon.server_zip_url != null)
                        {
                            status.UpdateStatus(UpdateGenerationStatusCode.DOWNLOADING_SERVER_ADDON);
                            await client.DownloadFileTaskAsync(new Uri(addon.addon_zip_url), updateDir + "serverAddon.zip");
                            serverAddon = updateDir + "serverAddon.zip";
                        }
                        else
                        {
                            serverAddon = clientAddon;
                        }
                    }

                    //last client update.
                    var previousUpdate = (branch.current_dist_id == null) ? null : da.Updates.GetUpdate(branch.current_dist_id.Value);

                    //all files downloaded. build the folders.
                    //extract the artifact and then our artifact over it.
                    if (clientArti != null)
                    {
                        var clientPath = updateDir + "client/";
                        status.UpdateStatus(UpdateGenerationStatusCode.EXTRACTING_CLIENT);
                        var clientZip = ZipFile.Open(clientArti, ZipArchiveMode.Read);
                        clientZip.ExtractToDirectory(clientPath, true);
                        clientZip.Dispose();
                        File.Delete(clientArti);
                        clientPath = GetZipFolder(clientPath);

                        if (clientAddon != null)
                        {
                            status.UpdateStatus(UpdateGenerationStatusCode.EXTRACTING_CLIENT_ADDON);
                            var addonZip = ZipFile.Open(clientAddon, ZipArchiveMode.Read);
                            addonZip.ExtractToDirectory(clientPath, true);
                            addonZip.Dispose();
                            if (clientAddon != serverAddon) File.Delete(clientAddon);
                        }
                        //emit version number
                        await System.IO.File.WriteAllTextAsync(Path.Combine(clientPath, "version.txt"), versionText);
                        if (request.catalog != null)
                        {
                            await System.IO.File.WriteAllTextAsync(Path.Combine(clientPath, "Content/Objects/catalog_downloads.xml"), request.catalog);
                        }

                        string diffZip = null;
                        FSOUpdateManifest manifest = null;

                        status.UpdateStatus(UpdateGenerationStatusCode.BUILDING_DIFF);
                        if (previousUpdate != null || request.disableIncremental)
                        {
                            result.last_update_id = previousUpdate.update_id;
                            //calculate difference, generate an incremental update manifest + zip
                            var prevFile = updateDir + "prev.zip";
                            await client.DownloadFileTaskAsync(new Uri(previousUpdate.full_zip), updateDir + "prev.zip");
                            var prevZip = ZipFile.Open(prevFile, ZipArchiveMode.Read);
                            prevZip.ExtractToDirectory(updateDir + "prev/", true);
                            prevZip.Dispose();
                            File.Delete(updateDir + "prev.zip");

                            var diffs = DiffGenerator.GetDiffs(Path.GetFullPath(updateDir + "prev/"), Path.GetFullPath(clientPath));

                            status.UpdateStatus(UpdateGenerationStatusCode.BUILDING_INCREMENTAL_UPDATE);
                            var toZip = diffs.Where(x => x.DiffType == FileDiffType.Add || x.DiffType == FileDiffType.Modify);
                            if (request.contentOnly) toZip = toZip.Where(x => x.Path.Replace('\\', '/').TrimStart('/').StartsWith("Content"));
                            if (!request.includeMonogameDelta) toZip = toZip.Where(x => !x.Path.Replace('\\', '/').TrimStart('/').StartsWith("Monogame"));
                            //build diff folder
                            Directory.CreateDirectory(updateDir + "diff/");
                            foreach (var diff in toZip)
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(updateDir + "diff/", diff.Path)));
                                System.IO.File.Copy(Path.Combine(clientPath, diff.Path), Path.Combine(updateDir + "diff/", diff.Path));
                            }
                            diffZip = updateDir + "diffResult.zip";
                            ClearFolderPermissions(updateDir + "diff/");
                            ZipFile.CreateFromDirectory(updateDir + "diff/", diffZip, CompressionLevel.Optimal, false);
                            Directory.Delete(updateDir + "diff/", true);
                            manifest = new FSOUpdateManifest() { Diffs = diffs };

                            Directory.Delete(updateDir + "prev/", true);
                        }
                        else
                        {
                            if (request.contentOnly) throw new Exception("Invalid request - you cannot make a content only update with no delta.");
                            //full update only. generate simple manifest that contains all files (added)
                            manifest = new FSOUpdateManifest() { Diffs = new List<FileDiff>() };
                        }

                        //pack full client
                        if (!request.contentOnly)
                        {
                            status.UpdateStatus(UpdateGenerationStatusCode.BUILDING_CLIENT);
                            var finalClientZip = updateDir + "clientResult.zip";
                            ClearFolderPermissions(clientPath);
                            ZipFile.CreateFromDirectory(clientPath, finalClientZip, CompressionLevel.Optimal, false);
                            Directory.Delete(updateDir + "client/", true);

                            status.UpdateStatus(UpdateGenerationStatusCode.PUBLISHING_CLIENT);
                            result.full_zip = await Api.INSTANCE.UpdateUploaderClient.UploadFile($"{baseUpdateKey}client-{versionName}.zip", finalClientZip, versionName);
                        }
                        status.UpdateStatus(UpdateGenerationStatusCode.PUBLISHING_CLIENT);
                        if (diffZip != null)
                        {
                            result.incremental_zip = await Api.INSTANCE.UpdateUploaderClient.UploadFile($"{baseUpdateKey}incremental-{versionName}.zip", diffZip, versionName);
                        }
                        await System.IO.File.WriteAllTextAsync(updateDir + "manifest.json", Newtonsoft.Json.JsonConvert.SerializeObject(manifest));
                        result.manifest_url = await Api.INSTANCE.UpdateUploaderClient.UploadFile($"{baseUpdateKey}{versionName}.json", updateDir + "manifest.json", versionName);
                    }

                    if (serverArti != null && !request.contentOnly)
                    {
                        var serverPath = updateDir + "server/";
                        status.UpdateStatus(UpdateGenerationStatusCode.EXTRACTING_SERVER);
                        var serverZip = ZipFile.Open(serverArti, ZipArchiveMode.Read);
                        serverZip.ExtractToDirectory(serverPath, true);
                        serverZip.Dispose();
                        File.Delete(serverArti);
                        serverPath = GetZipFolder(serverPath);

                        if (serverAddon != null)
                        {
                            status.UpdateStatus(UpdateGenerationStatusCode.EXTRACTING_SERVER_ADDON);
                            var addonZip = ZipFile.Open(serverAddon, ZipArchiveMode.Read);
                            addonZip.ExtractToDirectory(serverPath, true);
                            addonZip.Dispose();
                            File.Delete(serverAddon);
                        }
                        //emit version number
                        await System.IO.File.WriteAllTextAsync(Path.Combine(serverPath, "version.txt"), versionText);
                        if (request.catalog != null)
                        {
                            await System.IO.File.WriteAllTextAsync(Path.Combine(serverPath, "Content/Objects/catalog_downloads.xml"), request.catalog);
                        }

                        status.UpdateStatus(UpdateGenerationStatusCode.BUILDING_SERVER);
                        var finalServerZip = updateDir + "serverResult.zip";
                        ClearFolderPermissions(serverPath);
                        ZipFile.CreateFromDirectory(serverPath, finalServerZip, CompressionLevel.Optimal, false);
                        Directory.Delete(updateDir + "server/", true);

                        status.UpdateStatus(UpdateGenerationStatusCode.PUBLISHING_SERVER);
                        result.server_zip = await Api.INSTANCE.UpdateUploader.UploadFile($"{baseUpdateKey}server-{versionName}.zip", finalServerZip, versionName);
                    } else
                    {
                        result.server_zip = result.incremental_zip; //same as client, as server uses same content.
                    }

                    status.UpdateStatus(UpdateGenerationStatusCode.SCHEDULING_UPDATE);
                    var finalID = da.Updates.AddUpdate(result);
                    da.Updates.UpdateBranchLatest(branch.branch_id, branch.last_version_number, branch.minor_version_number);
                    status.SetResult(result);
                }
            } catch (Exception e)
            {
                status.SetFailure("Update could not be completed." + e.ToString());
            }
        }
    }
}
