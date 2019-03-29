using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FSO.Patcher
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            var path = UpdatePath();
            var platform = Environment.OSVersion.Platform;
            if (platform == PlatformID.Unix || platform == PlatformID.MacOSX)
            {
                //console only application
                var patcher = new CLIPatcher(path, args);
                patcher.Begin();
            }
            else
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                Directory.SetCurrentDirectory(baseDir);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new FormsPatcher(path, args));
            }
        }

        static List<string> UpdatePath()
        {
            try
            {
                var files = Directory.GetFiles("PatchFiles/");
                return files.Where(x => x.EndsWith(".zip") && !x.EndsWith("patch.zip")).OrderBy(x => {
                    var match = Regex.Match(x, @"\d+").Value ?? "200";
                    if (match == "") match = "200";
                    return int.Parse(match);
                    }
                ).ToList();
            }
            catch (Exception)
            {
                return new List<string>();
            }
        }
    }
}
