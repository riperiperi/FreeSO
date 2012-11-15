using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Linq;
using Microsoft.Win32;
using System.IO;
using SimsLib.FAR3;

namespace Mr.Shipper
{
    class Program
    {
        private static int[] m_RandomNumbers = new int[200];
        private static int m_RandomCounter = 0;

        static void Main(string[] args)
        {
            Random Rnd = new Random();
            m_RandomNumbers = Enumerable.Range(10000, 10200).OrderBy(i => Rnd.Next()).ToArray();

            //Find the path to TSO on the user's system.
            RegistryKey softwareKey = Registry.LocalMachine.OpenSubKey("SOFTWARE");
            if (Array.Exists(softwareKey.GetSubKeyNames(), delegate(string s) { return s.CompareTo("Maxis") == 0; }))
            {
                RegistryKey maxisKey = softwareKey.OpenSubKey("Maxis");
                if (Array.Exists(maxisKey.GetSubKeyNames(), delegate(string s) { return s.CompareTo("The Sims Online") == 0; }))
                {
                    RegistryKey tsoKey = maxisKey.OpenSubKey("The Sims Online");
                    string installDir = (string)tsoKey.GetValue("InstallDir");
                    installDir += "\\TSOClient\\";
                    GlobalSettings.Default.StartupPath = installDir;
                }
                else
                {
                    Console.WriteLine("Error TSO was not found on your system.");
                    Console.ReadLine();
                    return;
                }
            }
            else
            {
                Console.WriteLine("Error: No Maxis products were found on your system.");
                Console.ReadLine();
                return;
            }

            Console.WriteLine("Generating uigraphics database...");
            GenerateUIGraphicsDatabase();
            Console.WriteLine("Done!");
            Console.ReadLine();
        }

        /// <summary>
        /// Generates a database of the files in the uigraphics folder,
        /// as well as a *.cs file with an enumeration of the same files
        /// and their corresponding FileIDs.
        /// </summary>
        private static void GenerateUIGraphicsDatabase()
        {
            string[] Dirs = Directory.GetDirectories(GlobalSettings.Default.StartupPath + "uigraphics\\");
            Dictionary<Far3Entry, string> UIEntries = new Dictionary<Far3Entry, string>();

            foreach (string Dir in Dirs)
            {
                string[] Files = Directory.GetFiles(Dir);

                foreach (string Fle in Files)
                {
                    if (Fle.Contains(".dat"))
                    {
                        FAR3Archive Archive = new FAR3Archive(Fle);

                        foreach (Far3Entry Entry in Archive.GetAllFAR3Entries())
                            UIEntries.Add(Entry, Fle);
                    }
                    else
                    {
                        //This works for now, as there are always less than 100 unarchived files.
                        if (m_RandomCounter < 200)
                            m_RandomCounter++;

                        Far3Entry Entry = new Far3Entry();
                        Entry.Filename = Path.GetFileName(Fle).Replace(".png", "").Replace(".bmp", "");
                        Entry.FileID = (uint)m_RandomNumbers[m_RandomCounter];

                        CheckCollision(Entry.FileID, UIEntries);

                        UIEntries.Add(Entry, Fle);
                    }
                }
            }

            Directory.CreateDirectory("packingslips");
            StreamWriter Writer = new StreamWriter(File.Create("packingslips\\UIFileIDs.cs"));

            Writer.WriteLine("namespace TSOClient.ContentManager");
            Writer.WriteLine("{");
            Writer.WriteLine("  enum UIFileIDs");
            Writer.WriteLine("  {");

            foreach (KeyValuePair<Far3Entry, string> KVP in UIEntries)
            {
                Writer.WriteLine("      " + KVP.Key.Filename.Replace(".bmp", "").Replace(".tga", "").Replace("'", "")
                    + " = 0x" + string.Format("{0:X}", KVP.Key.FileID) + ",");
            }

            Writer.WriteLine("  }");
            Writer.WriteLine("}");
            Writer.Close();

            Writer = new StreamWriter(File.Create("packingslips\\uigraphics.xml"));
            Writer.WriteLine("<?xml version=\"1.0\"?>");
            Writer.WriteLine("<AssetList>");

            //For some really weird reason, "key" and "assetID" are written in reverse order...
            foreach (KeyValuePair<Far3Entry, string> KVP in UIEntries)
            {
                if (KVP.Value.Contains(".dat"))
                {
                    Writer.WriteLine("  " + "<DefineAssetString key=\"" + Path.GetFileName(KVP.Value) + 
                        "\" assetID=\"" + KVP.Key.FileID + "\"/>");
                }
                else
                {
                    DirectoryInfo DirInfo = new DirectoryInfo(KVP.Value);
                    Writer.WriteLine("  " + "<DefineAssetString key=\"" + DirInfo.Parent + "\\" + 
                        Path.GetFileName(KVP.Value) + "\" assetID=\"" + KVP.Key.FileID + "\"/>");
                }
            }

            Writer.WriteLine("</AssetList>");
            Writer.Close();
        }

        /// <summary>
        /// Checks for collisions between existing and generated IDs, and prints out if any were found.
        /// </summary>
        /// <param name="FileID">The generated ID to check.</param>
        /// <param name="UIEntries">The entries to check.</param>
        /// <returns>True if any collisions were found.</returns>
        private static bool CheckCollision(uint FileID, Dictionary<Far3Entry, string> UIEntries)
        {
            foreach(KeyValuePair<Far3Entry, string> KVP in UIEntries)
            {
                if (KVP.Key.FileID == FileID)
                {
                    Console.WriteLine("Found ID collision: " + FileID);
                    return true;
                }
            }

            return false;
        }
    }
}
