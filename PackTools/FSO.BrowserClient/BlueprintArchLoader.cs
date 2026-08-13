using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using FSO.LotView.Model;

namespace FSO_BrowserClient
{
    /// <summary>
    /// Loads a blueprint XML's architecture (floors + walls) straight into a LotView
    /// Blueprint — no VM. Mirrors the arch subset of VMWorldActivator.LoadFromXML:
    /// SetFloor/SetWall become direct array writes, then the change signals drive
    /// FloorGeom/WCRC regeneration on the next PreDraw.
    /// Parses with XDocument, not XmlSerializer: the reflection serializer fails
    /// under WASM publish (XmlConstructorInaccessible after ILStrip), and this
    /// format is 2 element shapes + attributes.
    /// </summary>
    public static class BlueprintArchLoader
    {
        public static void Load(Blueprint bp, string houseXml)
        {
            var model = ParseHouse(houseXml);

            var w = bp.Width;
            var h = bp.Height;

            // WallComponentRC.TileIndoors dereferences RoomMap; the Blueprint ctor
            // leaves the per-story arrays null.
            for (int i = 0; i < bp.RoomMap.Length; i++)
                bp.RoomMap[i] = bp.RoomMap[i] ?? new uint[w * h];
            if (bp.Rooms == null || bp.Rooms.Count == 0)
                bp.Rooms = new List<Room> { new Room { IsOutside = true } };

            foreach (var floor in model.World.Floors)
            {
                var level = floor.Level; // 0-based in XML
                if (level < 0 || level >= bp.Stories) continue;
                if (floor.X < 0 || floor.X >= w || floor.Y < 0 || floor.Y >= h) continue;
                bp.Floors[level][floor.Y * w + floor.X] = new FloorTile { Pattern = (ushort)floor.Value };
            }

            foreach (var wall in model.World.Walls)
            {
                var level = wall.Level;
                if (level < 0 || level >= bp.Stories) continue;
                if (wall.X < 0 || wall.X >= w || wall.Y < 0 || wall.Y >= h) continue;
                var off = (ushort)(wall.Y * w + wall.X);
                bp.Walls[level][off] = new WallTile
                {
                    Segments = wall.Segments,
                    TopLeftPattern = (ushort)wall.TopLeftPattern,
                    TopRightPattern = (ushort)wall.TopRightPattern,
                    BottomLeftPattern = (ushort)wall.BottomLeftPattern,
                    BottomRightPattern = (ushort)wall.BottomRightPattern,
                    TopLeftStyle = (ushort)wall.LeftStyle,
                    TopRightStyle = (ushort)wall.RightStyle,
                };
                bp.WallsAt[level].Add(off);
            }

            bp.SignalFloorChange();
            bp.SignalRoomChange();
            bp.SignalWallChange();
        }

        /// <summary>
        /// Attribute-by-attribute parse into the same model classes VMWorldActivator
        /// consumes, so the two loaders stay comparable field-for-field.
        /// </summary>
        public static XmlHouseData ParseHouse(string houseXml)
        {
            var doc = XDocument.Parse(houseXml);
            var house = doc.Root ?? throw new FormatException("no root element");
            var world = house.Element("world") ?? throw new FormatException("no <world>");

            int A(XElement e, string name) =>
                int.TryParse((string)e.Attribute(name), NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out var v) ? v : 0;

            return new XmlHouseData
            {
                Size = int.TryParse((string)house.Element("size"), out var size) ? size : 0,
                World = new XmlHouseDataWorld
                {
                    Floors = (world.Element("floors")?.Elements("floor") ?? Enumerable.Empty<XElement>())
                        .Select(f => new XmlHouseDataFloor
                        {
                            Level = A(f, "level"),
                            X = (short)A(f, "x"),
                            Y = (short)A(f, "y"),
                            Value = A(f, "value"),
                        }).ToList(),
                    Walls = (world.Element("walls")?.Elements("wall") ?? Enumerable.Empty<XElement>())
                        .Select(w => new XmlHouseDataWall
                        {
                            Level = A(w, "level"),
                            X = A(w, "x"),
                            Y = A(w, "y"),
                            _Segments = A(w, "segments"),
                            Placement = A(w, "placement"),
                            LeftStyle = A(w, "tls"),
                            RightStyle = A(w, "trs"),
                            TopLeftPattern = A(w, "tlp"),
                            TopRightPattern = A(w, "trp"),
                            BottomRightPattern = A(w, "brp"),
                            BottomLeftPattern = A(w, "blp"),
                        }).ToList(),
                    Pools = new List<XmlHouseDataPool>(),
                },
                Objects = (house.Element("objects")?.Elements("object") ?? Enumerable.Empty<XElement>())
                    .Select(o => new XmlHouseDataObject
                    {
                        GUID = ((string)o.Attribute("guid") ?? "0").Replace("0x", ""),
                        Level = A(o, "level"),
                        X = A(o, "x"),
                        Y = A(o, "y"),
                        Dir = A(o, "dir"),
                        Group = A(o, "group"),
                    }).ToList(),
            };
        }

        public static Microsoft.Xna.Framework.Vector2 WallCentroid(Blueprint bp)
        {
            long sx = 0, sy = 0; int n = 0;
            foreach (var off in bp.WallsAt[0])
            {
                sx += off % bp.Width;
                sy += off / bp.Width;
                n++;
            }
            if (n == 0) return new Microsoft.Xna.Framework.Vector2(bp.Width / 2f, bp.Height / 2f);
            return new Microsoft.Xna.Framework.Vector2(sx / (float)n, sy / (float)n);
        }
    }
}
