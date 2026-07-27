using FSO.LotView.Model;
using FSO.SimAntics.Entities;
using FSO.SimAntics.Model.TSOPlatform;

namespace FSO.SimAntics.Model
{
    public class VMObjectQueries
    {
        private VMContext Context;
        private Dictionary<int, VMObjectList<VMEntity>> TileToObjects = [];

        private Dictionary<uint, VMObjectList<VMEntity>> ObjectsByGUID = [];
        private Dictionary<short, VMObjectList<VMEntity>> ObjectsByCategory = [];
        private Dictionary<string, VMObjectList<VMEntity>> ObjectsBySemiGlobal = [];
        public VMObjectList<VMEntity> Avatars = [];
        public Dictionary<uint, VMAvatar> AvatarsByPersist = [];
        public Dictionary<uint, VMMultitileGroup> MultitileByPersist = [];
        public VMObjectList<VMEntity> WithAutonomy = [];

        public int NumUserObjects
        {
            get
            {
                //if we're not a community lot, we can short ciruit this.
                if (!Context.VM.TSOState.CommunityLot) return MultitileByPersist.Count;

                return MultitileByPersist.Count(x => (((x.Value.BaseObject.TSOState as VMTSOObjectState)
                    ?.ObjectFlags ?? 0) & VMTSOObjectFlags.FSODonated) == 0);
            }
        }

        public int NumDonatedObjects
        {
            get
            {
                return MultitileByPersist.Count(x => (((x.Value.BaseObject.TSOState as VMTSOObjectState)
                    ?.ObjectFlags ?? 0) & VMTSOObjectFlags.FSODonated) > 0);
            }
        }

        public VMObjectQueries(VMContext context)
        {
            Context = context;
        }

        private int GetOffest(LotTilePos pos)
        {
            if (pos == LotTilePos.OUT_OF_WORLD) return -1;
            return pos.TileX + pos.TileY * Context.Architecture.Width + (pos.Level - 1) * Context.Architecture.Width * Context.Architecture.Height;
        }

        public void RegisterObjectPos(VMEntity ent)
        {
            var off = GetOffest(ent.Position);

            if (!TileToObjects.TryGetValue(off, out var tile))
            {
                tile = [];
                TileToObjects.Add(off, tile);
            }

            tile.AddToObjList(ent); //if it's already on this tile, this will do nothing
        }

        public void UnregisterObjectPos(VMEntity ent)
        {
            var off = GetOffest(ent.Position);

            if (TileToObjects.TryGetValue(off, out var tile))
            {
                tile.DeleteFromObjList(ent);
                if (tile.Count == 0) TileToObjects.Remove(off);
            }
        }

        /// <summary>
        /// Debug function. Call to make sure positions are correctly registered.
        /// </summary>
        public void VerifyPositions()
        {
            foreach (var objs in TileToObjects)
            {
                var off = objs.Key;
                var tileX = off%Context.Architecture.Width;
                var tileY = (off / Context.Architecture.Width) % (Context.Architecture.Height);
                var level = (off / (Context.Architecture.Width * Context.Architecture.Height)) + 1;

                foreach (var obj in objs.Value)
                {
                    if (off == -1)
                    {
                        if (obj.Position != LotTilePos.OUT_OF_WORLD) throw new Exception("Should be out of World!");
                    }
                    else if (obj.Position.TileX != tileX || obj.Position.TileY != tileY || obj.Position.Level != level)
                        throw new Exception("Invalid Position Assignment!!");
                    if (obj.Dead) throw new Exception("but it's dead!");
                }
            }
        }

        public void RegisterAvatarPersist(VMAvatar ava, uint persistID)
        {
            if (persistID != 0) AvatarsByPersist[persistID] = ava;
        }

        public void RemoveAvatarPersist(uint persistID)
        {
            AvatarsByPersist.Remove(persistID);
        }

        public void RegisterMultitilePersist(VMMultitileGroup mul, uint persistID)
        {
            if (persistID != 0) MultitileByPersist[persistID] = mul;
        }

        public void RemoveMultitilePersist(VM vm, uint persistID)
        {
            MultitileByPersist.Remove(persistID);
            if (vm.PlatformState.LimitExceeded) VMBuildableAreaInfo.UpdateOverbudgetObjects(vm);
        }

        public void RegisterCategory(VMEntity obj, short category)
        {
            if (!ObjectsByCategory.TryGetValue(category, out var tile))
            {
                tile = [];
                ObjectsByCategory.Add(category, tile);
            }

            //debug check: use if things are going weird
            //if (!tile.Contains(obj))
            tile.AddToObjList(obj); 
        }

