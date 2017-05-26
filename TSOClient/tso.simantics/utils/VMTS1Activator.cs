using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using FSO.LotView;
using FSO.LotView.Components;
using FSO.LotView.Model;
using FSO.SimAntics.Model;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FSO.Files.Formats.IFF.Chunks.OBJM;

namespace FSO.SimAntics.Utils
{
    public class VMTS1Activator
    {
        private VM VM;
        private LotView.World World;
        private Blueprint Blueprint;

        public VMTS1Activator(VM vm, LotView.World world)
        {
            this.VM = vm;
            this.World = world;
        }

        public Blueprint LoadFromIff(IffFile iff)
        {
            var size = 64; //ts1 lots are fixed size
            if (VM.UseWorld) this.Blueprint = new Blueprint(size, size);
            VM.Entities = new List<VMEntity>();
            VM.Scheduler = new Engine.VMScheduler(VM);
            VM.Context = new VMContext(VM.Context.World);
            VM.Context.Globals = FSO.Content.Content.Get().WorldObjectGlobals.Get("global");
            VM.Context.VM = VM;
            VM.Context.Blueprint = Blueprint;
            VM.Context.Architecture = new VMArchitecture(size, size, Blueprint, VM.Context);

            var floorM = iff.Get<FLRm>(1)?.Entries ?? iff.Get<FLRm>(0)?.Entries ?? new List<WALmEntry>();
            var wallM = iff.Get<WALm>(1)?.Entries ?? iff.Get<WALm>(0)?.Entries ?? new List<WALmEntry>();
            var floorDict = BuildFloorDict(floorM);
            var wallDict = BuildWallDict(wallM);

            var arch = VM.Context.Architecture;
            //altitude as 0
            var advFloors = iff.Get<ARRY>(11);
            if (advFloors != null)
            {
                //advanced walls and floors from modern ts1. use 16 bit wall/floor data.
                arch.Floors[0] = RemapFloors(DecodeAdvFloors(advFloors.TransposeData), floorDict);
                arch.Floors[1] = RemapFloors(DecodeAdvFloors(iff.Get<ARRY>(111).TransposeData), floorDict);
                //objects as 3
                arch.Walls[0] = RemapWalls(DecodeAdvWalls(iff.Get<ARRY>(12).TransposeData), wallDict, floorDict);
                arch.Walls[1] = RemapWalls(DecodeAdvWalls(iff.Get<ARRY>(112).TransposeData), wallDict, floorDict);
            } else
            {
                arch.Floors[0] = RemapFloors(DecodeFloors(iff.Get<ARRY>(1).TransposeData), floorDict);
                arch.Floors[1] = RemapFloors(DecodeFloors(iff.Get<ARRY>(101).TransposeData), floorDict);
                //objects as 3
                arch.Walls[0] = RemapWalls(DecodeWalls(iff.Get<ARRY>(2).TransposeData), wallDict, floorDict);
                arch.Walls[1] = RemapWalls(DecodeWalls(iff.Get<ARRY>(102).TransposeData), wallDict, floorDict);
            }
            //objects as 103
            arch.Terrain.GrassState = iff.Get<ARRY>(6).TransposeData.Select(x => (byte)(127-x)).ToArray();
            //arch.Terrain.DarkType = Content.Model.TerrainType.SAND;
            //arch.Terrain.LightType = Content.Model.TerrainType.GRASS;
            arch.SignalTerrainRedraw();
            //targetgrass is 7
            //flags is 8/108
            var pools = iff.Get<ARRY>(9).TransposeData;
            var water = iff.Get<ARRY>(10).TransposeData;

            for (int i=0; i<pools.Length; i++)
            {
                //pools in freeso are slightly different
                if (pools[i] != 0xff && pools[i] != 0x0) arch.Floors[0][i].Pattern = 65535;
                if (water[i] != 0xff && water[i] != 0x0) arch.Floors[0][i].Pattern = 65534;
            }

            if (VM.UseWorld)
            {
                World.State.WorldSize = size;
                Blueprint.Terrain = CreateTerrain(size);
            }

            arch.RebuildWallsAt();

            arch.RegenRoomMap();
            VM.Context.RegeneratePortalInfo();
            
            
            var objm = iff.Get<OBJM>(1);

            var objt = iff.Get<OBJT>(0);
            int j = 0;

            for (int i=0; i< objm.IDToOBJT.Length; i+=2)
            {
                if (objm.IDToOBJT[i] == 0) continue;
                MappedObject target;
                if (!objm.ObjectData.TryGetValue(objm.IDToOBJT[i], out target)) continue;
                var entry = objt.Entries[objm.IDToOBJT[i + 1] - 1];
                target.Name = entry.Name;
                target.GUID = entry.GUID;

                Console.WriteLine((objm.IDToOBJT[i]) + ": " + objt.Entries[objm.IDToOBJT[i+1]-1].Name);
            }

            var objFlrs = new ushort[][] { DecodeObjID(iff.Get<ARRY>(3)?.TransposeData), DecodeObjID(iff.Get<ARRY>(103)?.TransposeData) };
            for (int flr = 0; flr < 2; flr++)
            {
                var objs = objFlrs[flr];
                if (objs == null) continue;
                for (int i = 0; i < objs.Length; i++)
                {
                    var obj = objs[i];
                    if (obj != 0)
                    {
                        var x = i % 64;
                        var y = i / 64;
                        MappedObject targ;
                        if (!objm.ObjectData.TryGetValue(obj, out targ)) continue;
                        targ.ArryX = x;
                        targ.ArryY = y;
                        targ.ArryLevel = flr + 1;
                    }
                }
            }

            var content = Content.Content.Get();
            var ents = new List<Tuple<VMEntity, OBJM.MappedObject>>();
            foreach (var obj in objm.ObjectData.Values)
            {
                var res = content.WorldObjects.Get(obj.GUID);
                if (res == null) continue; //failed to load this object
                if (res.OBJ.MasterID != 0)
                {
                    var allObjs = res.Resource.List<OBJD>().Where(x => x.MasterID == res.OBJ.MasterID);
                    var hasLead = allObjs.Any(x => x.MyLeadObject != 0);
                    if ((hasLead && res.OBJ.MyLeadObject != 0) || (!hasLead && res.OBJ.SubIndex == 0))
                    {
                        //this is the object
                        //look for its master

                        //multitile object.. find master objd

                        var master = allObjs.FirstOrDefault(x => x.SubIndex < 0);
                        if (master == null) continue;
                        obj.GUID = master.GUID;
                    }
                    else
                    {
                        continue;
                    }
                }

                //objm parent positioning
                //objects without positions inherit position from the objects in their "parent id".
                MappedObject src = obj;
                while (src.ParentID != 0)
                {
                    if (objm.ObjectData.TryGetValue(src.ParentID, out src))
                    {
                        if (src.ParentID == 0)
                        {
                            obj.ArryX = src.ArryX;
                            obj.ArryY = src.ArryY;
                            obj.ArryLevel = src.ArryLevel;
                        }
                    }
                }

                LotTilePos pos = LotTilePos.OUT_OF_WORLD;
                var dir = (Direction)(1 << obj.Direction);
                var nobj = VM.Context.CreateObjectInstance(obj.GUID, pos, dir).BaseObject;
                if (nobj == null) continue;
                if (obj.ContainerID == 0 && obj.ArryX != 0 && obj.ArryY != 0)
                    nobj.SetPosition(LotTilePos.FromBigTile((short)(obj.ArryX), (short)(obj.ArryY), (sbyte)obj.ArryLevel), dir, VM.Context, VMPlaceRequestFlags.AcceptSlots);

                for (int i = 0; i < nobj.MultitileGroup.Objects.Count; i++) nobj.MultitileGroup.Objects[i].ExecuteEntryPoint(11, VM.Context, true);

                ents.Add(new Tuple<VMEntity, OBJM.MappedObject>(nobj, obj));
            }

            //place objects in slots
            foreach (var ent in ents)
            {
                var obj = ent.Item2;
                if (ent.Item1.Position == LotTilePos.OUT_OF_WORLD && obj.ContainerID != 0 && obj.ArryX != 0 && obj.ArryY != 0)
                {
                    var dir = (Direction)(1 << obj.Direction);
                    ent.Item1.SetPosition(LotTilePos.FromBigTile((short)(obj.ArryX), (short)(obj.ArryY), (sbyte)obj.ArryLevel), dir, VM.Context, VMPlaceRequestFlags.AcceptSlots);
                    /*
                    var target = ents.First(x => x.Item2.ObjectID == obj.ContainerID);
                    target.Item1.PlaceInSlot(ent.Item1, obj.ContainerSlot, true, VM.Context);
                    */
                }
            }

            arch.SignalTerrainRedraw();
            VM.Context.World?.InitBlueprint(Blueprint);
            arch.Tick();
            return this.Blueprint;
        }

