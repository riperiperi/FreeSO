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
using TSOClient.LUI;
using LogThis;

namespace TSOClient
{
    public delegate void OnLoadingUpdatedDelegate(string LoadingText);

    public class ContentManager
    {
        private const int m_CACHESIZE = 104857600; //100 megabytes.
        private static int m_CurrentCacheSize = 0;

        private static Dictionary<ulong, string> m_Resources;
        private static Dictionary<ulong, byte[]> m_LoadedResources;
        private static bool initComplete = false;
        private static List<Floor> m_Floors;
        private static List<Wall> m_Walls;
        private static Wall m_DefaultWall;
        private static Floor m_DefaultFloor;

        private static ManualResetEvent m_ResetEvent = new ManualResetEvent(false);
        public static event OnLoadingUpdatedDelegate OnLoadingUpdatedEvent;

        public static Wall DefaultWall { get { return m_DefaultWall; } }
        public static Floor DefaultFloor { get { return m_DefaultFloor; } }
        public static List<Floor> Floors { get { return m_Floors; } }
        public static List<Wall> Walls { get { return m_Walls; } }
        
        static ContentManager()
        {
            FAR3Archive packingSlips = new FAR3Archive(GlobalSettings.Default.StartupPath + "packingslips\\packingslips.dat");

            List<KeyValuePair<uint, byte[]>> entries = packingSlips.GetAllEntries();
            Dictionary<ulong, string> TmpResources = new Dictionary<ulong, string>();

            m_Resources = new Dictionary<ulong, string>();
            m_LoadedResources = new Dictionary<ulong, byte[]>();
            foreach (KeyValuePair<uint, byte[]> kvp in entries)
            {
                BinaryReader br = new BinaryReader(new MemoryStream(kvp.Value));
                br.BaseStream.Position = 50;
                string path = br.ReadString();
                br.BaseStream.Position += 8;
                ulong id = Endian.SwapUInt64(br.ReadUInt64());

                string[] VersionElements = GlobalSettings.Default.ClientVersion.Split(".".ToCharArray());

                //Hack to correct references to old files contained in packingslips.dat,
                //that exists from version 1.1097.1.0 onwards...
                if(int.Parse(VersionElements[0]) >= 1 && int.Parse(VersionElements[1]) >= 1097 && 
                    int.Parse(VersionElements[2]) >= 1 && int.Parse(VersionElements[3]) >= 0)
                {
                    if (path.Contains("person_select_cityhousebtn.bmp"))
                        path = path.Replace("person_select_cityhousebtn.bmp", "person_select_cityhouseiconalpha.tga");
                    else if (path.Contains("person_select_editbtn.bmp"))
                        path = path.Replace("person_select_editbtn.bmp", "person_select_simcreatebtn.bmp");
                    else if (path.Contains("person_edit_exitbtn.bmp"))
                        path = path.Replace("person_edit_exitbtn.bmp", "person_edit_closebtn.bmp");
                    else if (path.Contains("person_edit_skinblackbtn.bmp"))
                        path = path.Replace("person_edit_skinblackbtn.bmp", "person_edit_skindarkbtn.bmp");
                    else if (path.Contains("person_edit_skinbrownbtn.bmp"))
                        path = path.Replace("person_edit_skinbrownbtn.bmp", "person_edit_skinmediumbtn.bmp");
                    else if (path.Contains("person_edit_skinwhitebtn.bmp"))
                        path = path.Replace("person_edit_skinwhitebtn.bmp", "person_edit_skinlightbtn.bmp");
                }

                TmpResources.Add(id, path);

                br.Close();
            }

            XmlDataDocument AccessoryTable = new XmlDataDocument();
            AccessoryTable.Load(GlobalSettings.Default.StartupPath + "packingslips\\accessorytable.xml");

            XmlNodeList NodeList = AccessoryTable.GetElementsByTagName("DefineAssetString");

            foreach(XmlNode Node in NodeList)
            {
                //Add the ID from the xml, and the path from packetslips.dat...
                m_Resources.Add(Convert.ToUInt64(Node.Attributes["assetID"].Value, 16),
                    TmpResources[Convert.ToUInt64(Node.Attributes["assetID"].Value, 16)]);
            }

            XmlDataDocument AnimTable = new XmlDataDocument(); 
            AnimTable.Load(GlobalSettings.Default.StartupPath + "packingslips\\animtable.xml");

            NodeList = AnimTable.GetElementsByTagName("DefineAssetString");

            foreach (XmlNode Node in NodeList)
            {
                //Add the ID from the xml, and the path from packetslips.dat...
                m_Resources.Add(Convert.ToUInt64(Node.Attributes["assetID"].Value, 16),
                    TmpResources[Convert.ToUInt64(Node.Attributes["assetID"].Value, 16)]);
            }

            foreach(KeyValuePair<ulong, string> KVP in TmpResources)
            {
                if(KVP.Value.Contains("uigraphics"))
                    m_Resources.Add(KVP.Key, KVP.Value);
                if (KVP.Value.Contains(".apr"))
                {
                    if (!m_Resources.ContainsKey(KVP.Key))
                        m_Resources.Add(KVP.Key, KVP.Value);
                }
            }

            initComplete = true;
        }

