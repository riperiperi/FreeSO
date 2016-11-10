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

        static HashSet<string> IgnoreFiles = new HashSet<string>()
        {
            "watchdog.exe",
            "config.json",
            "watchdog.ini",
            "Ninject.dll",
            "Ninject.xml",
            "NLog.config"
        };

        static int Main(string[] args)
        {
            var restart = true;
            while (restart)
            {
                var setup = AppDomain.CurrentDomain.SetupInformation;
                setup.ConfigurationFile = Path.Combine(Path.GetDirectoryName(setup.ConfigurationFile), "server.exe.config");
                var childDomain = AppDomain.CreateDomain("serverDomain", null, setup);
                int result = 3;
                try
                {
                    result = childDomain.ExecuteAssembly("server.exe", args);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unhandled exception occurred!");
                    Console.WriteLine(e.ToString());
                    e.ToString();
                }
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
                return result; 
                //was trying to do something smart here with appdomains to reload the app without closing it
                //but it breaks mono... so to loop running the application you need to use a shell script.
                //just loop while this watcher doesn't return 2 (shutdown)
            }
            return 0;
        }

        static void Update(string[] args)
        {
            var config = Config.Default;
            Uri url;

            if (!config.UseTeamCity)
            {
                Console.WriteLine("Fetching update from " + config.NormalUpdateUrl + "...");
                url = new Uri(config.NormalUpdateUrl);
            }
            else
            {
                Console.WriteLine("Fetching update from " + config.TeamCityUrl + "/" + config.TeamCityProject + "...");
                var baseUri = new Uri(config.TeamCityUrl);
                if (!Uri.TryCreate(baseUri, "guestAuth/downloadArtifacts.html?buildTypeId=" + config.TeamCityProject + "&buildId=lastSuccessful", out url))
                    url = null;
            }

            var wait = new AutoResetEvent(false);
            if (url != null) {
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
                            if (IgnoreFiles.Contains(entry.FullName)) continue;
                            var targPath = Path.Combine("./", entry.FullName);
                            Directory.CreateDirectory(Path.GetDirectoryName(targPath));
                            try
                            {
                                entry.ExtractToFile(targPath, true);
                            } catch (Exception e)
                            {
                                Console.WriteLine("Could not replace " + targPath + "!");
                            }
                        }
                        archive.Dispose();
                    }
                    Directory.Delete("selfUpdate/", true);
                    Console.WriteLine("Update Complete!");
                    wait.Set();
                };

                client.DownloadFileAsync(url, "selfUpdate/artifacts.zip");
            }

            wait.WaitOne();
        }
    }
}
