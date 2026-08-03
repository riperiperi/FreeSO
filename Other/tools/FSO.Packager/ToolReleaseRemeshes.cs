using FSO.Files.Formats.DBPF;
using FSO.Files.FSO;
using FSO.Files.RC;
using Newtonsoft.Json;
using System.Security.Cryptography;

namespace FSO.Packager
{
    internal class ToolReleaseRemeshes : ITool
    {
        private readonly ReleaseRemeshesOptions Options;
        private RSA? Crypto;
        private string ReleaseAssetsBase = "";
        private FSORemeshChannel RemeshChannel = new();


        public ToolReleaseRemeshes(ReleaseRemeshesOptions opts)
        {
            Options = opts;
        }

        private FSORemeshFile GetRemeshFile(string path, string url)
        {
            // Build a zip from the input directory.

            var data = File.ReadAllBytes(path);
            var shaHash = SHA256.HashData(data);

            return new FSORemeshFile()
            {
                url = url,
                size = data.Length,
                hash = Convert.ToBase64String(shaHash),
                signature = Crypto != null ? Convert.ToBase64String(Crypto.SignHash(shaHash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)) : "",
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

        private static string GetFormatString(FSO3DPackageTextureFormat format)
        {
            return format switch
            {
                FSO3DPackageTextureFormat.Credits => "credits",
                FSO3DPackageTextureFormat.Dxt => "dxt",
                FSO3DPackageTextureFormat.Png => "png",
                _ => throw new Exception("Unknown format")
            };
        }

        private void AddToChannel(FSORemeshFile file, FSO3DPackageMetadata meta)
        {
            RemeshChannel.name = meta.Name;
            RemeshChannel.description = meta.Description;
            RemeshChannel.url = meta.Url;

            switch (meta.Format)
            {
                case FSO3DPackageTextureFormat.Dxt:
                    RemeshChannel.dxt = file;
                    break;

                case FSO3DPackageTextureFormat.Png:
                    RemeshChannel.png = file;
                    break;
            }
        }

        private void ProcessRemesh(string game, FSO3DPackageTextureFormat format)
        {
            string formatString = GetFormatString(format);
            string filename = $"{game}-remeshes-{formatString}.dat";

            var path = Path.Combine(Options.SourceDirectory, filename);

            if (File.Exists(path))
            {
                var dbpf = new DBPFFile(path);

                var creditsData = dbpf.GetItemByID(DBPFTypeID.FSO3DCredits, 0);

                var credits = new FSO3DCredits();
                using var creditsStream = new MemoryStream(creditsData);
                credits.Read(creditsStream);

                // Add version related fields
                var meta = credits.Metadata;
                meta.Format = format;
                meta.ChannelName = RemeshChannel.channel ?? "";
                meta.Version = RemeshChannel.version;
                meta.PublicKey = RemeshChannel.publicKey ?? "";

                using var creditsOut = new MemoryStream();
                credits.Write(creditsOut);

                dbpf.AddOrReplace(0, DBPFTypeID.FSO3DCredits, DBPFGroupID.RemeshPackage, creditsOut.ToArray());

                // Save the modified file.
                using (var mem = new MemoryStream())
                {
                    dbpf.Write(mem);
                    dbpf.Dispose();

                    File.WriteAllBytes(path, mem.ToArray());
                }

                if (format != FSO3DPackageTextureFormat.Credits)
                {
                    // Generate metadata for the updater

                    var metaObj = GetRemeshFile(path, ReleaseAssetsBase + filename);

                    AddToChannel(metaObj, meta);
                }
            }
        }

        private void ProcessGame(string game)
        {
            RemeshChannel = new()
            {
                publicKey = RemeshChannel.publicKey,
                channel = RemeshChannel.channel,
                version = RemeshChannel.version
            };

            Console.WriteLine($"Processing packages for {game}.");
            ProcessRemesh(game, FSO3DPackageTextureFormat.Dxt);
            ProcessRemesh(game, FSO3DPackageTextureFormat.Png);
            ProcessRemesh(game, FSO3DPackageTextureFormat.Credits);

            // Output metadata for this game to the source directory

            string metaFilename = $"{game}-remeshes.json";
            var metaPath = Path.Combine(Options.SourceDirectory, metaFilename);

            var result = JsonConvert.SerializeObject(RemeshChannel);
            File.WriteAllText(metaPath, result);

            Console.WriteLine($"Output version metadata for {game}!");
        }

        public int Run()
        {
            var games = Options.Games.Split(',');

            string versionString = Environment.GetEnvironmentVariable("FSO_REMESH_VERSION") ?? "";
            string assetsBaseString = Environment.GetEnvironmentVariable("FSO_REMESH_ASSETS_BASE") ?? "https://github.com/riperiperi/FSO.Remeshes/releases/download/";
            ReleaseAssetsBase = assetsBaseString + versionString + "/";

            string publicKey = Environment.GetEnvironmentVariable("FSO_UPDATE_PUBLIC_KEY") ?? "";
            string privateKey = Environment.GetEnvironmentVariable("FSO_UPDATE_PRIVATE_KEY") ?? "";

            RSA? crypto = null;

            if (publicKey.Length > 0 && privateKey.Length > 0)
            {
                crypto = TryGetCrypto(privateKey);
            }

            Crypto = crypto;

            var versionSplit = versionString.Split('.', 2);

            if (!int.TryParse(versionSplit[1], out var version))
            {
                Console.WriteLine($"Incorrect format for version. (got {versionString}, should be like prod.1)");
                return 1;
            }

            RemeshChannel.publicKey = crypto != null ? publicKey : "";
            RemeshChannel.channel = versionSplit[0];
            RemeshChannel.version = version;

            Console.WriteLine(crypto == null ? "Packaging without signatures." : "Public/private key detected - update zips will be signed.");

            foreach (var game in games)
            {
                ProcessGame(game);
            }

            Console.WriteLine("Done!");

            return 0;
        }
    }
}