        public static byte[] GetResourceFromLongID(ulong id)
        {
            while (!initComplete) ;
            if (m_Resources.ContainsKey(id))
            {
                //Resource hasn't already been loaded...
                if (!m_LoadedResources.ContainsKey(id))
                {
                    string path = m_Resources[id].Replace("./", "").Replace("/", "\\");
                    if (!File.Exists(GlobalSettings.Default.StartupPath + path))
                    {
                        string[] pathSections = path.Split(new char[] { '\\' });
                        int directoryIdx = 0;
                        if (path.Contains("\\heads\\") || path.Contains("\\hands\\") || path.Contains("\\bodies\\") || path.Contains("\\accessories\\"))
                            directoryIdx = Array.FindLastIndex<string>(pathSections, delegate(string it) { if (it.CompareTo("avatardata") == 0) { return true; } return false; }) + 2;
                        else
                            directoryIdx = Array.FindLastIndex<string>(pathSections, delegate(string it) { if (it.CompareTo("TSOClient") == 0) { return true; } return false; }) + 2;
                        string directoryName = pathSections[directoryIdx];
                        path = path.Remove(path.LastIndexOf('\\'));
                        string archivePath = GlobalSettings.Default.StartupPath + "\\" + path.Remove((path.LastIndexOf(pathSections[directoryIdx]))) + directoryName + '\\' + directoryName + ".dat";

                        FAR3Archive archive = new FAR3Archive(archivePath);
                        TryToStoreResource(id, archive[pathSections[pathSections.Length - 1]]);
                        return archive[pathSections[pathSections.Length - 1]];
                    }
                    else
                    {
                        byte[] Resource = File.ReadAllBytes(GlobalSettings.Default.StartupPath + path);

                        TryToStoreResource(id, Resource);
                        return Resource;
                    }
                }
                else
                    return m_LoadedResources[id];
            }
            
            return new byte[0];
        }

        public byte[] this[ulong id]
        {
            get
            {
                if (m_Resources.ContainsKey(id))
                {
                    //Resource hasn't already been loaded...
                    if (!m_LoadedResources.ContainsKey(id))
                    {
                        string path = m_Resources[id].Replace("./", "");
                        if (!File.Exists(path))
                        {
                            string[] pathSections = path.Split(new char[] { '/' });
                            string directoryName = pathSections[pathSections.Length - 2];
                            string archivePath = GlobalSettings.Default.StartupPath + path.Remove(path.LastIndexOf('/') + 1) + directoryName + ".dat";

                            FAR3Archive archive = new FAR3Archive(archivePath);
                            TryToStoreResource(id, archive[pathSections[pathSections.Length - 1]]);
                            return archive[pathSections[pathSections.Length - 1]];
                        }
                        else
                        {
                            byte[] Resource = File.ReadAllBytes(GlobalSettings.Default.StartupPath + path);

                            TryToStoreResource(id, Resource);
                            return Resource;
                        }
                    }
                    else
                        return m_LoadedResources[id];
                }
                return new byte[0];
            }
        }