        private FloorTile[] RemapFloors(FloorTile[] floors, Dictionary<byte, ushort> dict)
        {
            for (int i=0; i<floors.Length; i++)
            {
                var floor = floors[i];
                if (floor.Pattern != 0)
                {
                    ushort newID;
                    if (dict.TryGetValue((byte)floor.Pattern, out newID))
                    {
                        floors[i].Pattern = newID;
                    }
                }
            }
            return floors;
        }

        private WallTile[] RemapWalls(WallTile[] walls, Dictionary<byte, ushort> dict, Dictionary<byte, ushort> floorDict)
        {
            for (int i = 0; i < walls.Length; i++)
            {
                var wall = walls[i];
                ushort newID;
                if (wall.BottomLeftPattern != 0)
                {
                    if (dict.TryGetValue((byte)wall.BottomLeftPattern, out newID))
                        walls[i].BottomLeftPattern = newID;
                }
                if (wall.BottomRightPattern != 0)
                {
                    if (dict.TryGetValue((byte)wall.BottomRightPattern, out newID))
                        walls[i].BottomRightPattern = newID;
                }
                if ((wall.Segments & WallSegments.AnyDiag) > 0)
                {
                    //diagonally split floors
                    if (wall.TopLeftPattern != 0)
                    {
                        if (floorDict.TryGetValue((byte)wall.TopLeftPattern, out newID))
                            walls[i].TopLeftPattern = newID;
                    }
                    if (wall.TopLeftStyle != 0)
                    {
                        if (floorDict.TryGetValue((byte)wall.TopLeftStyle, out newID))
                            walls[i].TopLeftStyle = newID;
                    }
                }
                else
                {
                    if (wall.TopLeftPattern != 0)
                    {
                        if (dict.TryGetValue((byte)wall.TopLeftPattern, out newID))
                            walls[i].TopLeftPattern = newID;
                    }
                    if (wall.TopRightPattern != 0)
                    {
                        if (dict.TryGetValue((byte)wall.TopRightPattern, out newID))
                            walls[i].TopRightPattern = newID;
                    }
                }
            }
            return walls;
        }

