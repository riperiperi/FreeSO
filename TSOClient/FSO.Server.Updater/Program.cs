using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FSO.Server.Watchdog
{
    public class Program
    {
        //really simple console application to retrieve and extract a server distribution from teamcity.

        static void Main(string[] args)
        {
            var restart = true;
            while (restart)
            {
                var setup = AppDomain.CurrentDomain.SetupInformation;
                setup.ConfigurationFile = Path.Combine(Path.GetDirectoryName(setup.ConfigurationFile), "server.exe.config");
                var childDomain = AppDomain.CreateDomain("serverDomain", null, setup);
                var result = childDomain.ExecuteAssembly("server.exe", args);
                AppDomain.Unload(childDomain);

                if (result > 1)
                {
                    //safe exit.
                    switch (result)
                    {
                        case 2:
                            restart = false; break;
                        case 4:
                            Update(new string[0]); break;
                    }
                }
            }
        }

        static void Update(string[] args)
        {
            if (args.Length < 2)
            {
                var nargs = new string[2];
                nargs[0] = (args.Length == 0) ? "http://servo.freeso.org" : args[0];
                nargs[1] = "FreeSO_TsoClient";
                args = nargs;
            }
            var teamcityUrl = args[0]; //http://servo.freeso.org
            var projectName = args[1]; //FreeSO_TsoClient
            Console.WriteLine("Fetching update from " + teamcityUrl + "/" + projectName + "...");

            var wait = new AutoResetEvent(false);

            Uri result = null;
            var baseUri = new Uri(teamcityUrl);
            if (Uri.TryCreate(baseUri, "guestAuth/downloadArtifacts.html?buildTypeId=" + projectName + "&buildId=lastSuccessful", out result)) {
                if (Directory.Exists("selfUpdate/")) Directory.Delete("selfUpdate/", true);
                Directory.CreateDirectory("selfUpdate/");
                Directory.CreateDirectory("selfUpdate/artifacts");
                Console.WriteLine("Downloading artifacts from teamcity...");
                var client = new WebClient();
                client.DownloadFileCompleted += (sender, evt) =>
                {
                    ZipFile.ExtractToDirectory("selfUpdate/artifacts.zip", "selfUpdate/artifacts/");
                    var files = Directory.GetFiles("selfUpdate/artifacts/");
                    foreach (var file in files)
                    {
                        Console.WriteLine("Extracting "+file+"...");
                        var archive = ZipFile.OpenRead(file);
                        var entries = archive.Entries;
                        foreach (var entry in entries)
                        {
                            var targPath = Path.Combine("./", entry.FullName);
                            Directory.CreateDirectory(Path.GetDirectoryName(targPath));
                            entry.ExtractToFile(targPath, true);
                        }
                        archive.Dispose();
                    }
                    Directory.Delete("selfUpdate/", true);
                    Console.WriteLine("Update Complete!");
                    wait.Set();
                };

                client.DownloadFileAsync(result, "selfUpdate/artifacts.zip");
            }

            wait.WaitOne();
        }
    }
}
