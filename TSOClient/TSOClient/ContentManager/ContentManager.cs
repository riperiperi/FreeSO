using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Threading;
using System.IO;
using SimsLib.FAR3;
using DNA;
using SimsLib.IFF;
using Microsoft.Xna.Framework.Graphics;
using SimsLib.FAR1;
using TSOClient.LUI;

namespace TSOClient
{
    public delegate void OnLoadingUpdatedDelegate(string LoadingText);

    public class ContentManager
    {
        private static Dictionary<ulong, string> m_Resources;
        private static bool initComplete = false;
        private static Random m_Rand;
        private static Dictionary<string, FAR3Archive> m_Archives;
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
                        return m_Archives[archivePath].GetItemByID((uint)(id>>32));
                    }
                }
                else
                {
                    return File.ReadAllBytes(GlobalSettings.Default.StartupPath + path);
                }
            }
            return new byte[0];
        }

        public byte[] this[ulong id]
        {
            get
            {
                if (m_Resources.ContainsKey(id))
                {
                    string path = m_Resources[id].Replace("./", "");
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
                        return File.ReadAllBytes(GlobalSettings.Default.StartupPath + path);
                    }
                }
                return new byte[0];
            }
        }

        private static void InitWalls(GraphicsDevice gd)
        {
            m_Walls = new List<Wall>();

            string walls1dat = GlobalSettings.Default.StartupPath + "housedata\\walls\\walls.far";
            string walls2dat = GlobalSettings.Default.StartupPath + "housedata\\walls2\\walls2.far";
            string walls3dat = GlobalSettings.Default.StartupPath + "housedata\\walls3\\walls3.far";
            string walls4dat = GlobalSettings.Default.StartupPath + "housedata\\walls4\\walls4.far";
            string walls5iff = GlobalSettings.Default.StartupPath + "objectdata\\globals\\walls.iff";
            string walls5Namesiff = GlobalSettings.Default.StartupPath + "objectdata\\globals\\build.iff";

            FARArchive walls1 = new FARArchive(walls1dat);
            FARArchive walls2 = new FARArchive(walls2dat);
            FARArchive walls3 = new FARArchive(walls3dat);
            FARArchive walls4 = new FARArchive(walls4dat);
            Iff walls5 = new Iff(File.OpenRead(walls5iff));
            Iff walls5names = new Iff(File.OpenRead(walls5Namesiff));

            // Load the first wall data
            List<KeyValuePair<string, byte[]>> walls = walls1.GetAllEntries();
            foreach (KeyValuePair<string, byte[]> kvp in walls)
            {
                Iff flr;

                if (kvp.Key.Contains("iff") || kvp.Key.Contains("spf"))
                    flr = new Iff(new MemoryStream(kvp.Value));
                else
                    flr = new Iff(new MemoryStream(kvp.Value));

                List<StringTableString> catalogInfo = (flr.StringTables[0].StringSets.Count > 0) ? flr.StringTables[0].StringSets[0].Strings : flr.StringTables[0].Strings;
                SPR2Parser spr1 = flr.SPR2s.Find(delegate(SPR2Parser sp) { return sp.ID == 1; });
                SPR2Parser spr2 = flr.SPR2s.Find(delegate(SPR2Parser sp) { return sp.ID == 1793; });
                SPR2Parser spr3 = flr.SPR2s.Find(delegate(SPR2Parser sp) { return sp.ID == 2049; });

                lock (m_Walls)
                {
                    m_Walls.Add(new Wall(catalogInfo[0].Str, catalogInfo[1].Str, catalogInfo[2].Str,
                        new Bitmap[,] {{spr1.GetFrame(0).BitmapData.BitMap, spr1.GetFrame(1).BitmapData.BitMap, 
                    spr1.GetFrame(2).BitmapData.BitMap, spr1.GetFrame(3).BitmapData.BitMap}, {spr2.GetFrame(0).BitmapData.BitMap, 
                    spr2.GetFrame(1).BitmapData.BitMap, spr2.GetFrame(2).BitmapData.BitMap, spr2.GetFrame(3).BitmapData.BitMap},
                    {spr3.GetFrame(0).BitmapData.BitMap, spr3.GetFrame(1).BitmapData.BitMap, spr3.GetFrame(2).BitmapData.BitMap, 
                    spr3.GetFrame(3).BitmapData.BitMap} }, gd,
                        flr.SPR2s.Find(delegate(SPR2Parser sp) { return sp.ID == 1; }).Name));
                }
            }

            LuaInterfaceManager.CallFunction("UpdateLoadingscreen");

            // Load the second wall data
            walls = walls2.GetAllEntries();
            foreach (KeyValuePair<string, byte[]> kvp in walls)
            {
                Iff flr;

                if (kvp.Key.Contains("iff") || kvp.Key.Contains("spf"))
                    flr = new Iff(new MemoryStream(kvp.Value));
                else
                    flr = new Iff(new MemoryStream(kvp.Value));

                List<StringTableString> catalogInfo = (flr.StringTables[0].StringSets.Count > 0) ? flr.StringTables[0].StringSets[0].Strings : flr.StringTables[0].Strings;
                SPR2Parser spr1 = flr.SPR2s.Find(delegate(SPR2Parser sp) { return sp.ID == 1; });
                SPR2Parser spr2 = flr.SPR2s.Find(delegate(SPR2Parser sp) { return sp.ID == 1793; });
                SPR2Parser spr3 = flr.SPR2s.Find(delegate(SPR2Parser sp) { return sp.ID == 2049; });

                lock (m_Walls)
                {
                    m_Walls.Add(new Wall(catalogInfo[0].Str, catalogInfo[1].Str, catalogInfo[2].Str,
                        new Bitmap[,] {{spr1.GetFrame(0).BitmapData.BitMap, spr1.GetFrame(1).BitmapData.BitMap, 
                    spr1.GetFrame(2).BitmapData.BitMap, spr1.GetFrame(3).BitmapData.BitMap}, {spr2.GetFrame(0).BitmapData.BitMap, 
                    spr2.GetFrame(1).BitmapData.BitMap, spr2.GetFrame(2).BitmapData.BitMap, spr2.GetFrame(3).BitmapData.BitMap}, 
                    {spr3.GetFrame(0).BitmapData.BitMap, spr3.GetFrame(1).BitmapData.BitMap, spr3.GetFrame(2).BitmapData.BitMap, 
                    spr3.GetFrame(3).BitmapData.BitMap} }, gd,
                        flr.SPR2s.Find(delegate(SPR2Parser sp) { return sp.ID == 1; }).Name));
                }
            }

            LuaInterfaceManager.CallFunction("UpdateLoadingscreen");

            // Load the third wall data
            walls = walls3.GetAllEntries();
            foreach (KeyValuePair<string, byte[]> kvp in walls)
            {
                Iff flr;

                if (kvp.Key.Contains("iff") || kvp.Key.Contains("spf"))
                    flr = new Iff(new MemoryStream(kvp.Value));
                else
                    flr = new Iff(new MemoryStream(kvp.Value));

                List<StringTableString> catalogInfo = (flr.StringTables[0].StringSets.Count > 0) ? flr.StringTables[0].StringSets[0].Strings : flr.StringTables[0].Strings;
                SPR2Parser spr1 = flr.SPR2s.Find(delegate(SPR2Parser sp) { return sp.ID == 1; });
                SPR2Parser spr2 = flr.SPR2s.Find(delegate(SPR2Parser sp) { return sp.ID == 1793; });
                SPR2Parser spr3 = flr.SPR2s.Find(delegate(SPR2Parser sp) { return sp.ID == 2049; });

                lock (m_Walls)
                {
                    m_Walls.Add(new Wall(catalogInfo[0].Str, catalogInfo[1].Str, catalogInfo[2].Str,
                        new Bitmap[,] {{spr1.GetFrame(0).BitmapData.BitMap, spr1.GetFrame(1).BitmapData.BitMap, 
                    spr1.GetFrame(2).BitmapData.BitMap, spr1.GetFrame(3).BitmapData.BitMap}, {spr2.GetFrame(0).BitmapData.BitMap, 
                    spr2.GetFrame(1).BitmapData.BitMap, spr2.GetFrame(2).BitmapData.BitMap, spr2.GetFrame(3).BitmapData.BitMap}, 
                    {spr3.GetFrame(0).BitmapData.BitMap, spr3.GetFrame(1).BitmapData.BitMap, spr3.GetFrame(2).BitmapData.BitMap, 
                    spr3.GetFrame(3).BitmapData.BitMap} }, gd,
                        flr.SPR2s.Find(delegate(SPR2Parser sp) { return sp.ID == 1; }).Name));
                }
            }

            LuaInterfaceManager.CallFunction("UpdateLoadingscreen");

            // Load the fourth wall data
            walls = walls4.GetAllEntries();
            foreach (KeyValuePair<string, byte[]> kvp in walls)
            {
                Iff flr;

                if (kvp.Key.Contains("iff") || kvp.Key.Contains("spf"))
                    flr = new Iff(new MemoryStream(kvp.Value));
                else
                    flr = new Iff(new MemoryStream(kvp.Value));

                List<StringTableString> catalogInfo = (flr.StringTables[0].StringSets.Count > 0) ? flr.StringTables[0].StringSets[0].Strings : flr.StringTables[0].Strings;
                SPR2Parser spr1 = flr.SPR2s.Find(delegate(SPR2Parser sp) { return sp.ID == 1; });
                SPR2Parser spr2 = flr.SPR2s.Find(delegate(SPR2Parser sp) { return sp.ID == 1793; });
                SPR2Parser spr3 = flr.SPR2s.Find(delegate(SPR2Parser sp) { return sp.ID == 2049; });

                lock (m_Walls)
                {
                    m_Walls.Add(new Wall(catalogInfo[0].Str, catalogInfo[1].Str, catalogInfo[2].Str,
                        new Bitmap[,] {{spr1.GetFrame(0).BitmapData.BitMap, spr1.GetFrame(1).BitmapData.BitMap, 
                    spr1.GetFrame(2).BitmapData.BitMap, spr1.GetFrame(3).BitmapData.BitMap}, {spr2.GetFrame(0).BitmapData.BitMap, 
                    spr2.GetFrame(1).BitmapData.BitMap, spr2.GetFrame(2).BitmapData.BitMap, spr2.GetFrame(3).BitmapData.BitMap}, 
                    {spr3.GetFrame(0).BitmapData.BitMap, spr3.GetFrame(1).BitmapData.BitMap, spr3.GetFrame(2).BitmapData.BitMap, 
                    spr3.GetFrame(3).BitmapData.BitMap} }, gd,
                        flr.SPR2s.Find(delegate(SPR2Parser sp) { return sp.ID == 1; }).Name));
                }
            }

            LuaInterfaceManager.CallFunction("UpdateLoadingscreen");

            int numActualWalls = 0;
            for (int i = 2; i < 31; i++)
            {
                Bitmap[,] frames = new Bitmap[3, 4];

                string spriteName = walls5.SPR2s[i].Name;
                SPR2Parser spr1 = walls5.SPR2s.Find(delegate(SPR2Parser sp) { return sp.ID == i; });
                SPR2Parser spr2 = walls5.SPR2s.Find(delegate(SPR2Parser sp) { return sp.ID == i + 1792; });
                SPR2Parser spr3 = walls5.SPR2s.Find(delegate(SPR2Parser sp) { return sp.ID == i + 2048; });

                if (spr1 != null && spr2 != null && spr3 != null && !(i >= 3 && i <= 9) && !(i >= 18 && i <= 532) &&
                    !(i >= 548 && i <= 1047) && !(i >= 1543 && i <= 1554) && !(i >= 1557 && i <= 2049))
                {
                    numActualWalls++;
                    frames = new Bitmap[,] {
                {spr1.GetFrame(0).BitmapData.BitMap, spr1.GetFrame(1).BitmapData.BitMap, spr1.GetFrame(2).BitmapData.BitMap, spr1.GetFrame(3).BitmapData.BitMap},
                {spr2.GetFrame(0).BitmapData.BitMap, spr2.GetFrame(1).BitmapData.BitMap, spr2.GetFrame(2).BitmapData.BitMap, spr2.GetFrame(3).BitmapData.BitMap},
                {spr3.GetFrame(0).BitmapData.BitMap, spr3.GetFrame(1).BitmapData.BitMap, spr3.GetFrame(2).BitmapData.BitMap, spr3.GetFrame(3).BitmapData.BitMap} };

                    string price = walls5names.StringTables[6].StringSets[0].Strings[(numActualWalls - 1) * 3].Str;
                    string title = walls5names.StringTables[6].StringSets[0].Strings[(numActualWalls - 1) * 3 + 1].Str;
                    string description = walls5names.StringTables[6].StringSets[0].Strings[(numActualWalls - 1) * 3 + 2].Str;

                    m_Walls.Add(new Wall(title, price, description, frames, gd, spriteName));
                }
            }

            LuaInterfaceManager.CallFunction("UpdateLoadingscreen");

            //m_Walls.Sort(delegate(Wall f1, Wall f2) { return f1.FloorName.CompareTo(f2.FloorName); });
        }

        private static void InitFloors(GraphicsDevice gd)
        {
            m_Floors = new List<Floor>();

            string floors1dat = GlobalSettings.Default.StartupPath + "housedata\\floors\\floors.far";
            string floors2dat = GlobalSettings.Default.StartupPath + "housedata\\floors2\\floors2.far";
            string floors3dat = GlobalSettings.Default.StartupPath + "housedata\\floors3\\floors3.far";
            string floors4dat = GlobalSettings.Default.StartupPath + "housedata\\floors4\\floors4.far";
            string floors5iff = GlobalSettings.Default.StartupPath + "objectdata\\globals\\floors.iff";
            string floors5Namesiff = GlobalSettings.Default.StartupPath + "objectdata\\globals\\build.iff";

            FARArchive floors1 = new FARArchive(floors1dat);
            FARArchive floors2 = new FARArchive(floors2dat);
            FARArchive floors3 = new FARArchive(floors3dat);
            FARArchive floors4 = new FARArchive(floors4dat);
            Iff floors5        = new Iff       (File.OpenRead(floors5iff));
            Iff floors5names   = new Iff       (File.OpenRead(floors5Namesiff));

            // Load the first floor data
            List<KeyValuePair<string, byte[]>> floors = floors1.GetAllEntries();
            foreach (KeyValuePair<string, byte[]> kvp in floors)
            {
                Iff flr;

                if (kvp.Key.Contains("iff") || kvp.Key.Contains("spf"))
                    flr = new Iff(new MemoryStream(kvp.Value));
                else
                    flr = new Iff(new MemoryStream(kvp.Value));

                List<StringTableString> catalogInfo = (flr.StringTables[0].StringSets.Count > 0) ? flr.StringTables[0].StringSets[0].Strings : flr.StringTables[0].Strings;

                lock (m_Floors)
                {
                    m_Floors.Add(new Floor(catalogInfo[0].Str, catalogInfo[1].Str, catalogInfo[2].Str,
                        new Bitmap[] {flr.SPR2s.Find(delegate (SPR2Parser sp) { 
                            return sp.ID == 1; }).GetFrame(0).BitmapData.BitMap, 
                    flr.SPR2s.Find(delegate (SPR2Parser sp) { return sp.ID == 257; }).GetFrame(0).BitmapData.BitMap,
                    flr.SPR2s.Find(delegate (SPR2Parser sp) { return sp.ID == 513; }).GetFrame(0).BitmapData.BitMap},
                        gd, flr.SPR2s.Find(delegate(SPR2Parser sp) { return sp.ID == 1; }).Name));
                }
            }

            LuaInterfaceManager.CallFunction("UpdateLoadingscreen");

            // Load the second floor data
            floors = floors2.GetAllEntries();
            foreach (KeyValuePair<string, byte[]> kvp in floors)
            {
                Iff flr;

                if (kvp.Key.Contains("iff") || kvp.Key.Contains("spf"))
                    flr = new Iff(new MemoryStream(kvp.Value));
                else
                    flr = new Iff(new MemoryStream(kvp.Value));

                List<StringTableString> catalogInfo = (flr.StringTables[0].StringSets.Count > 0) ? flr.StringTables[0].StringSets[0].Strings : flr.StringTables[0].Strings;

                lock (m_Floors)
                {
                    m_Floors.Add(new Floor(catalogInfo[0].Str, catalogInfo[1].Str, catalogInfo[2].Str,
                        new Bitmap[] {flr.SPR2s.Find(delegate (SPR2Parser sp) 
                        { return sp.ID == 1; }).GetFrame(0).BitmapData.BitMap, 
                    flr.SPR2s.Find(delegate (SPR2Parser sp) { return sp.ID == 257; }).GetFrame(0).BitmapData.BitMap,
                    flr.SPR2s.Find(delegate (SPR2Parser sp) { return sp.ID == 513; }).GetFrame(0).BitmapData.BitMap},
                        gd, flr.SPR2s.Find(delegate(SPR2Parser sp) { return sp.ID == 1; }).Name));
                }
            }

            LuaInterfaceManager.CallFunction("UpdateLoadingscreen");

            // Load the third floor data
            floors = floors3.GetAllEntries();
            foreach (KeyValuePair<string, byte[]> kvp in floors)
            {
                Iff flr;

                if (kvp.Key.Contains("iff") || kvp.Key.Contains("spf"))
                    flr = new Iff(new MemoryStream(kvp.Value));
                else
                    flr = new Iff(new MemoryStream(kvp.Value));

                List<StringTableString> catalogInfo = (flr.StringTables[0].StringSets.Count > 0) ? flr.StringTables[0].StringSets[0].Strings : flr.StringTables[0].Strings;

                lock (m_Floors)
                {
                    m_Floors.Add(new Floor(catalogInfo[0].Str, catalogInfo[1].Str, catalogInfo[2].Str,
                        new Bitmap[] { flr.SPR2s.Find(delegate (SPR2Parser sp) 
                        { return sp.ID == 1; }).GetFrame(0).BitmapData.BitMap, 
                    flr.SPR2s.Find(delegate (SPR2Parser sp) { return sp.ID == 257; }).GetFrame(0).BitmapData.BitMap,
                    flr.SPR2s.Find(delegate (SPR2Parser sp) { return sp.ID == 513; }).GetFrame(0).BitmapData.BitMap},
                        gd, flr.SPR2s.Find(delegate(SPR2Parser sp) { return sp.ID == 1; }).Name));
                }
            }

            LuaInterfaceManager.CallFunction("UpdateLoadingscreen");

            // Load the fourth floor data
            floors = floors4.GetAllEntries();
            foreach (KeyValuePair<string, byte[]> kvp in floors)
            {
                Iff flr;

                if (kvp.Key.Contains("iff") || kvp.Key.Contains("spf"))
                    flr = new Iff(new MemoryStream(kvp.Value));
                else
                    flr = new Iff(new MemoryStream(kvp.Value));

                List<StringTableString> catalogInfo = (flr.StringTables[0].StringSets.Count > 0) ? flr.StringTables[0].StringSets[0].Strings : flr.StringTables[0].Strings;

                lock (m_Floors)
                {
                    m_Floors.Add(new Floor(catalogInfo[0].Str, catalogInfo[1].Str, catalogInfo[2].Str,
                        new Bitmap[] {
                    flr.SPR2s.Find(delegate (SPR2Parser sp) { return sp.ID == 1; }).GetFrame(0).BitmapData.BitMap, 
                    flr.SPR2s.Find(delegate (SPR2Parser sp) { return sp.ID == 257; }).GetFrame(0).BitmapData.BitMap,
                    flr.SPR2s.Find(delegate (SPR2Parser sp) { return sp.ID == 513; }).GetFrame(0).BitmapData.BitMap},
                        gd, flr.SPR2s.Find(delegate(SPR2Parser sp) { return sp.ID == 1; }).Name));
                }
            }

            LuaInterfaceManager.CallFunction("UpdateLoadingscreen");

            for (int i = 1; i < 30; i++)
            {
                Bitmap[] frames = new Bitmap[3];

                string spriteName = floors5.SPR2s[i].Name;
                frames[0] = floors5.SPR2s.Find(delegate(SPR2Parser sp) { return sp.ID == i; }).GetFrame(0).BitmapData.BitMap;
                frames[1] = floors5.SPR2s.Find(delegate(SPR2Parser sp) { return sp.ID == i + 256; }).GetFrame(0).BitmapData.BitMap;
                frames[2] = floors5.SPR2s.Find(delegate(SPR2Parser sp) { return sp.ID == i + 512; }).GetFrame(0).BitmapData.BitMap;

                string price = floors5names.StringTables[2].StringSets[0].Strings[(i - 1) * 3].Str;
                string title = floors5names.StringTables[2].StringSets[0].Strings[(i - 1) * 3 + 1].Str;
                string description = floors5names.StringTables[2].StringSets[0].Strings[(i - 1) * 3 + 2].Str;

                m_Floors.Add(new Floor(title, price, description, frames, gd, spriteName));
            }


            myLoadingScreenEWH.Set();
        }

        private static bool bLoadingDone = false;
        public static void SetLoadingDoneSemaphor()
        {
            bLoadingDone = true;
        }

        private static EventWaitHandle myLoadingScreenEWH;

        /// <summary>
        /// Initializes loading of resources.
        /// </summary>
        /// <param name="ScreenMgr">A ScreenManager instance, used to access a GraphicsDevice.</param>
        public static void InitLoading(ScreenManager ScreenMgr)
        {
            myLoadingScreenEWH = new EventWaitHandle(false, EventResetMode.ManualReset, "Go_Away_Stupid_Loading_Screen_GO_U_HEARD_ME_DONT_MAKE_ME_GET_MY_STICK_OUT");
            ThreadPool.QueueUserWorkItem(new WaitCallback(LoadContent), ScreenMgr);
        }

        /// <summary>
        /// Threading function that takes care of loading.
        /// </summary>
        private static void LoadContent(object ScreenMgr)
        {
            ScreenManager Manager = (ScreenManager)ScreenMgr;

            InitWalls(Manager.GraphicsDevice);
            InitFloors(Manager.GraphicsDevice);
        }
    }
}