        private Dictionary<byte, ushort> BuildFloorDict (List<WALmEntry> entries)
        {
            var c = Content.Content.Get().WorldFloors.DynamicFloorFromID;
            var result = new Dictionary<byte, ushort>();
            foreach (var entry in entries)
            {
                ushort newID;
                if (c.TryGetValue(new string(entry.Name.TakeWhile(x => x != '.').ToArray()).ToLowerInvariant(), out newID)) {
                    result[entry.ID] = newID;
                }
            }
            return result;
        }

        private Dictionary<byte, ushort> BuildWallDict(List<WALmEntry> entries)
        {
            var c = Content.Content.Get().WorldWalls.DynamicWallFromID;
            var result = new Dictionary<byte, ushort>();
            foreach (var entry in entries)
            {
                ushort newID;
                if (c.TryGetValue(new string(entry.Name.TakeWhile(x => x != '.').ToArray()).ToLowerInvariant(), out newID)) {
                    result[entry.ID] = newID;
                }
            }
            return result;
        }

        private TerrainComponent CreateTerrain(int size)
        {
            var terrain = new TerrainComponent(new Rectangle(1, 1, size - 2, size - 2), Blueprint);
            this.InitWorldComponent(terrain);
            return terrain;
        }

        private void InitWorldComponent(WorldComponent component)
        {
            component.Initialize(this.World.State.Device, this.World.State);
        }

