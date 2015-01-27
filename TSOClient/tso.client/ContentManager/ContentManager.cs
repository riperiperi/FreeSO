/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

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
using DNA;
using Microsoft.Xna.Framework.Graphics;
using LogThis;
using TSOClient.Code.Utils;
using TSOClient.Code.UI.Framework;
using TSO.Files.FAR3;
using TSOClient.Code;

namespace TSOClient
{
    public delegate void OnLoadingUpdatedDelegate(string LoadingText);

    public class ContentManager
    {
        private const int m_CACHESIZE = 104857600; //100 megabytes.
        private static int m_CurrentCacheSize = 0;

        private static Dictionary<ulong, string> m_Resources;
        private static Dictionary<ulong, ContentResource> m_LoadedResources;
        private static bool initComplete = false;

        private static ManualResetEvent m_ResetEvent = new ManualResetEvent(false);
        //public static event OnLoadingUpdatedDelegate OnLoadingUpdatedEvent;

        private static Dictionary<string, FAR3Archive> m_Archives = new Dictionary<string, FAR3Archive>();

        /// <summary>
        /// These are all the resources which have been precomputed and cached, this is to improve load time
        /// </summary>
        private static Dictionary<ulong, string> m_CachedResources = new Dictionary<ulong, string>();
        
        static ContentManager()
        {
            m_Resources = new Dictionary<ulong, string>();
            m_LoadedResources = new Dictionary<ulong, ContentResource>();

            XmlDataDocument AnimTable = new XmlDataDocument();
            AnimTable.Load(GlobalSettings.Default.StartupPath + "packingslips\\animtable.xml");

            XmlNodeList NodeList = AnimTable.GetElementsByTagName("DefineAssetString");

            foreach (XmlNode Node in NodeList)
            {
                ulong FileID = Convert.ToUInt64(Node.Attributes["assetID"].Value, 16);
                //TODO: Figure out when to use avatardata2 and avatardata3...
                string FileName = GlobalSettings.Default.StartupPath + "avatardata\\animations\\animations.dat";

                m_Resources.Add(FileID, FileName);
            }

            XmlDataDocument UIGraphicsTable = new XmlDataDocument();
            UIGraphicsTable.Load(GlobalSettings.Default.StartupPath + "packingslips\\uigraphics.xml");

            NodeList = UIGraphicsTable.GetElementsByTagName("DefineAssetString");

            foreach (XmlNode Node in NodeList)
            {
                ulong FileID = Convert.ToUInt64(Node.Attributes["assetID"].Value, 16);

                string FileName = "";

                if (Node.Attributes["key"].Value.Contains(".dat"))
                {
                    FileName = GlobalSettings.Default.StartupPath + Node.Attributes["key"].Value;
                }
                else
                    FileName = GlobalSettings.Default.StartupPath + Node.Attributes["key"].Value;

                m_Resources.Add(FileID, FileName);
            }

            XmlDataDocument CollectionsTable = new XmlDataDocument();
            CollectionsTable.Load(GlobalSettings.Default.StartupPath + "packingslips\\collections.xml");

            NodeList = CollectionsTable.GetElementsByTagName("DefineAssetString");

            foreach (XmlNode Node in NodeList)
            {
                ulong FileID = Convert.ToUInt64(Node.Attributes["assetID"].Value, 16);
                string FileName = "";

                if (Node.Attributes["key"].Value.Contains(".dat"))
                {
                    FileName = GlobalSettings.Default.StartupPath + Node.Attributes["key"].Value;
                }

                m_Resources.Add(FileID, FileName);
            }

            XmlDataDocument PurchasablesTable = new XmlDataDocument();
            PurchasablesTable.Load(GlobalSettings.Default.StartupPath + "packingslips\\purchasables.xml");

            NodeList = PurchasablesTable.GetElementsByTagName("DefineAssetString");

            foreach (XmlNode Node in NodeList)
            {
                ulong FileID = Convert.ToUInt64(Node.Attributes["assetID"].Value, 16);
                string FileName = "";

                if (Node.Attributes["key"].Value.Contains(".dat"))
                {
                    FileName = GlobalSettings.Default.StartupPath + Node.Attributes["key"].Value;
                }

                m_Resources.Add(FileID, FileName);
            }

            XmlDataDocument OutfitsTable = new XmlDataDocument();
            OutfitsTable.Load(GlobalSettings.Default.StartupPath + "packingslips\\alloutfits.xml");

            NodeList = OutfitsTable.GetElementsByTagName("DefineAssetString");

            foreach (XmlNode Node in NodeList)
            {
                ulong FileID = Convert.ToUInt64(Node.Attributes["assetID"].Value, 16);
                string FileName = "";

                if (Node.Attributes["key"].Value.Contains(".dat"))
                {
                    FileName = GlobalSettings.Default.StartupPath + Node.Attributes["key"].Value;
                }

                m_Resources.Add(FileID, FileName);
            }

            XmlDataDocument AppearancesTable = new XmlDataDocument();
            AppearancesTable.Load(GlobalSettings.Default.StartupPath + "packingslips\\appearances.xml");

            NodeList = AppearancesTable.GetElementsByTagName("DefineAssetString");

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

                m_Resources.Add(FileID, FileName);
            }

