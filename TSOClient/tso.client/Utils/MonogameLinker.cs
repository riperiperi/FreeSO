using FSO.LotView;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FSO.Client.Utils
{
    public class MonogameLinker
    {
        //detects OS and copies the correct version of monogame into the parent directory.
        //there is probably a better way to do this that doesn't mess with multiple clients

        public static string AssemblyDir = "./";

        public static bool Link(bool preferDX11)
        {
            OperatingSystem os = Environment.OSVersion;
            PlatformID pid = os.Platform;

            bool linux = false;
            if (pid == PlatformID.MacOSX || pid == PlatformID.Unix) linux = true;

            if (linux && preferDX11)
            {
                //MessageBox.Show("DirectX is only available on Windows, dummy!");
                preferDX11 = false;
            }

            try {
                string contentDir = "Content/OGL/";
                string monogameDir = "Monogame/WindowsGL/";
                if (!linux)
                {
                    if (preferDX11)
                    {
                        contentDir = "Content/DX/";
                        monogameDir = "Monogame/Windows/";
                    }
                }
                //Check if MacOS by checkking user directory. Because PlatformID.MacOSX is not true on OS X.
                else if (Directory.Exists("/Users"))
                {
                    monogameDir = "Monogame/MacOS/";
                }
                else
                {
                    monogameDir = "Monogame/Linux/";
                }

                //DirectoryCopy(contentDir, "Content/", true);
                if (File.Exists("Monogame.Framework.dll")) File.Delete("Monogame.Framework.dll");

                AssemblyDir = monogameDir;
            } catch (Exception e)
            {
                //MessageBox.Show("Unable to link Monogame. Continuing... ("+e.ToString()+")");
            }

            return preferDX11;
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, true);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }
    }
}
