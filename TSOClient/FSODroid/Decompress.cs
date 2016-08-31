//from https://gist.github.com/Vaikesh/471eb223d0a5ee37944a, for simplicity

using System;
using Android.Util;
using Java.IO;
using Java.Util.Zip;
using System.IO;

namespace ZipManager
{
    public class Decompress
    {
        String _zipFile;
        String _location;

        public event Action<string> OnContinue;

        public Decompress(String zipFile, String location)
        {
            _zipFile = zipFile;
            _location = location;
            DirChecker("");
        }

        void DirChecker(String dir)
        {
            Directory.CreateDirectory(_location + dir);
        }

        public void UnZip()
        {
            byte[] buffer = new byte[65536];
            var fileInputStream = System.IO.File.OpenRead(_zipFile);
            
            var zipInputStream = new ZipInputStream(fileInputStream);
            ZipEntry zipEntry = null;
            int j = 0;
            int bestRead = 0;
            while ((zipEntry = zipInputStream.NextEntry) != null)
            {
                OnContinue?.Invoke(zipEntry.Name + ", " + bestRead);
                if (zipEntry.IsDirectory)
                {
                    DirChecker(zipEntry.Name);
                }
                else
                {
                    if (System.IO.File.Exists(_location + zipEntry.Name)) System.IO.File.Delete(_location + zipEntry.Name);
                    var foS = new FileOutputStream(_location + zipEntry.Name, true);
                    int read;
                    while ((read = zipInputStream.Read(buffer)) > 0)
                    {
                        if (read > bestRead) bestRead = read;
                        foS.Write(buffer, 0, read);
                    }
                    foS.Close();
                    zipInputStream.CloseEntry();
                }
            }
            zipInputStream.Close();
            fileInputStream.Close();
        }

    }
}