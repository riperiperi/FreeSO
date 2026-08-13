using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using FSO.LotView.Model;

namespace FSO_BrowserClient
{
    /// <summary>
    /// Loads a blueprint XML's architecture (floors + walls) straight into a LotView
    /// Blueprint — no VM. Mirrors the arch subset of VMWorldActivator.LoadFromXML:
    /// SetFloor/SetWall become direct array writes, then the change signals drive
    /// FloorGeom/WCRC regeneration on the next PreDraw.
    /// </summary>
    public static class BlueprintArchLoader
    {
        public static void Load(Blueprint bp, string houseXml)
        {
            XmlHouseData model;
            var serializer = new XmlSerializer(typeof(XmlHouseData));
            using (var reader = new StringReader(houseXml))
                model = (XmlHouseData)serializer.Deserialize(reader);

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
