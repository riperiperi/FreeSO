using FSO.LotView.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.SimAntics.Model
{
    public class VMSetToNextCache
    {
        private VMContext Context;
        private Dictionary<int, List<VMEntity>> TileToObjects = new Dictionary<int, List<VMEntity>>();

        private Dictionary<uint, List<VMEntity>> ObjectsByGUID = new Dictionary<uint, List<VMEntity>>();
        public List<VMEntity> Avatars = new List<VMEntity>();

        public VMSetToNextCache(VMContext context)
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
            List<VMEntity> tile = null;
            TileToObjects.TryGetValue(off, out tile);
            if (tile == null)
            {
                tile = new List<VMEntity>();
                TileToObjects.Add(off, tile);
            }
            VM.AddToObjList(tile, ent);
        }

        public void UnregisterObjectPos(VMEntity ent)
        {
            var off = GetOffest(ent.Position);
            List<VMEntity> tile = null;
            TileToObjects.TryGetValue(off, out tile);
            if (tile == null) return; //???
            tile.Remove(ent);
            if (tile.Count == 0) TileToObjects.Remove(off);
        }

        public void NewObject(VMEntity obj)
        {
            var guid = obj.Object.OBJ.GUID;
            List<VMEntity> list = null;
            ObjectsByGUID.TryGetValue(guid, out list);
            if (list == null)
            {
                list = new List<VMEntity>();
                ObjectsByGUID.Add(guid, list);
            }
            VM.AddToObjList(list, obj);

            if (obj is VMAvatar) VM.AddToObjList(Avatars, obj);
        }

        public void RemoveObject(VMEntity obj)
        {
            var guid = obj.Object.OBJ.GUID;
            List<VMEntity> list = null;
            ObjectsByGUID.TryGetValue(guid, out list);
            if (list != null)
            {
                list.Remove(obj);
                if (list.Count == 0) ObjectsByGUID.Remove(guid);
            }

            if (obj is VMAvatar) Avatars.Remove(obj);
        }

        public List<VMEntity> GetObjectsAt(LotTilePos pos)
        {
            var off = GetOffest(pos);
            List<VMEntity> tile = null;
            TileToObjects.TryGetValue(off, out tile);
            return tile;
        }

        public List<VMEntity> GetObjectsByGUID(uint guid)
        {
            List<VMEntity> tile = null;
            ObjectsByGUID.TryGetValue(guid, out tile);
            return tile;
        }
    }
}
