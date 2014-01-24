using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using LogThis;
using KISS;

namespace PDPatcher
{
    public class FileManager
    {
        /// <summary>
        /// Takes a backup of all files in the client's manifest.
        /// </summary>
        /// <param name="Manifest">The client's manifest.</param>
        /// <param name="WorkingDir">The client's residing directory.</param>
        public static void Backup(ManifestFile Manifest, string WorkingDir)
        {
            try
            {
                if (!Directory.Exists(WorkingDir + "Backup"))
                    Directory.CreateDirectory(WorkingDir + "Backup");

                foreach (PatchFile PFile in Manifest.PatchFiles)
                {
                    if (File.Exists(WorkingDir + PFile.Address))
                    {
                        FileManager.CreateDirectory(WorkingDir + "Backup\\" + PFile.Address);
                        File.Copy(WorkingDir + PFile.Address, WorkingDir + "Backup\\" + PFile.Address);
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogThis("Exception in FileManager.Backup:" + e.ToString(), eloglevel.error); 
            }
        }

        /// <summary>
        /// Creates a directory if it does not yet exist.
        /// </summary>
        /// <param name="FilePath">Path of the director(ies) to create.</param>
        public static void CreateDirectory(string FilePath)
        {
            string Dir = FilePath.Replace(Path.GetFileName(FilePath), "");

            if (!Directory.Exists(Dir))
                Directory.CreateDirectory(Dir);
        }
    }
}