            XmlDataDocument MeshTable = new XmlDataDocument();
            MeshTable.Load(GlobalSettings.Default.StartupPath + "packingslips\\meshes.xml");

            NodeList = MeshTable.GetElementsByTagName("DefineAssetString");

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

            XmlDataDocument TextureTable = new XmlDataDocument();
            TextureTable.Load(GlobalSettings.Default.StartupPath + "packingslips\\textures.xml");

            NodeList = TextureTable.GetElementsByTagName("DefineAssetString");

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

            XmlDataDocument BindingsTable = new XmlDataDocument();
            BindingsTable.Load(GlobalSettings.Default.StartupPath + "packingslips\\bindings.xml");

            NodeList = BindingsTable.GetElementsByTagName("DefineAssetString");

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

            var cacheFiles = Directory.GetFiles(GameFacade.CacheDirectory);
            foreach (var file in cacheFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                m_CachedResources.Add(ulong.Parse(fileName), file);
            }

            m_Resources.Add(0x100000005, GlobalSettings.Default.StartupPath + "avatardata\\skeletons\\skeletons.dat");

            initComplete = true;
            GameFacade.TriggerContentLoaderReady();
        }

        public static byte[] GetResourceFromLongID(ulong ID)
        {
            var rsrc = GetResourceInfo(ID);
            if (rsrc != null) { return rsrc.Data; }
            return null;
        }

        public static ContentResource GetResourceInfo(ulong ID)
        {
            /** Busy wait until we are ready **/
            while (!initComplete) ;

            ContentResource result = null;

            if (!m_LoadedResources.TryGetValue(ID, out result))
            {
                result = new ContentResource
                {
                    ID = ID
                };

                string path = m_Resources[ID];
                result.FilePath = path;
                result.FileExtension = Path.GetExtension(path).ToLower();

                if (!path.EndsWith(".dat"))
                {
                    /** Isnt an archive **/
                    result.Data = File.ReadAllBytes(path);
                    return result;
                }

                if (!m_Archives.ContainsKey(path))
                {
                    FAR3Archive Archive = new FAR3Archive(path);
                    m_Archives.Add(path, Archive);
                }

                result.Data = m_Archives[path].GetItemByID(ID);
                
                return result;
            }
            else
            {
                return result;
            }
        }

        public byte[] this[ulong FileID]
        {
            get
            {
                var result = GetResourceInfo(FileID);
                if (result == null) { return null; }
                return result.Data;
            }
        }

        /// <summary>
        /// Tries to store a resource in the internal cache.
        /// </summary>
        /// <param name="ID">The ID of the resource to store.</param>
        /// <param name="Resource">The resource to store.</param>
        public static void TryToStoreResource(ulong ID, ContentResource Resource)
        {
            lock (m_LoadedResources)
            {
                if (m_CurrentCacheSize < m_CACHESIZE)
                {
                    if (!m_LoadedResources.ContainsKey(ID))
                    {
                        m_LoadedResources.Add(ID, Resource);
                        m_CurrentCacheSize += Resource.Data.Length;
                    }
                }
                else
                {
                    ulong LastKey = m_LoadedResources.Keys.Last();

                    m_CurrentCacheSize -= m_LoadedResources[LastKey].Data.Length;
                    m_LoadedResources.Remove(LastKey);

                    m_LoadedResources.Add(ID, Resource);
                    m_CurrentCacheSize += Resource.Data.Length;
                }
            }
        }