        /// <summary>
        /// Tries to store a resource in the internal cache.
        /// </summary>
        /// <param name="ID">The ID of the resource to store.</param>
        /// <param name="Resource">The resource to store.</param>
        private static void TryToStoreResource(ulong ID, byte[] Resource)
        {
            if (m_CurrentCacheSize < m_CACHESIZE)
            {
                m_LoadedResources.Add(ID, Resource);
                m_CurrentCacheSize += Resource.Length;
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

        //From: http://social.msdn.microsoft.com/Forums/en/csharpgeneral/thread/cb9c7f4d-5f1e-4900-87d8-013205f27587
        private static ulong Get64BitRandom(ulong minValue, ulong maxValue)
        {
            Random Rnd = new Random(DateTime.Now.Millisecond);

            // Get a random array of 8 bytes.
            byte[] buffer = new byte[sizeof(ulong)];
            Rnd.NextBytes(buffer);

            return BitConverter.ToUInt64(buffer, 0) % (maxValue - minValue + 1) + minValue;
        }

        private static void LoadInitialTextures()
        {
            LuaInterfaceManager.CallFunction("UpdateLoadingscreen");

            //These textures are needed for the logindialog, so preload them.
            GetResourceFromLongID(0xe500000002);  //Dialog.
            GetResourceFromLongID(0x1e700000001); //Button.
            GetResourceFromLongID(0x7a500000001); //Progressbar.

            LuaInterfaceManager.CallFunction("UpdateLoadingscreen");

            //Textures for the personselection screen.
            GetResourceFromLongID(0x3fa00000001); //person_select_background.bmp
            GetResourceFromLongID(0x3ff00000001); //person_select_exitbtn.bmp
            GetResourceFromLongID(0x3fe00000001); //person_select_simcreatebtn.bmp
            GetResourceFromLongID(0x3fc00000001); //person_select_cityhouseiconalpha.tga
            GetResourceFromLongID(0x3f800000001); //person_select_arrowdownbtn.bmp
            GetResourceFromLongID(0x3f900000001); //person_select_arrowupbtn.bmp

            LuaInterfaceManager.CallFunction("UpdateLoadingscreen");

            //Textures for the CAS screen.
            GetResourceFromLongID(0x3dc00000001); //person_edit_background.bmp
            //GetResourceFromLongID(0x3dd00000001); //person_edit_backtoselectbtn.bmp
            GetResourceFromLongID(0x3e000000001); //person_edit_cancelbtn.bmp
            GetResourceFromLongID(0x3e300000001); //person_edit_closebtn.bmp
            GetResourceFromLongID(0x3e400000001); //person_edit_femalebtn.bmp
            GetResourceFromLongID(0x3eb00000001); //person_edit_malebtn.bmp
            GetResourceFromLongID(0x3f300000001); //person_edit_skindarkbtn.bmp
            GetResourceFromLongID(0x3f400000001); //person_edit_skinmediumbtn.bmp
            GetResourceFromLongID(0x3f500000001); //person_edit_skinbrowserarrowleft.bmp
            GetResourceFromLongID(0x3f600000001); //person_edit_skinbrowserarrowright.bmp
            GetResourceFromLongID(0x3f700000001); //person_edit_skinlightbtn.bmp

            LuaInterfaceManager.CallFunction("UpdateLoadingscreen");

            GetResourceFromLongID(0x89500000001); //cas-sas-creditsbtn.bmp
            GetResourceFromLongID(0x89600000001); //cas-sas-creditsindent.bmp

            //Textures for the credits screen.
            GetResourceFromLongID(0x8aa00000001); //creditscreen_backbtn.bmp
            GetResourceFromLongID(0x8ab00000001); //creditscreen_backbtnindent.bmp
            GetResourceFromLongID(0x8ac00000001); //creditscreen_background.bmp
            GetResourceFromLongID(0x8ad00000001); //creditscreen_exitbtn.bmp
            GetResourceFromLongID(0x8ae00000001); //creditscreen_maxisbtn.bmp
            GetResourceFromLongID(0x8af00000001); //creditscreen_tsologo_english.bmp

            GetResourceFromLongID(0x8a800000001); //cityselector_sortbtn.bmp
            GetResourceFromLongID(0x8a900000001); //cityselector_thumbnailbackground.bmp

            myLoadingScreenEWH.Set();
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
            T.Start();
        }

        /// <summary>
        /// Threading function that takes care of loading.
        /// </summary>
        private static void LoadContent(object ThreadObject)
        {
            //InitWalls(Manager.GraphicsDevice);
            //InitFloors(Manager.GraphicsDevice);
            LoadInitialTextures();
        }
    }
}