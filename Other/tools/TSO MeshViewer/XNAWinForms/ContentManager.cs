/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Threading;
using System.IO;
using System.Linq;
using System.Xml;
using SimsLib.FAR3;
using SimsLib.IFF;
using Microsoft.Xna.Framework.Graphics;
using SimsLib.FAR1;
using LogThis;

namespace Dressup
{
    public class ContentManager
    {
        private const int m_CACHESIZE = 104857600; //100 megabytes.
        private static int m_CurrentCacheSize = 0;

        private static Dictionary<ulong, string> m_Resources;
        private static Dictionary<ulong, byte[]> m_LoadedResources;
        private static bool initComplete = false;

        private static ManualResetEvent m_ResetEvent = new ManualResetEvent(false);

        public static Dictionary<ulong, string> Resources
        {
            get { return m_Resources; }
        }
        
        static ContentManager()
        {
            m_Resources = new Dictionary<ulong, string>();
            m_LoadedResources = new Dictionary<ulong, byte[]>();

            XmlDataDocument AnimTable = new XmlDataDocument(); 
            AnimTable.Load(GlobalSettings.Default.StartupPath + "packingslips\\animtable.xml");

            XmlNodeList NodeList = AnimTable.GetElementsByTagName("DefineAssetString");

            foreach (XmlNode Node in NodeList)
            {
                string HexID = RemovePadding(Node.Attributes["assetID"].Value);
                ulong FileID = Convert.ToUInt64(HexID, 16);
                //TODO: Figure out when to use avatardata2 and avatardata3...
                string FileName = GlobalSettings.Default.StartupPath + "avatardata\\animations\\animations.dat";

                if (!m_Resources.ContainsKey(FileID))
                    m_Resources.Add(FileID, FileName);
            }

            XmlDataDocument UIGraphicsTable = new XmlDataDocument();
            UIGraphicsTable.Load(GlobalSettings.Default.StartupPath + "packingslips\\uigraphics.xml");

            NodeList = UIGraphicsTable.GetElementsByTagName("DefineAssetString");

            foreach (XmlNode Node in NodeList)
            {
                string HexID = RemovePadding(Node.Attributes["assetID"].Value);
                ulong FileID = Convert.ToUInt64(HexID, 16);

                string FileName = "";

                if (Node.Attributes["key"].Value.Contains(".dat"))
                {
                    FileName = GlobalSettings.Default.StartupPath + Node.Attributes["key"].Value;
                }
                else
                    FileName = GlobalSettings.Default.StartupPath + Node.Attributes["key"].Value;

                if(!m_Resources.ContainsKey(FileID))
                    m_Resources.Add(FileID, FileName);
            }

            XmlDataDocument CollectionsTable = new XmlDataDocument();
            CollectionsTable.Load(GlobalSettings.Default.StartupPath + "packingslips\\collections.xml");

            NodeList = CollectionsTable.GetElementsByTagName("DefineAssetString");

            foreach (XmlNode Node in NodeList)
            {
                string HexID = RemovePadding(Node.Attributes["assetID"].Value);
                ulong FileID = Convert.ToUInt64(HexID, 16);
                string FileName = "";

                if (Node.Attributes["key"].Value.Contains(".dat"))
                {
                    FileName = GlobalSettings.Default.StartupPath + Node.Attributes["key"].Value;
                }

                if (!m_Resources.ContainsKey(FileID))
                    m_Resources.Add(FileID, FileName);
            }

            XmlDataDocument PurchasablesTable = new XmlDataDocument();
            PurchasablesTable.Load(GlobalSettings.Default.StartupPath + "packingslips\\purchasables.xml");

            NodeList = PurchasablesTable.GetElementsByTagName("DefineAssetString");

            foreach (XmlNode Node in NodeList)
            {
                string HexID = RemovePadding(Node.Attributes["assetID"].Value);
                ulong FileID = Convert.ToUInt64(HexID, 16);
                string FileName = "";

                if (Node.Attributes["key"].Value.Contains(".dat"))
                {
                    FileName = GlobalSettings.Default.StartupPath + Node.Attributes["key"].Value;
                }

                if (!m_Resources.ContainsKey(FileID))
                    m_Resources.Add(FileID, FileName);
            }

            XmlDataDocument OutfitsTable = new XmlDataDocument();
            OutfitsTable.Load(GlobalSettings.Default.StartupPath + "packingslips\\alloutfits.xml");

            NodeList = OutfitsTable.GetElementsByTagName("DefineAssetString");

            foreach (XmlNode Node in NodeList)
            {
                string HexID = RemovePadding(Node.Attributes["assetID"].Value);
                ulong FileID = Convert.ToUInt64(HexID, 16);
                string FileName = "";

                if (Node.Attributes["key"].Value.Contains(".dat"))
                {
                    FileName = GlobalSettings.Default.StartupPath + Node.Attributes["key"].Value;
                }

                if (!m_Resources.ContainsKey(FileID))
                    m_Resources.Add(FileID, FileName);
            }

            XmlDataDocument AppearancesTable = new XmlDataDocument();
            AppearancesTable.Load(GlobalSettings.Default.StartupPath + "packingslips\\appearances.xml");

            NodeList = AppearancesTable.GetElementsByTagName("DefineAssetString");

            foreach (XmlNode Node in NodeList)
            {
                string HexID = RemovePadding(Node.Attributes["assetID"].Value);
                ulong FileID = Convert.ToUInt64(HexID, 16);
                string FileName = "";

                if (Node.Attributes["key"].Value.Contains(".dat"))
                {
                    FileName = GlobalSettings.Default.StartupPath + Node.Attributes["key"].Value;
                }

                if (!m_Resources.ContainsKey(FileID))
                    m_Resources.Add(FileID, FileName);
            }

            XmlDataDocument ThumbnailsTable = new XmlDataDocument();
            ThumbnailsTable.Load(GlobalSettings.Default.StartupPath + "packingslips\\thumbnails.xml");

            NodeList = ThumbnailsTable.GetElementsByTagName("DefineAssetString");

            foreach (XmlNode Node in NodeList)
            {
                ulong FileID = Convert.ToUInt64(Node.Attributes["assetID"].Value, 16);
                string FileName = "";

                if (Node.Attributes["key"].Value.Contains(".dat"))
                {
                    FileName = GlobalSettings.Default.StartupPath + Node.Attributes["key"].Value;
                }

                if (!m_Resources.ContainsKey(FileID))
                    m_Resources.Add(FileID, FileName);
            }

            XmlDataDocument MeshTable = new XmlDataDocument();
            MeshTable.Load(GlobalSettings.Default.StartupPath + "packingslips\\meshes.xml");

            NodeList = MeshTable.GetElementsByTagName("DefineAssetString");

            foreach (XmlNode Node in NodeList)
            {
                string HexID = RemovePadding(Node.Attributes["assetID"].Value);
                ulong FileID = Convert.ToUInt64(HexID, 16);
                string FileName = "";

                if (Node.Attributes["key"].Value.Contains(".dat"))
                {
                    FileName = GlobalSettings.Default.StartupPath + Node.Attributes["key"].Value;
                }

                if (!m_Resources.ContainsKey(FileID))
                    m_Resources.Add(FileID, FileName);
            }

            XmlDataDocument TextureTable = new XmlDataDocument();
            TextureTable.Load(GlobalSettings.Default.StartupPath + "packingslips\\textures.xml");

            NodeList = TextureTable.GetElementsByTagName("DefineAssetString");

            foreach (XmlNode Node in NodeList)
            {
                string HexID = RemovePadding(Node.Attributes["assetID"].Value);
                ulong FileID = Convert.ToUInt64(HexID, 16);
                string FileName = "";

                if (Node.Attributes["key"].Value.Contains(".dat"))
                {
                    FileName = GlobalSettings.Default.StartupPath + Node.Attributes["key"].Value;
                }

                if (!m_Resources.ContainsKey(FileID))
                    m_Resources.Add(FileID, FileName);
            }

            XmlDataDocument BindingsTable = new XmlDataDocument();
            BindingsTable.Load(GlobalSettings.Default.StartupPath + "packingslips\\bindings.xml");

            NodeList = BindingsTable.GetElementsByTagName("DefineAssetString");

            foreach (XmlNode Node in NodeList)
            {
                string HexID = RemovePadding(Node.Attributes["assetID"].Value);
                ulong FileID = Convert.ToUInt64(HexID, 16);
                string FileName = "";

                if (Node.Attributes["key"].Value.Contains(".dat"))
                {
                    FileName = GlobalSettings.Default.StartupPath + Node.Attributes["key"].Value;
                }

                if (!m_Resources.ContainsKey(FileID))
                    m_Resources.Add(FileID, FileName);
            }

            XmlDataDocument HandgroupsTable = new XmlDataDocument();
            HandgroupsTable.Load(GlobalSettings.Default.StartupPath + "packingslips\\handgroups.xml");

            NodeList = HandgroupsTable.GetElementsByTagName("DefineAssetString");

            foreach (XmlNode Node in NodeList)
            {
                string HexID = RemovePadding(Node.Attributes["assetID"].Value);
                ulong FileID = Convert.ToUInt64(HexID, 16);
                string FileName = "";

                if (Node.Attributes["key"].Value.Contains(".dat"))
                {
                    FileName = GlobalSettings.Default.StartupPath + Node.Attributes["key"].Value;
                }

                if (!m_Resources.ContainsKey(FileID))
                    m_Resources.Add(FileID, FileName);
            }

            m_Resources.Add(0x100000005, GlobalSettings.Default.StartupPath + "avatardata\\skeletons\\skeletons.dat");

            initComplete = true;
        }