        private static EventWaitHandle myLoadingScreenEWH;

        /// <summary>
        /// Initializes loading of resources.
        /// </summary>
        /// <param name="ScreenMgr">A ScreenManager instance, used to access a GraphicsDevice.</param>
        public static void InitLoading()
        {
            myLoadingScreenEWH = new EventWaitHandle(false, EventResetMode.ManualReset, "Go_Away_Stupid_Loading_Screen_GO_U_HEARD_ME_DONT_MAKE_ME_GET_MY_STICK_OUT");
            Thread T = new Thread(new ParameterizedThreadStart(LoadContent));
            //TODO: This should only be set to speed up debug
            T.Priority = ThreadPriority.AboveNormal;
            T.Start();
        }


        public static float PreloadProgress = 0.0f;

        /// <summary>
        /// Threading function that takes care of loading.
        /// </summary>
        private static void LoadContent(object ThreadObject)
        {
            var loadingList = new List<ContentPreload>();

            /** UI Textures **/
            loadingList.AddRange(
                CollectionUtils.Select<FileIDs.UIFileIDs, ContentPreload>(
                    Enum.GetValues(typeof(FileIDs.UIFileIDs)),
                    x => new ContentPreload
                    {
                        ID = (ulong)((long)x),
                        Type = ContentPreloadType.Other
                    }
                )
            );

            ///** Sim textures for CAS **/
            loadingList.AddRange(
                CollectionUtils.Select<FileIDs.UIFileIDs, ContentPreload>(
                    Enum.GetValues(typeof(FileIDs.OutfitsFileIDs)),
                    x => new ContentPreload
                    {
                        ID = (ulong)((long)x),
                        Type = ContentPreloadType.Other
                    }
                )
            );
            loadingList.AddRange(
                CollectionUtils.Select<FileIDs.UIFileIDs, ContentPreload>(
                    Enum.GetValues(typeof(FileIDs.AppearancesFileIDs)),
                    x => new ContentPreload
                    {
                        ID = (ulong)((long)x),
                        Type = ContentPreloadType.Other
                    }
                )
            );
            loadingList.AddRange(
                CollectionUtils.Select<FileIDs.UIFileIDs, ContentPreload>(
                    Enum.GetValues(typeof(FileIDs.PurchasablesFileIDs)),
                    x => new ContentPreload
                    {
                        ID = (ulong)((long)x),
                        Type = ContentPreloadType.Other
                    }
                )
            );
            loadingList.AddRange(
                CollectionUtils.Select<FileIDs.UIFileIDs, ContentPreload>(
                    Enum.GetValues(typeof(FileIDs.ThumbnailsFileIDs)),
                    x => new ContentPreload
                    {
                        ID = (ulong)((long)x),
                        Type = ContentPreloadType.Other
                    }
                )
            );
            
            var startTime = DateTime.Now;

            var totalItems = (float)loadingList.Count;
            loadingList.Shuffle();

            var loadingListLength = loadingList.Count;
            for (var i = 0; i < loadingListLength; i++)
            {
                var item = loadingList[i];
                try
                {
                    ContentResource contentItem = null;
                    contentItem = ContentManager.GetResourceInfo(item.ID);

                    switch (item.Type)
                    {
                        case ContentPreloadType.UITexture:
                            /** Apply alpha channel masking & load into GD **/
                            UIElement.StoreTexture(item.ID, contentItem, true, true);
                            break;

                        case ContentPreloadType.UITexture_NoMask:
                            UIElement.StoreTexture(item.ID, contentItem, false, true);
                            break;

                        case ContentPreloadType.Other:
                            ContentManager.TryToStoreResource(item.ID, contentItem);
                            break;
                    }
                }
                catch (Exception)
                {
                }

                PreloadProgress = i / totalItems;
            }

            var endTime = DateTime.Now;
            System.Diagnostics.Debug.WriteLine("Content took " + new TimeSpan(endTime.Ticks - startTime.Ticks).ToString() + " to load");

            PreloadProgress = 1.0f;


        }

