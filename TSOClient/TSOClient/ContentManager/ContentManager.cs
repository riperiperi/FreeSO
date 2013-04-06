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
using DNA;
using SimsLib.IFF;
using Microsoft.Xna.Framework.Graphics;
using SimsLib.FAR1;
using LogThis;
using TSOClient.Code;
using TSOClient.Code.Utils;
using TSOClient.Code.UI.Framework;

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
        //private static List<Floor> m_Floors;
        //private static List<Wall> m_Walls;
        //private static Wall m_DefaultWall;
        //private static Floor m_DefaultFloor;

        private static ManualResetEvent m_ResetEvent = new ManualResetEvent(false);
        public static event OnLoadingUpdatedDelegate OnLoadingUpdatedEvent;

        private static Dictionary<string, FAR3Archive> m_Archives = new Dictionary<string, FAR3Archive>();

        /// <summary>
        /// These are all the resources which have been precomputed and cached, this is to improve load time
        /// </summary>
        private static Dictionary<ulong, string> m_CachedResources = new Dictionary<ulong, string>();


        //public static Wall DefaultWall { get { return m_DefaultWall; } }
        //public static Floor DefaultFloor { get { return m_DefaultFloor; } }
        //public static List<Floor> Floors { get { return m_Floors; } }
        //public static List<Wall> Walls { get { return m_Walls; } }
        
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





            //m_CachedResources
            var cacheFiles = Directory.GetFiles(GameFacade.CacheDirectory);
            foreach (var file in cacheFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                m_CachedResources.Add(ulong.Parse(fileName), file);
            }





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


                if (m_CachedResources.ContainsKey(ID))
                {
                    result.FromCache = true;
                    result.FilePath = m_CachedResources[ID];
                    result.FileExtension = Path.GetExtension(result.FilePath).ToLower();

                    /** We have a cached version of this file :) **/
                    result.Data = File.ReadAllBytes(result.FilePath);
                    return result;
                }


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

                /*byte[] Resource;

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
                    return Resource;*/
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
                    byte[] Buffer;
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

        private static void LoadInitialTextures()
        {
            //LuaInterfaceManager.CallFunction("UpdateLoadingscreen");

            ////These textures are needed for the logindialog, so preload them.
            //TryToStoreResource((ulong)FileIDs.UIFileIDs.dialog_backgroundtemplate, GetResourceFromLongID((ulong)FileIDs.UIFileIDs.dialog_backgroundtemplate));
            //TryToStoreResource((ulong)FileIDs.UIFileIDs.buttontiledialog, GetResourceFromLongID((ulong)FileIDs.UIFileIDs.buttontiledialog));
            //TryToStoreResource((ulong)FileIDs.UIFileIDs.dialog_progressbarback, GetResourceFromLongID((ulong)FileIDs.UIFileIDs.dialog_progressbarback));

            //LuaInterfaceManager.CallFunction("UpdateLoadingscreen");

            ////Textures for the personselection screen.
            //TryToStoreResource((ulong)FileIDs.UIFileIDs.person_select_background, GetResourceFromLongID((ulong)FileIDs.UIFileIDs.person_select_background));
            //TryToStoreResource((ulong)FileIDs.UIFileIDs.person_select_exitbtn, GetResourceFromLongID((ulong)FileIDs.UIFileIDs.person_select_exitbtn));
            //TryToStoreResource((ulong)FileIDs.UIFileIDs.person_select_simcreatebtn, GetResourceFromLongID((ulong)FileIDs.UIFileIDs.person_select_simcreatebtn));
            //TryToStoreResource((ulong)FileIDs.UIFileIDs.person_select_cityhouseiconalpha, GetResourceFromLongID((ulong)FileIDs.UIFileIDs.person_select_cityhouseiconalpha));
            //TryToStoreResource((ulong)FileIDs.UIFileIDs.person_select_arrowdownbtn, GetResourceFromLongID((ulong)FileIDs.UIFileIDs.person_select_arrowdownbtn));
            //TryToStoreResource((ulong)FileIDs.UIFileIDs.person_select_arrowupbtn, GetResourceFromLongID((ulong)FileIDs.UIFileIDs.person_select_arrowupbtn));

            //LuaInterfaceManager.CallFunction("UpdateLoadingscreen");

            ////Textures for the CAS screen.
            //TryToStoreResource((ulong)FileIDs.UIFileIDs.person_edit_background, GetResourceFromLongID((ulong)FileIDs.UIFileIDs.person_edit_background));
            ////GetResourceFromLongID(0x3dd00000001); //person_edit_backtoselectbtn.bmp
            //TryToStoreResource((ulong)FileIDs.UIFileIDs.person_edit_cancelbtn, GetResourceFromLongID((ulong)FileIDs.UIFileIDs.person_edit_cancelbtn));
            //TryToStoreResource((ulong)FileIDs.UIFileIDs.person_edit_closebtn, GetResourceFromLongID((ulong)FileIDs.UIFileIDs.person_edit_closebtn));
            //TryToStoreResource((ulong)FileIDs.UIFileIDs.person_edit_femalebtn, GetResourceFromLongID((ulong)FileIDs.UIFileIDs.person_edit_femalebtn));
            //TryToStoreResource((ulong)FileIDs.UIFileIDs.person_edit_malebtn, GetResourceFromLongID((ulong)FileIDs.UIFileIDs.person_edit_malebtn));
            //TryToStoreResource((ulong)FileIDs.UIFileIDs.person_edit_skindarkbtn, GetResourceFromLongID((ulong)FileIDs.UIFileIDs.person_edit_skindarkbtn));
            //TryToStoreResource((ulong)FileIDs.UIFileIDs.person_edit_skinmediumbtn, GetResourceFromLongID((ulong)FileIDs.UIFileIDs.person_edit_skinmediumbtn));
            //TryToStoreResource((ulong)FileIDs.UIFileIDs.person_edit_skinbrowserarrowleft, GetResourceFromLongID((ulong)FileIDs.UIFileIDs.person_edit_skinbrowserarrowleft));
            //TryToStoreResource((ulong)FileIDs.UIFileIDs.person_edit_skinbrowserarrowright, GetResourceFromLongID((ulong)FileIDs.UIFileIDs.person_edit_skinbrowserarrowright));
            //TryToStoreResource((ulong)FileIDs.UIFileIDs.person_edit_skinlightbtn, GetResourceFromLongID((ulong)FileIDs.UIFileIDs.person_edit_skinlightbtn));

            //LuaInterfaceManager.CallFunction("UpdateLoadingscreen");

            //TryToStoreResource((ulong)FileIDs.UIFileIDs.cas_sas_creditsbtn, GetResourceFromLongID((ulong)FileIDs.UIFileIDs.cas_sas_creditsbtn));
            //TryToStoreResource((ulong)FileIDs.UIFileIDs.cas_sas_creditsindent, GetResourceFromLongID((ulong)FileIDs.UIFileIDs.cas_sas_creditsindent));

            ////Textures for the credits screen.
            //TryToStoreResource((ulong)FileIDs.UIFileIDs.creditscreen_backbtn, GetResourceFromLongID((ulong)FileIDs.UIFileIDs.creditscreen_backbtn));
            //TryToStoreResource((ulong)FileIDs.UIFileIDs.creditscreen_backbtnindent, GetResourceFromLongID((ulong)FileIDs.UIFileIDs.creditscreen_backbtnindent));
            //TryToStoreResource((ulong)FileIDs.UIFileIDs.creditscreen_background, GetResourceFromLongID((ulong)FileIDs.UIFileIDs.creditscreen_background));
            //TryToStoreResource((ulong)FileIDs.UIFileIDs.creditscreen_exitbtn, GetResourceFromLongID((ulong)FileIDs.UIFileIDs.creditscreen_exitbtn));
            //TryToStoreResource((ulong)FileIDs.UIFileIDs.creditscreen_maxisbtn, GetResourceFromLongID((ulong)FileIDs.UIFileIDs.creditscreen_maxisbtn));
            //TryToStoreResource((ulong)FileIDs.UIFileIDs.creditscreen_tsologo_english, GetResourceFromLongID((ulong)FileIDs.UIFileIDs.creditscreen_tsologo_english));

            //TryToStoreResource((ulong)FileIDs.UIFileIDs.cityselector_sortbtn, GetResourceFromLongID((ulong)FileIDs.UIFileIDs.cityselector_sortbtn));
            //TryToStoreResource((ulong)FileIDs.UIFileIDs.cityselector_thumbnailbackground, GetResourceFromLongID((ulong)FileIDs.UIFileIDs.cityselector_thumbnailbackground));

            //ulong[] ThumbnailIDs = (ulong[])Enum.GetValues(typeof(FileIDs.ThumbnailsFileIDs));

            ////Preload a bunch of thumbnails (used by CAS)
            ///*for(int i = 0; i < 200; i++)
            //    GetResourceFromLongID(ThumbnailIDs[i]);*/

            //ulong[] OutfitIDs = (ulong[])Enum.GetValues(typeof(FileIDs.OutfitsFileIDs));

            ////Preload a bunch of outfits (used by CAS)
            //foreach (ulong OutfitID in OutfitIDs)
            //    TryToStoreResource(OutfitID, GetResourceFromLongID(OutfitID));

            //ulong[] AppearanceIDs = (ulong[])Enum.GetValues(typeof(FileIDs.AppearancesFileIDs));

            ////Preload a bunch of appearances (used by CAS)
            //foreach (ulong AppearanceID in AppearanceIDs)
            //    TryToStoreResource(AppearanceID, GetResourceFromLongID(AppearanceID));

            //ulong[] PurchasableIDs = (ulong[])Enum.GetValues(typeof(FileIDs.PurchasablesFileIDs));

            ////Preload a bunch of appearances (used by CAS)
            //foreach (ulong PurchasableID in PurchasableIDs)
            //    TryToStoreResource(PurchasableID, GetResourceFromLongID(PurchasableID));

            //myLoadingScreenEWH.Set();

            //return;
        }

        private static EventWaitHandle myLoadingScreenEWH;

        /// <summary>
        /// Initializes loading of resources.
        /// </summary>
        /// <param name="ScreenMgr">A ScreenManager instance, used to access a GraphicsDevice.</param>
        public static void InitLoading()
        {
            myLoadingScreenEWH = new EventWaitHandle(false, EventResetMode.ManualReset, "Go_Away_Stupid_Loading_Screen_GO_U_HEARD_ME_DONT_MAKE_ME_GET_MY_STICK_OUT");
            //ThreadPool.QueueUserWorkItem(new WaitCallback(LoadContent));
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
            //loadingList.AddRange(
            //    CollectionUtils.Select<FileIDs.UIFileIDs, ContentPreload>(
            //        Enum.GetValues(typeof(FileIDs.UIFileIDs)),
            //        x => new ContentPreload{
            //            ID = (ulong)((long)x),
            //            Type = ContentPreloadType.UITexture
            //        }
            //    )
            //);

            ///** Sim textures for CAS **/
            //loadingList.AddRange(
            //    CollectionUtils.Select<FileIDs.UIFileIDs, ContentPreload>(
            //        Enum.GetValues(typeof(FileIDs.OutfitsFileIDs)),
            //        x => new ContentPreload
            //        {
            //            ID = (ulong)((long)x),
            //            Type = ContentPreloadType.Other
            //        }
            //    )
            //);
            //loadingList.AddRange(
            //    CollectionUtils.Select<FileIDs.UIFileIDs, ContentPreload>(
            //        Enum.GetValues(typeof(FileIDs.AppearancesFileIDs)),
            //        x => new ContentPreload
            //        {
            //            ID = (ulong)((long)x),
            //            Type = ContentPreloadType.Other
            //        }
            //    )
            //);
            //loadingList.AddRange(
            //    CollectionUtils.Select<FileIDs.UIFileIDs, ContentPreload>(
            //        Enum.GetValues(typeof(FileIDs.PurchasablesFileIDs)),
            //        x => new ContentPreload
            //        {
            //            ID = (ulong)((long)x),
            //            Type = ContentPreloadType.Other
            //        }
            //    )
            //);
            //loadingList.AddRange(
            //    CollectionUtils.Select<FileIDs.UIFileIDs, ContentPreload>(
            //        Enum.GetValues(typeof(FileIDs.ThumbnailsFileIDs)),
            //        x => new ContentPreload
            //        {
            //            ID = (ulong)((long)x),
            //            Type = ContentPreloadType.UITexture_NoMask
            //        }
            //    )
            //);


            
            var startTime = DateTime.Now;

            GameFacade.Cache = new BlobCache(Path.Combine(GameFacade.CacheRoot, "__pdcache.blob"));
            var allCached = GameFacade.Cache.ReadAll();
            GameFacade.Cache.StartWrite();

            var totalItems = (float)loadingList.Count;
            loadingList.Shuffle();
            //var processingPool = new ContentProcessingPool(1);

            var loadingListLength = loadingList.Count;
            for (var i = 0; i < loadingListLength; i++)
            {
                var item = loadingList[i];
                try
                {
                    ContentResource contentItem = null;
                    byte[] cachedItem = null;
                    if (allCached.TryGetValue(item.ID, out cachedItem))
                    {
                        contentItem = new ContentResource
                        {
                            FromCache = true,
                            ID = item.ID,
                            Data = cachedItem
                        };
                    }
                    else
                    {
                        contentItem = ContentManager.GetResourceInfo(item.ID);
                    }

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

                    //ProcessResource(item, contentItem);
                }
                catch (Exception ex)
                {
                }

                //loadingList.RemoveAt(0);
                PreloadProgress = i / totalItems;
            }

            GameFacade.Cache.Flush();

            var endTime = DateTime.Now;
            System.Diagnostics.Debug.WriteLine("Content took " + new TimeSpan(endTime.Ticks - startTime.Ticks).ToString() + " to load");

            PreloadProgress = 1.0f;


            //var archives = new List<string>();
            //var workItemHash = loadingList.ToDictionary(x => x.ID, x => x);

            ///**
            // * Work out all the archives we need, this allows us to load
            // * whole archives in one go to speed up the process
            // */
            //foreach (var item in loadingList)
            //{
            //    if (m_Resources.ContainsKey(item.ID))
            //    {
            //        var path = m_Resources[item.ID].Replace("./", "");
            //        if (path.EndsWith(".dat"))
            //        {
            //            if (!archives.Contains(path))
            //            {
            //                archives.Add(path);
            //            }
            //        }
            //    }
            //}


            //var totalItems = (float)loadingList.Count;
            //var numProcessingThreads = 2;
            //var processingPool = new ContentProcessingPool(numProcessingThreads);
            //var dataToProcess = new List<KeyValuePair<ContentPreload, byte[]>>();

            //foreach (var archivePath in archives)
            //{
            //    var archive = new FAR3Archive(archivePath);
            //    var allEntries = archive.GetAllEntries2();
            //    ContentPreload workItem;

            //    foreach (var entry in allEntries)
            //    {
            //        ulong ID = (ulong)(((ulong)entry.Key.FileID) << 32 | ((ulong)(entry.Key.TypeID >> 32)));

            //        workItemHash.TryGetValue(ID, out workItem);
            //        if (workItem != null)
            //        {
            //            //processingPool.Process(workItem, entry.Value);
            //            dataToProcess.Add(new KeyValuePair<ContentPreload, byte[]>(workItem, entry.Value));

            //            //ProcessResource(workItem, entry.Value);
            //            loadingList.Remove(workItem);

            //            PreloadProgress = (totalItems - loadingList.Count) / totalItems;
            //        }
            //    }
            //}

            ///** Load & process the rest **/
            //while (loadingList.Count > 0)
            //{
            //    var item = loadingList[0];
            //    try
            //    {
            //        var binaryData = ContentManager.GetResourceFromLongID(item.ID);
            //        dataToProcess.Add(new KeyValuePair<ContentPreload, byte[]>(item, binaryData));
            //        //ProcessResource(item, binaryData);
            //    }
            //    catch (Exception ex)
            //    {
            //    }

            //    loadingList.Remove(item);
            //    PreloadProgress = (totalItems - loadingList.Count) / totalItems;
            //}


            //var xsdsd = true;


            //var numThreads = 1;
            //var total = (float)loadingList.Count;
            //var workers = new List<ContentPreloadThread>();

            //for (var i = 0; i < numThreads; i++)
            //{
            //    var worker = new ContentPreloadThread(workers, loadingList);
            //    workers.Add(worker);
            //}

            //foreach (var worker in workers)
            //{
            //    var thread = new Thread(new ThreadStart(worker.Run));
            //    thread.Start();
            //}


            //while (workers.Count > 0)
            //{
            //    PreloadProgress = (total - loadingList.Count) / total;
            //    Thread.Sleep(500);
            //}

            //PreloadProgress = 1.0f;


            //InitWalls(Manager.GraphicsDevice);
            //InitFloors(Manager.GraphicsDevice);
            //LoadInitialTextures();
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
            //var processingSem = new Semaphore(numProcessingThreads, numProcessingThreads);
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