        public FloorTile[] DecodeFloors(byte[] data)
        {
            //todo: flrm map
            return data.Select(x => new FloorTile { Pattern = x }).ToArray();
        }

        public FloorTile[] DecodeAdvFloors(byte[] data)
        {
            int j = 0;
            var result = new FloorTile[data.Length / 2];
            for (int i = 0; i < data.Length; i += 2)
            {
                result[j++] = new FloorTile() { Pattern = (ushort)(data[i] | (data[i + 1] << 8)) };
            }
            return result;
        }

        public ushort[] DecodeObjID(byte[] data)
        {
            if (data == null) return null;
            int j = 0;
            var result = new ushort[data.Length / 2];
            for (int i = 0; i < data.Length; i += 2)
            {
                result[j++] = (ushort)(data[i] | (data[i + 1] << 8));
            }
            return result;
        }

        public static HashSet<ushort> ValidWallStyles = new HashSet<ushort>()
        {
            0x2, 0xc, 0xe, 0xd
        };

        public WallTile[] DecodeWalls(byte[] data)
        {
            int j = 0;
            var result = new WallTile[data.Length / 8];
            for (int i=0; i<data.Length; i+=8)
            {
                var tile = new WallTile
                {
                    Segments = (WallSegments)data[i],
                    TopLeftStyle = (ushort)(data[i + 2]),
                    TopRightStyle = (ushort)(data[i + 3]),
                    TopLeftPattern = (ushort)(data[i + 4]),
                    TopRightPattern = (ushort)(data[i + 5]),
                    BottomLeftPattern = (ushort)(data[i + 6]),
                    BottomRightPattern = (ushort)(data[i + 7])
                };

                if ((tile.Segments & WallSegments.AnyDiag) == 0) {
                    if (!ValidWallStyles.Contains(tile.TopLeftStyle)) tile.TopLeftStyle = 1;
                    if (!ValidWallStyles.Contains(tile.TopRightStyle)) tile.TopRightStyle = 1;
                    if ((tile.Segments & WallSegments.TopLeft) == 0) tile.TopLeftStyle = 0;
                    if ((tile.Segments & WallSegments.TopRight) == 0) tile.TopRightStyle = 0;
                }
                result[j++] = tile;
            }
            return result;
        }

        public WallTile[] DecodeAdvWalls(byte[] data)
        {
            int j = 0;
            var result = new WallTile[data.Length / 14];
            for (int i = 0; i < data.Length; i += 14)
            {
                var tile = new WallTile
                {
                    Segments = (WallSegments)data[i],
                    TopLeftStyle = (ushort)(data[i + 2] | (data[i + 3] << 8)),
                    TopRightStyle = (ushort)(data[i + 4] | (data[i + 5] << 8)),
                    TopLeftPattern = (ushort)(data[i + 6] | (data[i + 7] << 8)),
                    TopRightPattern = (ushort)(data[i + 8] | (data[i + 9] << 8)),
                    BottomLeftPattern = (ushort)(data[i + 10] | (data[i + 11] << 8)),
                    BottomRightPattern = (ushort)(data[i + 12] | (data[i + 13] << 8))
                };

                if ((tile.Segments & WallSegments.AnyDiag) == 0)
                {
                    if (!ValidWallStyles.Contains(tile.TopLeftStyle)) tile.TopLeftStyle = 1;
                    if (!ValidWallStyles.Contains(tile.TopRightStyle)) tile.TopRightStyle = 1;
                    if ((tile.Segments & WallSegments.TopLeft) == 0) tile.TopLeftStyle = 0;
                    if ((tile.Segments & WallSegments.TopRight) == 0) tile.TopRightStyle = 0;
                }
                result[j++] = tile;
            }
            return result;
        }
    }
}