        private static string RemovePadding(string HexNumber)
        {
            HexNumber = HexNumber.Replace("0x", "");

            //I think the idea is that no ID starts with 0, from looking at packingslips.txt...
            HexNumber = HexNumber.TrimStart("0".ToCharArray());

            return "0x" + HexNumber;
        }

        public static byte[] GetResourceFromLongID(ulong ID)
        {
            byte[] Resource;

            while (!initComplete) ;
            //Resource hasn't already been loaded...
            if (!m_LoadedResources.TryGetValue(ID, out Resource))
            {
                string path = m_Resources[ID];

                FAR3Archive Archive = new FAR3Archive(path);

                Resource = Archive.GetItemByID(ID);
                return Resource;
            }
            else
                return m_LoadedResources[ID];
        }

        public byte[] this[ulong FileID]
        {
            get
            {
                byte[] Resource;

                //Resource hasn't already been loaded...
                if (!m_LoadedResources.TryGetValue(FileID, out Resource))
                {
                    string path = m_Resources[FileID].Replace("./", "");
                    if (!File.Exists(path))
                    {
                        string[] pathSections = path.Split(new char[] { '/' });
                        string directoryName = pathSections[pathSections.Length - 2];
                        string archivePath = GlobalSettings.Default.StartupPath + path.Remove(path.LastIndexOf('/') + 1) + directoryName + ".dat";

                        FAR3Archive archive = new FAR3Archive(archivePath);
                        return archive[pathSections[pathSections.Length - 1]];
                    }
                    else
                    {
                        Resource = File.ReadAllBytes(GlobalSettings.Default.StartupPath + path);
                        return Resource;
                    }
                }
                else
                    return Resource;
            }
        }

        /// <summary>
        /// Tries to store a resource in the internal cache.
        /// </summary>
        /// <param name="ID">The ID of the resource to store.</param>
        /// <param name="Resource">The resource to store.</param>
        private static void TryToStoreResource(ulong ID, byte[] Resource)
        {
            lock (m_LoadedResources)
            {
                if (m_CurrentCacheSize < m_CACHESIZE)
                {
                    byte[] Buffer;
                    if (!m_LoadedResources.TryGetValue(ID, out Buffer))
                    {
                        m_LoadedResources.Add(ID, Resource);
                        m_CurrentCacheSize += Resource.Length;
                    }
                }
                else
                {
                    ulong LastKey = m_LoadedResources.Keys.Last();

                    m_CurrentCacheSize -= m_LoadedResources[LastKey].Length;
                    m_LoadedResources.Remove(LastKey);

                    m_LoadedResources.Add(ID, Resource);
                    m_CurrentCacheSize += Resource.Length;
                }
            }
        }
    }
}