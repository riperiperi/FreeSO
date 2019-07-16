using FSO.Common.Content;
using FSO.Common.Utils;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using FSO.Files.Formats.OTF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Content.Interfaces
{
    public abstract class AbstractObjectProvider : IContentProvider<GameObject>
    {
        protected TimedReferenceCache<ulong, GameObject> Cache = new TimedReferenceCache<ulong, GameObject>();
        protected Content ContentManager;

        public Dictionary<ulong, GameObjectReference> Entries;
        public List<GameObjectReference> ControllerObjects = new List<GameObjectReference>();

        public AbstractObjectProvider(Content contentManager)
        {
            this.ContentManager = contentManager;
        }

        private bool WithSprites;

        protected TimedReferenceCache<string, GameObjectResource> ProcessedFiles = new TimedReferenceCache<string, GameObjectResource>();

        #region IContentProvider<GameObject> Members

        public GameObject Get(uint id)
        {
            return Get((ulong)id);
        }

        protected abstract Func<string, GameObjectResource> GenerateResource(GameObjectReference reference);

        public GameObject Get(ulong id)
        {
            return Cache.GetOrAdd(id, (_) =>
            {
                GameObjectReference reference;
                GameObjectResource resource = null;

                lock (Entries)
                {
                    Entries.TryGetValue(id, out reference);
                    if (reference == null)
                    {
                        Console.WriteLine("Failed to get Object ID: " + id.ToString() + " (no resource)");
                        return null;
                    }
                    /*
                    lock (ProcessedFiles)
                    {
                        //if a file is processed but an object in it is not in the cache, it may have changed.
                        //check for it again!
                        ProcessedFiles.TryGetValue(reference.FileName, out resource);
                    }
                    */
                }

                resource = ProcessedFiles.GetOrAdd(reference.FileName, GenerateResource(reference));
                if (resource.MainIff == null) return null;
                foreach (var objd in resource.MainIff.List<OBJD>())
                {
                    if (objd.GUID == id)
                    {
                        var item = new GameObject
                        {
                            GUID = objd.GUID,
                            OBJ = objd,
                            Resource = resource
                        };
                        return item; //found it!
                    }
                }
                Console.WriteLine("Failed to get Object ID: " + id.ToString() + " from resource " + resource.Name);
                return null;
            });
        }

        public GameObject Get(uint type, uint fileID)
        {
            return Get(fileID);
        }

        public GameObject Get(ContentID id)
        {
            throw new NotImplementedException();
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
                AddObject(iff, obj.OBJ);
            }
        }

        public void AddObject(IffFile iff, OBJD obj)
        {
            lock (Entries)
            {
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
                    Name = obj.ChunkLabel,
                    Group = (short)obj.MasterID,
                    SubIndex = obj.SubIndex
                });
            }
        }

        public void RemoveObject(uint GUID)
        {
            lock (Entries)
            {
                Entries.Remove(GUID);
            }
            lock (Cache)
            {
                GameObject removed;
                Cache.TryRemove(GUID, out removed);
            }
        }

        public void ResetFile(IffFile iff)
        {
            lock (Entries)
            {
                var ToRemove = new List<uint>();
                foreach (var objt in Entries)
                {
                    var obj = objt.Value;
                    if (obj.FileName == iff.RuntimeInfo.Path) ToRemove.Add((uint)objt.Key);
                }
                foreach (var guid in ToRemove)
                {
                    Entries.Remove(guid);
                }

                //add all OBJDs
                var list = iff.List<OBJD>();
                if (list != null)
                {
                    foreach (var obj in list) AddObject(iff, obj);
                }
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

        public GameObject Get(string name)
        {
            //special: attempt to find the first object in the specified file.
            if (name.EndsWith(".iff")) name = name.Substring(0, name.Length - 4);
            ulong guid = 0;
            lock (Entries)
            {
                var result = Entries.Values.FirstOrDefault(x => x.FileName == name);
                if (result != null)
                {
                    guid = result.ID;
                }
            }
            if (guid == 0) return null;
            return Get(guid);
        }
    }
}
