using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.content.framework;
using tso.common.content;
using System.Xml;
using tso.content.codecs;
using System.Text.RegularExpressions;
using tso.files.formats.iff;
using tso.files.formats.iff.chunks;
using tso.files.formats.otf;

namespace tso.content
{
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

        public void Init(){
            Iffs = new FAR1Provider<Iff>(ContentManager, new IffCodec(), "objectdata\\objects\\objiff.far");
            Sprites = new FAR1Provider<Iff>(ContentManager, new IffCodec(), new Regex(".*\\\\objspf.*\\.far"));
            TuningTables = new FAR1Provider<OTF>(ContentManager, new OTFCodec(), new Regex(".*\\\\objotf.*\\.far"));

            Iffs.Init();
            Sprites.Init();
            TuningTables.Init();

            /** Load packingslip **/
            Entries = new Dictionary<ulong, GameObjectReference>();
            Cache = new Dictionary<ulong, GameObject>();

            var packingslip = new XmlDataDocument();
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





        /*
        public void Import(Iff iff)
        {
            foreach (OBJD obj in iff.OBJDs)
            {
                if(obj.IsMultiTile == false || obj.IsMaster){
                    var go = new GameObject {
                        GUID = obj.GUID,
                        Iff = iff,
                        Master = obj
                    };
                    objects.Add(go.GUID, go);
                }
            }
        }*/

    }

    public class GameObjectReference : IContentReference<GameObject>
    {
        public ulong ID;
        public string FileName;

        private WorldObjectProvider Provider;

        public GameObjectReference(WorldObjectProvider provider){
            this.Provider = provider;
        }

        #region IContentReference<GameObject> Members

        public GameObject Get()
        {
            return Provider.Get(ID);
        }

        #endregion
    }

    public class GameObject
    {
        public ulong GUID;
        public OBJD OBJ;
        public GameObjectResource Resource;
        
        
        /**public OTFTable ObjectTuning
        {
            get
            {
                return Resource.Tuning.GetTable(4096);
            }
        }**/
    }

    public class GameObjectResource {
        //DO NOT USE THESE, THEY ARE ONLY PUBLIC FOR DEBUG UTILITIES
        public Iff Iff;
        public Iff Sprites;
        public OTF Tuning;

        //private Dictionary<int, DrawGroup> DrawGroupsById;

        public GameObjectResource(Iff iff, Iff sprites, OTF tuning){
            this.Iff = iff;
            this.Sprites = sprites;
            this.Tuning = tuning;

            /*
            DrawGroupsById = new Dictionary<int, DrawGroup>();
            foreach (var dgrp in iff.DrawGroups){
                DrawGroupsById.Add(dgrp.ID, dgrp);
            }
            if (sprites != null)
            {
                foreach (var dgrp in sprites.DrawGroups)
                {
                    DrawGroupsById.Add(dgrp.ID, dgrp);
                }
            }*/
        }

        public T Get<T>(ushort id){
            var type = typeof(T);
            if (type == typeof(OTFTable))
            {
                return (T)(object)Tuning.GetTable(id);
            }

            T item1 = this.Iff.Get<T>(id);
            if (item1 != null){
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

        public List<T> List<T>()
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
