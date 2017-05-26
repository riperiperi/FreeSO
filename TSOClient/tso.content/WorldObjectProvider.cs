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
using FSO.Common;
using FSO.Common.Utils;
using FSO.Content.Interfaces;

namespace FSO.Content
{
    /// <summary>
    /// Provides access to binding (*.iff, *.spf, *.otf) data in FAR3 archives.
    /// </summary>
    public class WorldObjectProvider : AbstractObjectProvider
    {
        private FAR1Provider<IffFile> Iffs;
        private FAR1Provider<IffFile> Sprites;
        private FAR1Provider<OTFFile> TuningTables;

        public WorldObjectProvider(Content contentManager) : base(contentManager)
        {
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
            Cache = new TimedReferenceCache<ulong, GameObject>();

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

            //Directory.CreateDirectory(Path.Combine(FSOEnvironment.ContentDir, "Objects"));
            string[] paths = Directory.GetFiles(Path.Combine(FSOEnvironment.ContentDir, "Objects"), "*.iff", SearchOption.AllDirectories);
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

        protected override Func<string, GameObjectResource> GenerateResource(GameObjectReference reference)
        {
            return (fname) =>
            {
                /** Better set this up! **/
                IffFile sprites = null, iff = null;
                OTFFile tuning = null;

                if (reference.Source == GameObjectSource.Far)
                {
                    iff = this.Iffs.Get(reference.FileName + ".iff");
                    iff.RuntimeInfo.Path = reference.FileName;
                    if (WithSprites) sprites = this.Sprites.Get(reference.FileName + ".spf");
                    var rewrite = PIFFRegistry.GetOTFRewrite(reference.FileName + ".otf");
                    try
                    {
                        tuning = (rewrite != null) ? new OTFFile(rewrite) : this.TuningTables.Get(reference.FileName + ".otf");
                    }
                    catch (Exception)
                    {
                        //if any issues occur loading an otf, just silently ignore it.
                    }
                }
                else
                {
                    iff = new IffFile(reference.FileName);
                    iff.RuntimeInfo.Path = reference.FileName;
                    iff.RuntimeInfo.State = IffRuntimeState.Standalone;
                }

                if (iff.RuntimeInfo.State == IffRuntimeState.PIFFPatch)
                {
                    //OBJDs may have changed due to patch. Remove all file references
                    ResetFile(iff);
                }

                iff.RuntimeInfo.UseCase = IffUseCase.Object;
                if (sprites != null) sprites.RuntimeInfo.UseCase = IffUseCase.ObjectSprites;

                return new GameObjectResource(iff, sprites, tuning, reference.FileName);
            };
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

        private AbstractObjectProvider Provider;

        public GameObjectReference(AbstractObjectProvider provider)
        {
            this.Provider = provider;
        }

        #region IContentReference<GameObject> Members

        public GameObject Get()
        {
            return Provider.Get(ID);
        }

        public object GetThrowawayGeneric()
        {
            throw new NotImplementedException();
        }

        public object GetGeneric()
        {
            return Get();
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
        public Dictionary<string, VMTreeByNameTableEntry> TreeByName;
        public override IffFile MainIff
        {
            get { return Iff; }
        }

        public GameObjectResource(IffFile iff, IffFile sprites, OTFFile tuning, string iname)
        {
            this.Iff = iff;
            this.Sprites = sprites;
            this.Tuning = tuning;
            this.Name = iname;

            if (iff == null) return;
            var GLOBChunks = iff.List<GLOB>();
            if (GLOBChunks != null && GLOBChunks[0].Name != "")
            {
                var sg = FSO.Content.Content.Get().WorldObjectGlobals.Get(GLOBChunks[0].Name);
                if (sg != null) SemiGlobal = sg.Resource; //used for tuning constant fetching.
            }

            TreeByName = new Dictionary<string, VMTreeByNameTableEntry>();
            var bhavs = List<BHAV>();
            if (bhavs != null)
            {
                foreach (var bhav in bhavs)
                {
                    string name = bhav.ChunkLabel;
                    for (var i = 0; i < name.Length; i++)
                    {
                        if (name[i] == 0)
                        {
                            name = name.Substring(0, i);
                            break;
                        }
                    }
                    if (!TreeByName.ContainsKey(name)) TreeByName.Add(name, new VMTreeByNameTableEntry(bhav));
                }
            }
            //also add semiglobals

            if (SemiGlobal != null)
            {
                bhavs = SemiGlobal.List<BHAV>();
                if (bhavs != null)
                {
                    foreach (var bhav in bhavs)
                    {
                        string name = bhav.ChunkLabel;
                        for (var i = 0; i < name.Length; i++)
                        {
                            if (name[i] == 0)
                            {
                                name = name.Substring(0, i);
                                break;
                            }
                        }
                        if (!TreeByName.ContainsKey(name)) TreeByName.Add(name, new VMTreeByNameTableEntry(bhav));
                    }
                }
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

    public class VMTreeByNameTableEntry
    {
        public BHAV bhav;

        public VMTreeByNameTableEntry(BHAV bhav)
        {
            this.bhav = bhav;
        }
    }

}
