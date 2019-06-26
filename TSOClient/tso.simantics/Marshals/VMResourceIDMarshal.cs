using FSO.Content;
using FSO.LotView.Model;
using FSO.SimAntics.NetPlay.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using FSO.SimAntics.Model;

namespace FSO.SimAntics.Marshals
{
    /// <summary>
    /// The FreeSO equivalent of FLRm and WALm, as well as a simple name substitution for the roof id.
    /// </summary>
    public class VMResourceIDMarshal : VMSerializable
    {
        public Dictionary<ushort, string> WallNamesByID = new Dictionary<ushort, string>();
        public Dictionary<ushort, string> FloorNamesByID = new Dictionary<ushort, string>();
        public string Roof = "";

        public VMResourceIDMarshal()
        {
        }

        /// <summary>
        /// Generate a resource ID map from the specified lot.
        /// </summary>
        /// <param name="vm">The target lot VM.</param>
        public VMResourceIDMarshal(VM vm)
        {
            var arch = vm.Context.Architecture;
            var content = Content.Content.Get();
            var floorids = new HashSet<ushort>();
            foreach (var level in arch.Floors)
            {
                foreach (var floor in level) floorids.Add(floor.Pattern);
            }
            var wallids = new HashSet<ushort>();
            foreach (var level in arch.Walls)
            {
                foreach (var wall in level)
                {
                    //this can get stupid. diagonal walls can also contain floors, obviously.
                    //styles don't need remapping since they are limited to the base game ones, and custom temp wall styles are restored from objects.

                    if ((wall.Segments & LotView.Model.WallSegments.AnyDiag) > 0)
                    {
                        if (wall.TopLeftPattern != 0) floorids.Add(wall.TopLeftPattern);
                        if (wall.TopLeftStyle != 0) floorids.Add(wall.TopLeftStyle);

                        if (wall.BottomLeftPattern != 0) wallids.Add(wall.BottomLeftPattern);
                        if (wall.BottomRightPattern != 0) wallids.Add(wall.BottomRightPattern);
                    }
                    else if ((wall.Segments & LotView.Model.WallSegments.AnyAdj) > 0)
                    {
                        if (wall.BottomLeftPattern != 0) wallids.Add(wall.BottomLeftPattern);
                        if (wall.BottomRightPattern != 0) wallids.Add(wall.BottomRightPattern);
                        if (wall.TopLeftPattern != 0) wallids.Add(wall.TopLeftPattern);
                        if (wall.TopRightPattern != 0) wallids.Add(wall.TopRightPattern);
                    }

                }
            }

            foreach (var x in floorids) {
                FloorReference fref;
                if (content.WorldFloors.Entries.TryGetValue(x, out fref) && fref.FileName != "global")
                {
                    FloorNamesByID.Add(x, new string(fref.FileName.ToLowerInvariant().TakeWhile(y => y != '.').ToArray()));
                }
            }

            foreach (var x in wallids)
            {
                WallReference wref;
                if (content.WorldWalls.Entries.TryGetValue(x, out wref) && wref.FileName != "global")
                {
                    WallNamesByID.Add(x, new string(wref.FileName.ToLowerInvariant().TakeWhile(y => y != '.').ToArray()));
                }
            }

            Roof = content.WorldRoofs.IDToName((int)arch.RoofStyle);
        }

        public void Apply(VM vm)
        {
            var wallMap = BuildDict(WallNamesByID, Content.Content.Get().WorldWalls.DynamicWallFromID, vm.LoadErrors, VMLoadErrorCode.MISSING_WALL);
            var floorMap = BuildDict(FloorNamesByID, Content.Content.Get().WorldFloors.DynamicFloorFromID, vm.LoadErrors, VMLoadErrorCode.MISSING_FLOOR);

            var arch = vm.Context.Architecture;
            foreach (var floors in arch.Floors)
            {
                for (int i = 0; i < floors.Length; i++)
                {
                    var floor = floors[i];
                    if (floor.Pattern != 0)
                    {
                        ushort newID;
                        if (floorMap.TryGetValue(floor.Pattern, out newID))
                        {
                            floors[i].Pattern = newID;
                        }
                    }
                }
            }

            foreach (var walls in arch.Walls)
            {
                for (int i = 0; i < walls.Length; i++)
                {
                    var wall = walls[i];
                    ushort newID;
                    if (wall.BottomLeftPattern != 0)
                    {
                        if (wallMap.TryGetValue(wall.BottomLeftPattern, out newID))
                            walls[i].BottomLeftPattern = newID;
                    }
                    if (wall.BottomRightPattern != 0)
                    {
                        if (wallMap.TryGetValue(wall.BottomRightPattern, out newID))
                            walls[i].BottomRightPattern = newID;
                    }
                    if ((wall.Segments & WallSegments.AnyDiag) > 0)
                    {
                        //diagonally split floors
                        if (wall.TopLeftPattern != 0)
                        {
                            if (floorMap.TryGetValue(wall.TopLeftPattern, out newID))
                                walls[i].TopLeftPattern = newID;
                        }
                        if (wall.TopLeftStyle != 0)
                        {
                            if (floorMap.TryGetValue(wall.TopLeftStyle, out newID))
                                walls[i].TopLeftStyle = newID;
                        }
                    }
                    else
                    {
                        if (wall.TopLeftPattern != 0)
                        {
                            if (wallMap.TryGetValue(wall.TopLeftPattern, out newID))
                                walls[i].TopLeftPattern = newID;
                        }
                        if (wall.TopRightPattern != 0)
                        {
                            if (wallMap.TryGetValue(wall.TopRightPattern, out newID))
                                walls[i].TopRightPattern = newID;
                        }
                    }
                }
            }

            arch.RoofStyle = (uint)Content.Content.Get().WorldRoofs.NameToID(Roof);
            if (arch.RoofStyle == int.MaxValue)
            {
                vm.LoadErrors.Add(new VMLoadError(VMLoadErrorCode.MISSING_ROOF, Roof));
                arch.RoofStyle = 0;
            }
            arch.SignalAllDirty();
        }

        private Dictionary<ushort, ushort> BuildDict(Dictionary<ushort, string> oldIDToName, Dictionary<string, ushort> nameToID, List<VMLoadError> errors, VMLoadErrorCode code)
        {
            var result = new Dictionary<ushort, ushort>();
            foreach (var entry in oldIDToName)
            {
                ushort newID;
                if (nameToID.TryGetValue(entry.Value, out newID))
                {
                    result[entry.Key] = newID;
                } else
                {
                    errors.Add(new VMLoadError(code, entry.Value));
                }
            }
            return result;
        }

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write(WallNamesByID.Count);
            foreach (var item in WallNamesByID)
            {
                writer.Write(item.Key);
                writer.Write(item.Value);
            }

            writer.Write(FloorNamesByID.Count);
            foreach (var item in FloorNamesByID)
            {
                writer.Write(item.Key);
                writer.Write(item.Value);
            }

            writer.Write(Roof);
        }

        public void Deserialize(BinaryReader reader)
        {
            var wallCount = reader.ReadInt32();
            for (int i=0; i<wallCount; i++)
            {
                var id = reader.ReadUInt16();
                var name = reader.ReadString();
                WallNamesByID[id] = name;
            }

            var floorCount = reader.ReadInt32();
            for (int i = 0; i < floorCount; i++)
            {
                var id = reader.ReadUInt16();
                var name = reader.ReadString();
                FloorNamesByID[id] = name;
            }

            Roof = reader.ReadString();
        }
    }
}
