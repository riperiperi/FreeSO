using FSO.Files.FSO;
using FSO.Files.Utils;
using Newtonsoft.Json;
using Octokit;
using System.IO.Compression;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace FSO.UpdateBuilder
{
    internal class DeltaJson
    {
        public FileDiff[] Diffs { get; set; } = [];
    }

    internal class Program
    {
        private static Regex AssetUrlUntagged = new Regex("/untagged-[0-9a-f]+/");

        private static LibGit2Sharp.Commit? GetReleaseCommit(Release lastRelease, LibGit2Sharp.Repository gitRepo)
        {
            var lastTag = gitRepo.Tags.FirstOrDefault(tag => tag.FriendlyName == lastRelease.TagName);

            if (lastTag != null && lastTag.PeeledTarget is LibGit2Sharp.Commit commit)
            {
                return commit;
            }

            return null;
        }

        private static async Task<bool> DownloadLastBuild(HttpClient http, FSOUpdateFile file, string workingDirectory, string platform, string[] targets, string version)
        {
            if (!targets.Contains(platform) || file == null)
            {
                return false;
            }

            string targetDirectory = Path.Combine(workingDirectory, $"{platform}-old");

            try
            {
                Console.WriteLine($"Trying to download old {platform} version from {FixAssetUrl(file.zip, version)}");
                var fileRequest = await http.GetAsync(FixAssetUrl(file.zip, version));

                if (!fileRequest.IsSuccessStatusCode)
                {
                    return false;
                }

                using var zipStream = await fileRequest.Content.ReadAsStreamAsync();

                ZipFile.ExtractToDirectory(zipStream, targetDirectory);

                Console.WriteLine($"Downloaded old {platform} version.");
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Download failed: {FixAssetUrl(file.zip, version)}");
                return false;
            }
        }

        private static async Task<FSOUpdateFile> FolderToZip(GitHubClient client, Release release, string target, string versionString, string zipQualifier, string directory, RSA? crypto)
        {
            // Build a zip from the input directory.

            using var mem = new MemoryStream();
            ZipFile.CreateFromDirectory(directory, mem, CompressionLevel.Optimal, false);

            mem.Position = 0;

            var asset = await client.Repository.Release.UploadAsset(release, new ReleaseAssetUpload()
            {
                FileName = $"{zipQualifier}-{target}-{versionString}.zip",
                ContentType = "application/zip",
                RawData = new MemoryStream(mem.ToArray()),
            });

            var hash = SHA256.Create();

            mem.Position = 0;
            var shaHash = SHA256.HashData(mem);

            return new FSOUpdateFile()
            {
                zip = FixAssetUrl(asset.BrowserDownloadUrl, versionString),
                size = (int)mem.Length,
                hash = Convert.ToBase64String(shaHash),
                signature = crypto != null ? Convert.ToBase64String(crypto.SignHash(shaHash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)) : "",
            };
        }

        private static RSA TryGetCrypto(string privateKey)
        {
            try
            {
                var rsa = RSA.Create();

                rsa.ImportFromPem(privateKey.Replace('^', '\n'));

                return rsa;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string FixAssetUrl(string url, string version)
        {
            return AssetUrlUntagged.Replace(url, $"/{version}/");
        }

        static async Task Main(string[] args)
        {
            string workingDirectory = args[0] ?? "./";
            Console.WriteLine($"FreeSO Update Packager (working directory: {workingDirectory})");
            Console.WriteLine("==============================================================");
            Console.WriteLine("");

            Console.WriteLine("Initializing GitHub Client");
            var client = new GitHubClient(new ProductHeaderValue("freeso-ci"));
            var rawToken = Environment.GetEnvironmentVariable("GH_TOKEN");
            var tokenAuth = new Octokit.Credentials(rawToken);
            client.Credentials = tokenAuth;

            string repoString = Environment.GetEnvironmentVariable("FSO_UPDATE_GITHUB_REPO") ?? "riperiperi/FreeSO";
            string[] splitRepo = repoString.Split('/');
            string authorName = splitRepo[0];
            string repoName = splitRepo[1];
            string primaryBranchName = Environment.GetEnvironmentVariable("FSO_UPDATE_PRIMARY_BRANCH") ?? "master";

            string releaseChannel = Environment.GetEnvironmentVariable("FSO_UPDATE_RELEASE_CHANNEL") ?? "FreeSO Archive";
            string releaseSuffix = Environment.GetEnvironmentVariable("FSO_UPDATE_RELEASE_SUFFIX") ?? "";
            string prereleaseChannel = Environment.GetEnvironmentVariable("FSO_UPDATE_PRERELEASE_CHANNEL") ?? "FreeSO Archive Beta";
            string prereleaseSuffix = Environment.GetEnvironmentVariable("FSO_UPDATE_PRERELEASE_SUFFIX") ?? "beta";
            string channelUrl = Environment.GetEnvironmentVariable("FSO_UPDATE_CHANNEL_URL") ?? "";

            string initialVersion = Environment.GetEnvironmentVariable("FSO_UPDATE_INITIAL_VERSION") ?? "v0.1.0";
            string targetsString = Environment.GetEnvironmentVariable("FSO_UPDATE_TARGETS") ?? "windows";

            string publicKey = Environment.GetEnvironmentVariable("FSO_UPDATE_PUBLIC_KEY") ?? "";
            string privateKey = Environment.GetEnvironmentVariable("FSO_UPDATE_PRIVATE_KEY") ?? "";

            RSA? crypto = null;

            if (publicKey.Length > 0 && privateKey.Length > 0)
            {
                crypto = TryGetCrypto(privateKey);
            }

            Console.WriteLine(crypto == null ? "Packaging without signatures." : "Public/private key detected - update zips will be signed.");

            string[] targets = targetsString.Split(',');

            var baseVersion = ParsedVersion.Parse(initialVersion);

            if (baseVersion == null)
            {
                Console.WriteLine("FSO_UPDATE_INITIAL_VERSION isn't in the right format. (should be similar to v1.2.3)");
                return;
            }

            int majorTarget = baseVersion.Value.Major;

            Console.WriteLine("Fetching last release...");

            var releases = await client.Repository.Release.GetAll(authorName, repoName);

            using LibGit2Sharp.Repository gitRepo = new LibGit2Sharp.Repository(Path.Combine(workingDirectory, "../"));

            var branches = gitRepo.Branches;
            var activeBranch = branches.First(x => x.IsCurrentRepositoryHead);

            bool isPrerelease = activeBranch.FriendlyName != primaryBranchName;

            baseVersion = baseVersion.Value.WithSuffix(isPrerelease ? prereleaseSuffix : releaseSuffix);

            // Determine what the last release was for the current channel. (prerelease or otherwise)

            var pastReleases = releases.Where(x => x.Prerelease == isPrerelease).OrderByDescending(x => x.CreatedAt);
            var lastRelease = releases.FirstOrDefault();

            ConventionalCommitsBump bump = ConventionalCommitsBump.Patch;
            var changelog = new StringBuilder();

            bool windowsDelta = false;
            bool macDelta = false;
            bool linuxDelta = false;

            ParsedVersion newVersion;
            string? lastVersionString = null;

            if (lastRelease != null)
            {
                // Determine the last published version
                lastVersionString = lastRelease.TagName;
                var lastVersion = ParsedVersion.Parse(lastVersionString);

                // Try and construct the changelog.
                var lastCommit = GetReleaseCommit(lastRelease, gitRepo);
                if (lastCommit != null && lastVersion != null)
                {
                    Console.WriteLine($"Found previous version: {lastVersion}");
                    Console.WriteLine("Building changelog...");
                    var commitBranches = branches.Where(branch => branch.Commits.Any(x => x.Id == lastCommit.Id));

                    // If the current branch contains the last ref, prefer it. Otherwise just select the first owner of that ref that we find.
                    var commitBranch = commitBranches.Any(x => x.FriendlyName == activeBranch.FriendlyName) ? activeBranch : commitBranches.First();

                    // If the branches are different, find the latest commit that both share.
                    List<LibGit2Sharp.Commit> newCommits = [];
                    if (commitBranch != activeBranch)
                    {
                        changelog.AppendLine($"Switched from branch {commitBranch.FriendlyName} to {activeBranch.FriendlyName} - changes in the source branch may have been reverted.");
                        
                        if (lastVersion.Value.Minor == 0)
                        {
                            bump = ConventionalCommitsBump.Major; // The last release had a breaking change, so undoing it will cause another.
                        }
                        else if (lastVersion.Value.Patch == 0)
                        {
                            bump = ConventionalCommitsBump.Minor; // Same, but for minor.
                        }

                        var latestShared = activeBranch.Commits.FirstOrDefault(a => commitBranch.Commits.Any(b => a.Id == b.Id));

                        if (latestShared != null)
                        {
                            foreach (var commit in activeBranch.Commits)
                            {
                                if (commit.Id == latestShared.Id)
                                {
                                    break;
                                }

                                newCommits.Add(commit);
                            }
                        }
                        else
                        {
                            changelog.AppendLine($"Couldn't find a shared commit between the branches.");
                        }
                    }
                    else
                    {
                        foreach (var commit in activeBranch.Commits)
                        {
                            if (commit.Id == lastCommit.Id)
                            {
                                break;
                            }

                            newCommits.Add(commit);
                        }
                    }

                    // From the commit list, try parse each commit message with conventional commits format, add it to the changelog.
                    // If there are any breaking changes (eg. feat!:) then do a minor bump instead of patch.

                    Console.WriteLine($"Found {newCommits.Count} commits since the last update.");

                    changelog.AppendLine("");

                    foreach (var commit in newCommits)
                    {
                        ConventionalCommits.AddToChangelog(changelog, ref bump, commit);
                    }

                    newVersion = lastVersion.Value.Next(majorTarget, bump);

                    Console.WriteLine($"Downloading client assets for {lastVersion} to create delta...");
                    // Download and extract the assets
                    var manifestAsset = lastRelease.Assets.FirstOrDefault(x => x.Name == $"manifest-{lastVersion.Value}.json");
                    var http = new HttpClient();
                    http.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/octet-stream"));
                    http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", rawToken);

                    if (manifestAsset != null)
                    {
                        Console.WriteLine($"Downloading manifest from {FixAssetUrl(manifestAsset.BrowserDownloadUrl, lastVersionString)}...");
                        var manifestResponse = await http.GetAsync(FixAssetUrl(manifestAsset.BrowserDownloadUrl, lastVersionString));
                        if (manifestResponse.IsSuccessStatusCode)
                        {
                            var content = await manifestResponse.Content.ReadFromJsonAsync<FSOUpdateMetadata>();

                            if (content != null)
                            {
                                windowsDelta = await DownloadLastBuild(http, content.full.windows, workingDirectory, "windows", targets, lastVersionString);
                                macDelta = await DownloadLastBuild(http, content.full.mac, workingDirectory, "mac", targets, lastVersionString);
                                linuxDelta = await DownloadLastBuild(http, content.full.linux, workingDirectory, "linux", targets, lastVersionString);
                            }
                            else
                            {
                                Console.WriteLine($"Manifest failed to parse.");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Couldn't download manifest: {manifestResponse.StatusCode} {manifestResponse.ReasonPhrase}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Manifest asset was missing...");
                    }
                }
                else
                {
                    Console.WriteLine($"Failed to identify last release commit. (was it made manually?)");
                    changelog.AppendLine("Changelog unavailable.");

                    lastVersionString = null;
                    newVersion = baseVersion.Value;
                }
            }
            else
            {
                // There's no delta for this release. Use the initial version.
                Console.WriteLine($"Starting a new release channel with initial version {baseVersion.Value}.");
                changelog.AppendLine("New release channel.");

                newVersion = baseVersion.Value;
            }

            string versionString = newVersion.ToString();
            string channel = isPrerelease ? prereleaseChannel : releaseChannel;
            string changelogString = changelog.ToString();

            Console.WriteLine($"Creating release for new version {versionString}.");

            // Build the version.json
            FSOVersionInfo info = new()
            {
                id = versionString,
                publicKey = publicKey,
                channel = channel,
                channelUrl = channelUrl
            };

            var infoText = JsonConvert.SerializeObject(info);

            // Create the release on GitHub

            bool anyDelta = windowsDelta || macDelta || linuxDelta;

            var release = await client.Repository.Release.Create(authorName, repoName, new NewRelease(versionString)
            {
                Name = $"{versionString} ({channel})",
                Body = $"Changelog:\n\n{changelogString}",
                Prerelease = isPrerelease,
                Draft = true,
                TargetCommitish = activeBranch.Commits.First().Sha,
            });

            var manifest = new FSOUpdateMetadataStandalone()
            {
                id = versionString,
                channel = channel,
                publicKey = publicKey,
                lastid = lastVersionString,
                date = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                server = new FSOUpdateCrossPlatformFile(),
                full = new FSOUpdateCrossPlatformFile(),
                delta = anyDelta ? new FSOUpdateCrossPlatformFile() : null,
                changelog = changelogString,
            };

            Console.WriteLine($"Generating and uploading assets for release targets: {string.Join(", ", targets)}");

            foreach (var target in targets)
            {
                Console.WriteLine($"- {target}");
                string clientPath = Path.Combine(workingDirectory, $"{target}");
                string serverPath = Path.Combine(workingDirectory, $"{target}-server");

                // Insert version.json into the build.
                File.WriteAllText(Path.Combine(clientPath, "version.json"), infoText);
                File.WriteAllText(Path.Combine(serverPath, "version.json"), infoText);

                // Build and upload client/server zips (with encrypted SHA-256 hash)
                Console.WriteLine("  - Client Full Zip...");
                FSOUpdateFile clientInfo = await FolderToZip(client, release, target, versionString, "client", clientPath, crypto);
                Console.WriteLine("  - Server Full Zip...");
                FSOUpdateFile serverInfo = await FolderToZip(client, release, target, versionString, "server", serverPath, crypto);

                manifest.full.SetPlatform(target, clientInfo);
                manifest.server.SetPlatform(target, serverInfo);

                // If this target can build a client delta, do that here.

                if (windowsDelta) // TODO: other targets
                {
                    Console.WriteLine("  - Client Delta:");
                    Console.WriteLine("    Calculating diff...");
                    var diffs = DiffGenerator.GetDiffs(
                        Path.GetFullPath(Path.Combine(workingDirectory, $"{target}-old")),
                        Path.GetFullPath(clientPath));

                    FileDiff[] toZip = [..diffs.Where(x => x.DiffType == FileDiffType.Add || x.DiffType == FileDiffType.Modify)];
                    //build diff folder
                    string deltaDir = Path.Combine(workingDirectory, $"{target}-delta");
                    Directory.CreateDirectory(deltaDir);
                    Console.WriteLine($"    Adding {toZip.Length} new or modified files...");
                    foreach (var diff in toZip)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(deltaDir, diff.Path))!);
                        System.IO.File.Copy(Path.Combine(clientPath, diff.Path), Path.Combine(deltaDir, diff.Path));
                    }

                    var deltaJson = new DeltaJson()
                    {
                        Diffs = [..diffs]
                    };

                    File.WriteAllText(Path.Combine(deltaDir, "delta.json"), JsonConvert.SerializeObject(deltaJson));

                    Console.WriteLine($"    Building delta zip...");
                    FSOUpdateFile deltaInfo = await FolderToZip(client, release, target, versionString, "client-delta", deltaDir, crypto);

                    manifest.delta!.SetPlatform(target, deltaInfo);
                }
            }


            Console.WriteLine($"Finished building update! Uploading final manifest.");

            // Finally, upload the final manifest. This will get added to the update list by the update API.

            var manifestData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(manifest));

            await client.Repository.Release.UploadAsset(release, new ReleaseAssetUpload()
            {
                FileName = $"manifest-{versionString}.json",
                ContentType = "application/json",
                RawData = new MemoryStream(manifestData),
            });

            Console.WriteLine($"Undrafting release...");

            await client.Repository.Release.Edit(authorName, repoName, release.Id, new ReleaseUpdate()
            {
                Draft = false,
                MakeLatest = isPrerelease ? null : MakeLatestQualifier.True,
            });

            Console.WriteLine($"Done.");
        }
    }
}
