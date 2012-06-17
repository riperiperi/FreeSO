using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using SimsLib.FAR3;

namespace XNAWinForms
{
    class ContentManager
    {
        private static Dictionary<ulong, string> m_Resources;
        private static bool initComplete = false;
        private static Random m_Rand;
        private static Dictionary<string, FAR3Archive> m_Archives;

        public static Dictionary<ulong, string> Resources
        {
            get { return m_Resources; }
        }

        static ContentManager()
        {
            m_Archives = new Dictionary<string, FAR3Archive>();
            m_Rand = new Random(0);
            FAR3Archive packingSlips = new FAR3Archive(GlobalSettings.Default.StartupPath + "packingslips\\packingslips.dat");

            List<KeyValuePair<uint, byte[]>> entries = packingSlips.GetAllEntries();

            m_Resources = new Dictionary<ulong, string>();
            foreach (KeyValuePair<uint, byte[]> kvp in entries)
            {
                BinaryReader br = new BinaryReader(new MemoryStream(kvp.Value));
                br.BaseStream.Position = 50;
                string path = br.ReadString();
                br.BaseStream.Position += 8;
                ulong id = Endian.SwapUInt64(br.ReadUInt64());
                m_Resources.Add(id, path);
            }
            KeyValuePair<uint, byte[]> vp = entries[35854];
            string s = new List<string>(m_Resources.Values)[35854];
            initComplete = true;
        }

        public static byte[] GetResourceFromLongID(ulong id)
        {
            while (!initComplete) ;
            if (m_Resources.ContainsKey(id))
            {
                string path = m_Resources[id].Replace("./", "");
                if (!File.Exists(path))
                {
                    string[] pathSections = path.Split(new char[] { '/' });
                    int directoryIdx = 0;
                    if (path.Contains("/heads/") || path.Contains("/hands/") || path.Contains("/bodies/") || path.Contains("/accessories/"))
                        directoryIdx = Array.FindLastIndex<string>(pathSections, delegate(string it) { if (it.CompareTo("avatardata") == 0) { return true; } return false; }) + 2;
                    else
                        directoryIdx = Array.FindLastIndex<string>(pathSections, delegate(string it) { if (it.CompareTo("TSOClient") == 0) { return true; } return false; }) + 2;
                    string directoryName = pathSections[directoryIdx];
                    path = path.Remove(path.LastIndexOf('/'));
                    string archivePath = GlobalSettings.Default.StartupPath + '/' + path.Remove((path.LastIndexOf(pathSections[directoryIdx]))) + directoryName + '/' + directoryName + ".dat";

                    if (!m_Archives.ContainsKey(archivePath))
                    {
                        FAR3Archive archive = new FAR3Archive(archivePath);
                        m_Archives.Add(archivePath, archive);
                        return archive[pathSections[pathSections.Length - 1]];
                    }
                    else
                    {
                        return m_Archives[archivePath].GetItemByID((uint)(id >> 32));
                    }
                }
                else
                {
                    return File.ReadAllBytes(GlobalSettings.Default.StartupPath + path);
                }
            }
            return new byte[0];
        }
    }
}