        private static void ProcessResource(ContentPreload resource, ContentResource item)
        {
            var id = resource.ID;

            try
            {
                switch (resource.Type)
                {
                    case ContentPreloadType.UITexture:
                        /** Apply alpha channel masking & load into GD **/
                        UIElement.StoreTexture(id, item, true, true);
                        break;

                    case ContentPreloadType.UITexture_NoMask:
                        UIElement.StoreTexture(id, item, false, true);
                        break;

                    case ContentPreloadType.Other:
                        ContentManager.TryToStoreResource(id, item);
                        break;
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Failed to load file: " + id + ", " + e.Message);
            }
        }

        public static Dictionary<ulong, string> GetResources()
        {
            return m_Resources;
        }
    }

    /// <summary>
    /// Creates a binary file which contains several other non compressed files,
    /// The file format is:
    /// [fileID],[fileLength],[fileData]
    /// </summary>
    public class BlobCache
    {
        private BinaryWriter Writer;
        private string FilePath;

        public BlobCache(string filePath)
        {
            this.FilePath = filePath;
        }

        public Dictionary<ulong, byte[]> ReadAll()
        {
            var result = new Dictionary<ulong, byte[]>();
            if (!File.Exists(FilePath))
            {
                return result;
            }

            using (var reader = new BinaryReader(File.OpenRead(FilePath)))
            {

                var len = reader.BaseStream.Length;
                while (len > 8)
                {
                    var fileID = reader.ReadUInt64();
                    var fileLength = reader.ReadInt32();
                    var fileData = reader.ReadBytes(fileLength);

                    result.Add(fileID, fileData);

                    len -= 16;
                    len -= fileLength;
                }

                return result;
            }
        }

        public void StartWrite()
        {
            if (File.Exists(FilePath))
            {

                Writer = new BinaryWriter(File.OpenWrite(FilePath));
            }
            else
            {

                Writer = new BinaryWriter(File.Create(FilePath));
            }
        }

        public void AddFile(ulong id, byte[] data)
        {
            Writer.Write(id);
            Writer.Write(data.Length);
            Writer.Write(data);
        }

        public void Flush()
        {
            Writer.Flush();
            Writer.Close();
        }
    }

    public class ContentProcessingPool
    {
        private Semaphore m_Lock;
        private bool m_Done;
        private List<ContentPreload> m_Work = new List<ContentPreload>();

        public ContentProcessingPool(int maxThreads)
        {
            m_Lock = new Semaphore(maxThreads, maxThreads);

            /** Spawn threads **/
            m_Done = false;

            for (var i = 0; i < maxThreads; i++)
            {
                var thread = new Thread(new ThreadStart(DoProcess));
                thread.Start();
            }
        }

        private void DoProcess()
        {
            while (!m_Done)
            {
                ContentPreload nextWork = null;
                lock (m_Work)
                {
                    if (m_Work.Count > 0)
                    {
                        nextWork = m_Work[0];
                        m_Work.RemoveAt(0);
                    }
                }
                if (nextWork != null)
                {
                    ProcessResource(nextWork);
                    m_Lock.Release();
                    
                }
            }
        }

        public void Process(ContentPreload resource, ContentResource item)
        {
            m_Lock.WaitOne();
            
            lock (m_Work)
            {
                resource.Item = item;
                m_Work.Add(resource);
            }
        }

        private void ProcessResource(ContentPreload resource)
        {
            try
            {
                var id = resource.ID;
                var item = resource.Item;

                switch (resource.Type)
                {
                    case ContentPreloadType.UITexture:
                        /** Apply alpha channel masking & load into GD **/
                        UIElement.StoreTexture(id, item, true, true);
                        break;

                    case ContentPreloadType.UITexture_NoMask:
                        UIElement.StoreTexture(id, item, false, false);
                        break;

                    case ContentPreloadType.Other:
                        ContentManager.TryToStoreResource(id, item);
                        break;
                }
            }
            catch
            {
            }
        }

    }

    public class ContentResource
    {
        public ulong ID;
        public byte[] Data;
        public string FileExtension;
        public string FilePath;
        public bool FromCache;
    }

    public class ContentPreload
    {
        public ContentPreloadType Type;
        public ulong ID;
        public ContentResource Item;
    }

    public enum ContentPreloadType
    {
        UITexture,
        UITexture_NoMask,
        Other
    }
}