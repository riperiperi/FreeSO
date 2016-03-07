/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Content.Framework;
using FSO.Common.Content;
using System.Xml;
using FSO.Content.Codecs;
using System.Text.RegularExpressions;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using FSO.Files.Formats.OTF;
using System.Collections.Concurrent;
using System.IO;

namespace FSO.Content
{
    /// <summary>
    /// Provides access to binding (*.iff, *.spf, *.otf) data in FAR3 archives.
    /// </summary>
    public class WorldObjectProvider : IContentProvider<GameObject>
    {
        private ConcurrentDictionary<ulong, GameObject> Cache = new ConcurrentDictionary<ulong, GameObject>();
        private FAR1Provider<IffFile> Iffs;
        private FAR1Provider<IffFile> Sprites;
        private FAR1Provider<OTFFile> TuningTables;
        private Content ContentManager;

        public Dictionary<ulong, GameObjectReference> Entries;

        public WorldObjectProvider(Content contentManager)
        {
            this.ContentManager = contentManager;
        }

        private bool WithSprites;

        /// <summary>
        /// Initiates loading of world objects.
        /// </summary>
        public void Init(bool withSprites)
        {
            WithSprites = withSprites;
            Iffs = new FAR1Provider<IffFile>(ContentManager, new IffCodec(), "objectdata/objects/objiff.far");
            
            TuningTables = new FAR1Provider<OTFFile>(ContentManager, new OTFCodec(), new Regex(".*/objotf.*\\.far"));

            Iffs.Init();
            TuningTables.Init();

            if (withSprites)
            {
                Sprites = new FAR1Provider<IffFile>(ContentManager, new IffCodec(), new Regex(".*/objspf.*\\.far"));
                Sprites.Init();
            }

            /** Load packingslip **/
            Entries = new Dictionary<ulong, GameObjectReference>();
            Cache = new ConcurrentDictionary<ulong, GameObject>();

            var packingslip = new XmlDocument();
            packingslip.Load(ContentManager.GetPath("packingslips/objecttable.xml"));
            var objectInfos = packingslip.GetElementsByTagName("I");

            foreach (XmlNode objectInfo in objectInfos)
            {
                ulong FileID = Convert.ToUInt32(objectInfo.Attributes["g"].Value, 16);
                Entries.Add(FileID, new GameObjectReference(this)
                {
                    ID = FileID,
                    FileName = objectInfo.Attributes["n"].Value,
                    Source = GameObjectSource.Far,
                    Name = objectInfo.Attributes["o"].Value,
                    Group = Convert.ToInt16(objectInfo.Attributes["m"].Value),
                    SubIndex = Convert.ToInt16(objectInfo.Attributes["i"].Value)
                });
            }

            //init local objects, piff clones

            string[] paths = Directory.GetFiles("Content/Objects", "*.iff", SearchOption.AllDirectories);
            for (int i = 0; i < paths.Length; i++)
            {
                string entry = paths[i];
                string filename = Path.GetFileName(entry);
                IffFile iffFile = new IffFile(entry);

                var objs = iffFile.List<OBJD>();
                foreach (var obj in objs)
                {
                    Entries.Add(obj.GUID, new GameObjectReference(this)
                    {
                        ID = obj.GUID,
                        FileName = entry,
                        Source = GameObjectSource.Standalone,
                        Name = obj.ChunkLabel,
                        Group = (short)obj.MasterID,
                        SubIndex = obj.SubIndex
                    });
                }
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
            if (Cache.ContainsKey(id))
            {
                return Cache[id];
            }

            lock (Cache)
            {
                if (!Cache.ContainsKey(id))
                {
                    GameObjectReference reference;

                    lock (Entries)
                    {
                        Entries.TryGetValue(id, out reference);
                        if (reference == null) return null;
                        if (ProcessedFiles.Contains(reference.FileName))
                        {
                            return null;
                        }
                    }

                    /** Better set this up! **/
                    IffFile sprites = null, iff = null;
                    OTFFile tuning = null;

                    if (reference.Source == GameObjectSource.Far)
                    {
                        iff = this.Iffs.Get(reference.FileName + ".iff");
                        if (WithSprites) sprites = this.Sprites.Get(reference.FileName + ".spf");
                        tuning = this.TuningTables.Get(reference.FileName + ".otf");
                    } else
                    {
                        iff = new IffFile(reference.FileName);
                        iff.RuntimeInfo.Path = reference.FileName;
                        iff.RuntimeInfo.State = IffRuntimeState.Standalone;
                    }

                    iff.RuntimeInfo.UseCase = IffUseCase.Object;
                    if (sprites != null) sprites.RuntimeInfo.UseCase = IffUseCase.ObjectSprites;

                    ProcessedFiles.Add(reference.FileName);

                    var resource = new GameObjectResource(iff, sprites, tuning, reference.FileName);

                    foreach (var objd in iff.List<OBJD>())
                    {
                        var item = new GameObject
                        {
                            GUID = objd.GUID,
                            OBJ = objd,
                            Resource = resource
                        };
                        Cache.GetOrAdd(item.GUID, item);
                    }
                    //0x3BAA9787
                    if (!Cache.ContainsKey(id))
                    {
                        return null;
                    }
                    return Cache[id];
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
        EXTERNAL MODIFICATION API
        Lets user add/remove/modify object references. (master id/guid/group info)
        */

        public void AddObject(GameObject obj)
        {
            lock (Entries)
            {
                var iff = obj.Resource.MainIff;
                GameObjectSource source;
                switch (iff.RuntimeInfo.State)
                {
                    case IffRuntimeState.PIFFClone:
                        source = GameObjectSource.PIFFClone;
                        break;
                    case IffRuntimeState.Standalone:
                        source = GameObjectSource.Standalone;
                        break;
                    default:
                        source = GameObjectSource.Far;
                        break;
                }

                Entries.Add(obj.GUID, new GameObjectReference(this)
                {
                    ID = obj.GUID,
                    FileName = iff.RuntimeInfo.Path,
                    Source = source,
                    Name = obj.OBJ.ChunkLabel,
                    Group = (short)obj.OBJ.MasterID,
                    SubIndex = obj.OBJ.SubIndex
                });
            }
        }

        public void RemoveObject(uint GUID)
        {
            lock (Entries)
            {
                Entries.Remove(GUID);
            }
        }

        public void ModifyMeta(GameObject obj, uint oldGUID)
        {
            lock (Entries)
            {
                RemoveObject(oldGUID);
                AddObject(obj);
            }
        }
    }

    public class GameObjectReference : IContentReference<GameObject>
    {
        public ulong ID;
        public string FileName;
        public GameObjectSource Source;

        public string Name;
        public short Group;
        public short SubIndex;

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

    public enum GameObjectSource
    {
        Far,
        PIFFClone,
        Standalone
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
        public abstract IffFile MainIff { get; }
        public abstract T Get<T>(ushort id);
        public abstract List<T> List<T>();
        public T[] ListArray<T>()
        {
            List<T> result = List<T>();
            if (result == null) result = new List<T>();
            return result.ToArray();
        }
        public GameGlobalResource SemiGlobal;
    }

    /// <summary>
    /// The resource for an object in the game world.
    /// </summary>
    public class GameObjectResource : GameIffResource
    {
        //DO NOT USE THESE, THEY ARE ONLY PUBLIC FOR DEBUG UTILITIES
        public IffFile Iff;
        public IffFile Sprites;
        public OTFFile Tuning;

        //use this tho
        public string Name;
        public override IffFile MainIff
        {
            get { return Iff; }
        }

        public GameObjectResource(IffFile iff, IffFile sprites, OTFFile tuning, string name)
        {
            this.Iff = iff;
            this.Sprites = sprites;
            this.Tuning = tuning;
            this.Name = name;

            if (iff == null) return;
            var GLOBChunks = iff.List<GLOB>();
            if (GLOBChunks != null && GLOBChunks[0].Name != "")
            {
                var sg = FSO.Content.Content.Get().WorldObjectGlobals.Get(GLOBChunks[0].Name);
                if (sg != null) SemiGlobal = sg.Resource; //used for tuning constant fetching.
            }
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
            if ((type == typeof(SPR2) || type == typeof(SPR) || type == typeof(DGRP)) && this.Sprites != null)
            {
                return this.Sprites.List<T>();
            }
            return this.Iff.List<T>();
        }
    }
}