        public void RemoveCategory(VMEntity obj, short category)
        {
            if (ObjectsByCategory.TryGetValue(category, out var tile))
            {
                tile.DeleteFromObjList(obj);
                if (tile.Count == 0) ObjectsByCategory.Remove(category);
            }
        }

        public void RegisterSemiGlobal(VMEntity obj, string semiGlobal)
        {
            VMObjectList<VMEntity> tile;
            if (semiGlobal != null)
            {
                if (!ObjectsBySemiGlobal.TryGetValue(semiGlobal.ToLowerInvariant(), out tile))
                {
                    tile = [];
                    ObjectsBySemiGlobal.Add(semiGlobal.ToLowerInvariant(), tile);
                }

                //debug check: use if things are going weird
                //if (!tile.Contains(obj))
                tile.AddToObjList(obj);
            }
        }

        public void RemoveSemiGlobal(VMEntity obj, string semiGlobal)
        {
            VMObjectList<VMEntity> tile;
            if (semiGlobal != null)
            {
                if (ObjectsBySemiGlobal.TryGetValue(semiGlobal, out tile))
                {
                    tile.DeleteFromObjList(obj);
                    if (tile.Count == 0) ObjectsBySemiGlobal.Remove(semiGlobal);
                }
            }
        }

        public void NewObject(VMEntity obj)
        {
            var guid = obj.Object.OBJ.GUID;

            if (!ObjectsByGUID.TryGetValue(guid, out var list))
            {
                list = [];
                ObjectsByGUID.Add(guid, list);
            }

            list.AddToObjList(obj);
            RegisterCategory(obj, obj.GetValue(VMStackObjectVariable.Category));

            if (obj.SemiGlobal != null)
            {
                if (obj.SemiGlobal.Iff.Filename != null) //sanity check
                {
                    RegisterSemiGlobal(obj, obj.SemiGlobal.Iff.Filename);
                }
            }

            if (obj is VMAvatar)
            {
                Avatars.AddToObjList(obj);
                if (obj.PersistID != 0) AvatarsByPersist[obj.PersistID] = (VMAvatar)obj;
            }

            if (obj.TreeTable != null && obj.TreeTable.AutoInteractions.Length > 0)
            {
                WithAutonomy.AddToObjList(obj);
            }
        }

        public void RemoveObject(VMEntity obj)
        {
            var guid = obj.Object.OBJ.GUID;

            if (ObjectsByGUID.TryGetValue(guid, out var list))
            {
                list.DeleteFromObjList(obj);
                if (list.Count == 0) ObjectsByGUID.Remove(guid);
            }

            RemoveCategory(obj, obj.GetValue(VMStackObjectVariable.Category));

            if (obj.SemiGlobal != null)
            {
                if (obj.SemiGlobal.Iff.Filename != null) //sanity check
                {
                    RemoveSemiGlobal(obj, obj.SemiGlobal.Iff.Filename.ToLowerInvariant());
                }
            }

            if (obj is VMAvatar)
            {
                Avatars.DeleteFromObjList(obj);
                AvatarsByPersist.Remove(obj.PersistID);
            }
            else if (obj.PersistID > 0 && obj.MultitileGroup.Objects.Count == 1)
            {
                MultitileByPersist.Remove(obj.PersistID);
                if (obj.Thread != null)
                {
                    var vm = obj.Thread.Context.VM;
                    if (vm.PlatformState.LimitExceeded) VMBuildableAreaInfo.UpdateOverbudgetObjects(vm);
                }
            }

            if (obj.TreeTable != null && obj.TreeTable.AutoInteractions.Length > 0)
            {
                WithAutonomy.DeleteFromObjList(obj);
            }
        }

        public VMObjectList<VMEntity> GetObjectsAt(LotTilePos pos)
        {
            var off = GetOffest(pos);

            TileToObjects.TryGetValue(off, out var tile);

            return tile;
        }

        public VMObjectList<VMEntity> GetObjectsByGUID(uint guid)
        {
            ObjectsByGUID.TryGetValue(guid, out var tile);
            return tile;
        }

        public VMObjectList<VMEntity> GetObjectsByCategory(short category)
        {
            ObjectsByCategory.TryGetValue(category, out var tile);
            return tile;
        }

        public VMObjectList<VMEntity> GetObjectsBySemiGlobal(string semiGlobal)
        {
            ObjectsBySemiGlobal.TryGetValue(semiGlobal.ToLowerInvariant(), out var tile);
            return tile;
        }
    }
}
