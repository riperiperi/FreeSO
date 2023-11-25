using FSO.LotView.Model;
using FSO.SimAntics.Entities;
using FSO.SimAntics.Model.TSOPlatform;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FSO.SimAntics.Model
{
    public class VMObjectQueries
    {
        private VMContext Context;
        private Dictionary<int, List<VMEntity>> TileToObjects = new Dictionary<int, List<VMEntity>>();

        private Dictionary<uint, List<VMEntity>> ObjectsByGUID = new Dictionary<uint, List<VMEntity>>();
        private Dictionary<short, List<VMEntity>> ObjectsByCategory = new Dictionary<short, List<VMEntity>>();
        private Dictionary<string, List<VMEntity>> ObjectsBySemiGlobal = new Dictionary<string, List<VMEntity>>();
        public List<VMEntity> Avatars = new List<VMEntity>();
        public Dictionary<uint, VMAvatar> AvatarsByPersist = new Dictionary<uint, VMAvatar>();
        public Dictionary<uint, VMMultitileGroup> MultitileByPersist = new Dictionary<uint, VMMultitileGroup>();
        public List<VMEntity> WithAutonomy = new List<VMEntity>();

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

            List<VMEntity> tile;
            if (!TileToObjects.TryGetValue(off, out tile))
            {
                tile = new List<VMEntity>();
                TileToObjects.Add(off, tile);
            }

            VM.AddToObjList(tile, ent); //if it's already on this tile, this will do nothing
        }

        public void UnregisterObjectPos(VMEntity ent)
        {
            var off = GetOffest(ent.Position);

            List<VMEntity> tile;
            if (TileToObjects.TryGetValue(off, out tile))
            {
                tile.Remove(ent);
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
            List<VMEntity> tile;
            if (!ObjectsByCategory.TryGetValue(category, out tile))
            {
                tile = new List<VMEntity>();
                ObjectsByCategory.Add(category, tile);
            }

            //debug check: use if things are going weird
            //if (!tile.Contains(obj))
            VM.AddToObjList(tile, obj); 
        }

        public void RemoveCategory(VMEntity obj, short category)
        {
            List<VMEntity> tile;

            if (ObjectsByCategory.TryGetValue(category, out tile))
            {
                VM.DeleteFromObjList(tile, obj);
                if (tile.Count == 0) ObjectsByCategory.Remove(category);
            }
        }

        public void RegisterSemiGlobal(VMEntity obj, string semiGlobal)
        {
            List<VMEntity> tile;
            if (semiGlobal != null)
            {
                if (!ObjectsBySemiGlobal.TryGetValue(semiGlobal.ToLowerInvariant(), out tile))
                {
                    tile = new List<VMEntity>();
                    ObjectsBySemiGlobal.Add(semiGlobal.ToLowerInvariant(), tile);
                }

                //debug check: use if things are going weird
                //if (!tile.Contains(obj))
                VM.AddToObjList(tile, obj);
            }
        }

        public void RemoveSemiGlobal(VMEntity obj, string semiGlobal)
        {
            List<VMEntity> tile;
            if (semiGlobal != null)
            {
                if (ObjectsBySemiGlobal.TryGetValue(semiGlobal, out tile))
                {
                    VM.DeleteFromObjList(tile, obj);
                    if (tile.Count == 0) ObjectsBySemiGlobal.Remove(semiGlobal);
                }
            }
        }

        public void NewObject(VMEntity obj)
        {
            var guid = obj.Object.OBJ.GUID;

            List<VMEntity> list;
            if (!ObjectsByGUID.TryGetValue(guid, out list))
            {
                list = new List<VMEntity>();
                ObjectsByGUID.Add(guid, list);
            }

            VM.AddToObjList(list, obj);
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
                VM.AddToObjList(Avatars, obj);
                if (obj.PersistID != 0) AvatarsByPersist[obj.PersistID] = (VMAvatar)obj;
            }

            if (obj.TreeTable != null && obj.TreeTable.AutoInteractions.Length > 0)
            {
                VM.AddToObjList(WithAutonomy, obj);
            }
        }

        public void RemoveObject(VMEntity obj)
        {
            var guid = obj.Object.OBJ.GUID;

            List<VMEntity> list;
            if (ObjectsByGUID.TryGetValue(guid, out list))
            {
                VM.DeleteFromObjList(list, obj);
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
                Avatars.Remove(obj);
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
                WithAutonomy.Remove(obj);
            }
        }

        public List<VMEntity> GetObjectsAt(LotTilePos pos)
        {
            var off = GetOffest(pos);

            List<VMEntity> tile;
            TileToObjects.TryGetValue(off, out tile);

            return tile;
        }

        public List<VMEntity> GetObjectsByGUID(uint guid)
        {
            List<VMEntity> tile;
            ObjectsByGUID.TryGetValue(guid, out tile);
            return tile;
        }

        public List<VMEntity> GetObjectsByCategory(short category)
        {
            List<VMEntity> tile;
            ObjectsByCategory.TryGetValue(category, out tile);
            return tile;
        }

        public List<VMEntity> GetObjectsBySemiGlobal(string semiGlobal)
        {
            List<VMEntity> tile;
            ObjectsBySemiGlobal.TryGetValue(semiGlobal.ToLowerInvariant(), out tile);
            return tile;
        }
    }
}
