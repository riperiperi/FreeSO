using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Content.framework;
using TSO.Common.content;
using System.Xml;
using TSO.Content.codecs;
using System.Text.RegularExpressions;
using TSO.Files.formats.iff;
using TSO.Files.formats.iff.chunks;
using TSO.Files.formats.otf;

namespace TSO.Content
{
    /// <summary>
    /// Provides access to binding (*.iff, *.spf, *.otf) data in FAR3 archives.
    /// </summary>
    public class WorldObjectProvider : IContentProvider<GameObject>
    {
        private Dictionary<ulong, GameObject> Cache = new Dictionary<ulong, GameObject>();
        private FAR1Provider<Iff> Iffs;
        private FAR1Provider<Iff> Sprites;
        private FAR1Provider<OTF> TuningTables;
        private Content ContentManager;

        private Dictionary<ulong, GameObjectReference> Entries;

        public WorldObjectProvider(Content contentManager)
        {
            this.ContentManager = contentManager;
        }

        /// <summary>
        /// Initiates loading of world objects.
        /// </summary>
        public void Init()
        {
            Iffs = new FAR1Provider<Iff>(ContentManager, new IffCodec(), "objectdata\\objects\\objiff.far");
            Sprites = new FAR1Provider<Iff>(ContentManager, new IffCodec(), new Regex(".*\\\\objspf.*\\.far"));
            TuningTables = new FAR1Provider<OTF>(ContentManager, new OTFCodec(), new Regex(".*\\\\objotf.*\\.far"));

            Iffs.Init();
            TuningTables.Init();
            Sprites.Init();

            /** Load packingslip **/
            Entries = new Dictionary<ulong, GameObjectReference>();
            Cache = new Dictionary<ulong, GameObject>();

            var packingslip = new XmlDocument();
            packingslip.Load(ContentManager.GetPath("packingslips\\objecttable.xml"));
            var objectInfos = packingslip.GetElementsByTagName("I");

            foreach (XmlNode objectInfo in objectInfos)
            {
                ulong FileID = Convert.ToUInt32(objectInfo.Attributes["g"].Value, 16);
                Entries.Add(FileID, new GameObjectReference(this)
                {
                    ID = FileID,
                    FileName = objectInfo.Attributes["n"].Value
                });
            }


            var donwloadsInfo = new XmlDocument();
            donwloadsInfo.Load(this.ContentManager.GetPath(@"packingslips\downloads.xml"));
            XmlNodeList list3 = donwloadsInfo.GetElementsByTagName("I");
            foreach (XmlNode node3 in list3)
            {
                ulong num;
                num = Convert.ToUInt32(node3.Attributes["g"].Value, 0x10);
                this.Entries.Add(num, new GameObjectReference(this) { ID = num, FileName = node3.Attributes["n"].Value });
            }

        }

        private List<string> ProcessedFiles = new List<string>();

        #region IContentProvider<GameObject> Members

        public GameObject Get(uint id)
        {
            return Get((ulong)id);
        }

        public GameObject Get(ulong id)
        {
            lock (Cache)
            {
                if (Cache.ContainsKey(id))
                {
                    return Cache[id];
                }

                var reference = this.Entries[id];
                if (ProcessedFiles.Contains(reference.FileName)){
                    return null;
                }

                /** Better set this up! **/
                var iff = this.Iffs.Get(reference.FileName + ".iff");
                var sprites = this.Sprites.Get(reference.FileName + ".spf");
                var tuning = this.TuningTables.Get(reference.FileName + ".otf");
                ProcessedFiles.Add(reference.FileName);

                if (iff == null)
                {
                    //Get objects from iff, if there is no file from far with that id;
                    iff = new Iff(this.ContentManager.GetPath(@"userdata\downloads\") + reference.FileName + ".iff");
                    var resource = new GameObjectResource(iff, null, null);
                    foreach (OBJD objd in iff.List<OBJD>())
                    {
                        var obj = new GameObject
                        {
                            GUID = (ulong)objd.GUID,
                            OBJ = objd,
                            Resource = resource
                        };
                        if (!this.Cache.ContainsKey(obj.GUID))
                        {
                            this.Cache.Add(obj.GUID, obj);
                        }
                    }
                }
                else
                {
                var resource = new GameObjectResource(iff, sprites, tuning);

                foreach (var objd in iff.List<OBJD>())
                {
                    var item = new GameObject
                    {
                        GUID = objd.GUID,
                        OBJ = objd,
                        Resource = resource
                    };
                    if (!Cache.ContainsKey(item.GUID))
                    {
                        Cache.Add(item.GUID, item);
                    }
                }

                }
                //0x3BAA9787
                if (!Cache.ContainsKey(id))
                {
                    return null;
                }
                return Cache[id];
                
            }
        }

        public GameObject Get(uint type, uint fileID)
        {
            return Get(fileID);
        }

        public List<IContentReference<GameObject>> List()
        {
            return null;
        }

        #endregion
    }

    public class GameObjectReference : IContentReference<GameObject>
    {
        public ulong ID;
        public string FileName;

        private WorldObjectProvider Provider;

        public GameObjectReference(WorldObjectProvider provider)
        {
            this.Provider = provider;
        }

        #region IContentReference<GameObject> Members

        public GameObject Get()
        {
            return Provider.Get(ID);
        }

        #endregion
    }

    /// <summary>
    /// An object in the game world.
    /// </summary>
    public class GameObject
    {
        public ulong GUID;
        public OBJD OBJ;
        public GameObjectResource Resource;
    }

    public abstract class GameIffResource
    {
        public abstract T Get<T>(ushort id);
        public abstract List<T> List<T>();
        public GameGlobalResource SemiGlobal;
    }

    /// <summary>
    /// The resource for an object in the game world.
    /// </summary>
    public class GameObjectResource : GameIffResource
    {
        //DO NOT USE THESE, THEY ARE ONLY PUBLIC FOR DEBUG UTILITIES
        public Iff Iff;
        public Iff Sprites;
        public OTF Tuning;

        public GameObjectResource(Iff iff, Iff sprites, OTF tuning)
        {
            this.Iff = iff;
            this.Sprites = sprites;
            this.Tuning = tuning;
        }

        /// <summary>
        /// Gets a game object's resource based on the ID found in the object's OTF.
        /// </summary>
        /// <typeparam name="T">Type of object reource to load (IFF, SPF).</typeparam>
        /// <param name="id">ID of the resource to load.</param>
        /// <returns>An object's resource of the specified type.</returns>
        public override T Get<T>(ushort id)
        {
            var type = typeof(T);
            if (type == typeof(OTFTable))
            {
                if (Tuning != null)
                {
                    return (T)(object)Tuning.GetTable(id);
                }
                else
                {
                    return default(T);
                }
            }

            T item1 = this.Iff.Get<T>(id);
            if (item1 != null)
            {
                return item1;
            }

            if (this.Sprites != null)
            {
                T item2 = this.Sprites.Get<T>(id);
                if (item2 != null)
                {
                    return item2;
                }
            }
            return default(T);
        }

        public override List<T> List<T>()
        {
            var type = typeof(T);
            if (type == typeof(SPR2) || type == typeof(SPR) || type == typeof(DGRP))
            {
                return this.Sprites.List<T>();
            }
            return this.Iff.List<T>();
        }
    }
}
