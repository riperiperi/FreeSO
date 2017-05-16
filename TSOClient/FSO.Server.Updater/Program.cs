using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

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
            if (args.Length > 0 && args.Any(x => x == "--update"))
            {
                Update(new string[0]);
                args = args.Where(x => x != "--update").ToArray();
            }
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

        static string GetTeamcityLatestURL()
        {
            var config = Config.Default;
            Uri url;
            var baseUri = new Uri(config.TeamCityUrl);
            if (!Uri.TryCreate(baseUri, "guestAuth/app/rest/builds?locator=buildType:" + config.TeamCityProject + ",status:success,count:1,branch:" + config.Branch, out url))
                url = null;

            if (url != null)
            {
                string contents;
                using (var wc = new System.Net.WebClient())
                    contents = wc.DownloadString(url);
                var doc = new XmlDocument();
                doc.LoadXml(contents);
                var builds = doc.GetElementsByTagName("build");
                foreach (XmlNode build in builds)
                {
                    var wholenumber = build.Attributes["number"].Value;
                    var number = wholenumber.Substring(wholenumber.LastIndexOf('-') + 1);
                    return config.TeamCityUrl.TrimEnd('/') + "/repository/download/" + config.TeamCityProject + "/" + build.Attributes["id"].Value + ":id/server-" + number + ".zip?guest=1";
                }
            }

            return null;
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
                url = new Uri(GetTeamcityLatestURL());
                Console.WriteLine("(specifically " + url.ToString() + ")");
                //var baseUri = new Uri(config.TeamCityUrl);
                //if (!Uri.TryCreate(baseUri, "guestAuth/downloadArtifacts.html?buildTypeId=" + config.TeamCityProject + "&buildId=lastSuccessful", out url))
                //    url = null;
            }

            using (var file = File.Open("updateUrl.txt", FileMode.Create, FileAccess.Write))
            {
                var writer = new StreamWriter(file);
                writer.WriteLine(url.ToString().Replace(":id/server-", ":id/client-"));
                writer.Close();
            }

            var wait = new AutoResetEvent(false);
            if (url != null) {
                if (Directory.Exists("selfUpdate/")) Directory.Delete("selfUpdate/", true);
                Directory.CreateDirectory("selfUpdate/");
                Console.WriteLine("Downloading artifacts...");
                var client = new WebClient();
                client.DownloadFileCompleted += (sender, evt) =>
                {
                    var file = "selfUpdate/artifact.zip";
                    Console.WriteLine("Extracting " + file + "...");
                    var archive = ZipFile.OpenRead(file);
                    var entries = archive.Entries;
                    foreach (var entry in entries)
                    {
                        var targPath = Path.Combine("./", entry.FullName);
                        if (File.Exists(targPath) && IgnoreFiles.Contains(entry.FullName)) continue;
                        Directory.CreateDirectory(Path.GetDirectoryName(targPath));
                        try
                        {
                            entry.ExtractToFile(targPath, true);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Could not replace " + targPath + "!");
                        }
                    }
                    archive.Dispose();
                    Directory.Delete("selfUpdate/", true);
                    Console.WriteLine("Update Complete!");
                    wait.Set();
                };

                client.DownloadFileAsync(url, "selfUpdate/artifact.zip");
            }

            wait.WaitOne();
        }
    }
}
