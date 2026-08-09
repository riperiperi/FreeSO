using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FSO.HouseGen
{
    /// <summary>
    /// HouseLayout -> blueprint XML, deterministically. No AI in this path.
    ///
    /// Wall storage convention, established by running blueprints through the VM rather than by
    /// reading the enum. A wall is identified by the LOW edge of a tile: a wall between (x-1,y)
    /// and (x,y) is TopLeft on (x,y); between (x,y-1) and (x,y) it is TopRight on (x,y). A room's
    /// east and south walls are therefore written on the row/column just OUTSIDE the room.
    ///
    /// Each wall is then written TWICE — once on that low edge, once as the mirrored high-edge bit
    /// on the neighbour (BottomRight/BottomLeft). Enclosure does not need the mirror; doors do.
    /// See AddWall.
    ///
    /// examples/house-one-room.xml carries a comment claiming BottomRight/BottomLeft encode east
    /// and south walls. They do not — they are the same walls recorded from the other side.
    /// </summary>
    public static class BlueprintWriter
    {
        private const int TopLeft = 1;   // edge toward -x
        private const int TopRight = 2;  // edge toward -y
        private const int BottomRight = 4; // edge toward +x — the mirror of the +x neighbour's TopLeft
        private const int BottomLeft = 8;  // edge toward +y — the mirror of the +y neighbour's TopRight

        /// Smallest room dimension we will emit. Below this a room reads as a corridor
        /// artifact rather than a room, and doors have nowhere to go.
        public const int MinRoomDimension = 2;

        public static string Write(HouseLayout layout)
        {
            Validate(layout);

            var floors = new List<(int Level, int X, int Y, int Value)>();
            // (level,x,y) -> OR-ed segment bits. Shared walls between adjacent rooms collapse
            // here automatically: both rooms name the same tile and the same bit.
            var walls = new Dictionary<(int Level, int X, int Y), int>();

            foreach (var room in layout.Rooms)
            {
                int x0 = room.X, y0 = room.Y;
                int x1 = room.X + room.Width - 1, y1 = room.Y + room.Height - 1;

                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                        floors.Add((room.Level, x, y, room.Floor));

                for (int y = y0; y <= y1; y++)
                {
                    AddWall(walls, room.Level, x0, y, TopLeft);      // west
                    AddWall(walls, room.Level, x1 + 1, y, TopLeft);  // east
                }
                for (int x = x0; x <= x1; x++)
                {
                    AddWall(walls, room.Level, x, y0, TopRight);      // north
                    AddWall(walls, room.Level, x, y1 + 1, TopRight);  // south
                }
            }

            // Checked here rather than in Validate because it needs the computed walls: a door in
            // open air cuts nothing, places an object nobody can see the point of, and reports no
            // error anywhere in the engine.
            foreach (var door in layout.Doors)
            {
                int bit = IsWestEdge(door.Edge) ? TopLeft : TopRight;
                if (!walls.TryGetValue((door.Level, door.X, door.Y), out var segs) || (segs & bit) == 0)
                    throw new ArgumentException(
                        $"Door at ({door.X},{door.Y}) edge \"{door.Edge}\" on level {door.Level} has no wall to cut. " +
                        $"A door goes on the tile whose {(IsWestEdge(door.Edge) ? "west" : "north")} edge carries the wall — " +
                        $"for a room's east or south wall that is the tile just outside the room.");
            }

            floors.Sort((a, b) => a.Level != b.Level ? a.Level - b.Level
                                : a.Y != b.Y ? a.Y - b.Y : a.X - b.X);

            var wallKeys = new List<(int Level, int X, int Y)>(walls.Keys);
            wallKeys.Sort((a, b) => a.Level != b.Level ? a.Level - b.Level
                                  : a.Y != b.Y ? a.Y - b.Y : a.X - b.X);

            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n");
            sb.Append("<house xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\n");
            sb.Append("  <size>").Append(layout.Size).Append("</size>\n");
            sb.Append("  <category>0</category>\n");
            sb.Append("  <world>\n");

            sb.Append("    <floors>\n");
            foreach (var f in floors)
                sb.Append(string.Format(CultureInfo.InvariantCulture,
                    "      <floor level=\"{0}\" x=\"{1}\" y=\"{2}\" value=\"{3}\" />\n",
                    f.Level, f.X, f.Y, f.Value));
            sb.Append("    </floors>\n");

            sb.Append("    <walls>\n");
            foreach (var k in wallKeys)
                sb.Append(string.Format(CultureInfo.InvariantCulture,
                    "      <wall level=\"{0}\" x=\"{1}\" y=\"{2}\" segments=\"{3}\" placement=\"0\" " +
                    "tls=\"1\" trs=\"1\" tlp=\"2\" trp=\"2\" brp=\"2\" blp=\"2\" />\n",
                    k.Level, k.X, k.Y, walls[k]));
            sb.Append("    </walls>\n");

            sb.Append("    <pools />\n");
            sb.Append("  </world>\n");

            if (layout.Doors.Count == 0)
            {
                sb.Append("  <objects />\n");
            }
            else
            {
                sb.Append("  <objects>\n");
                foreach (var door in layout.Doors)
                {
                    // Levels are NOT consistent across this format. VMWorldActivator adds +1 to
                    // floor and wall levels, but takes an object's level as-is AND skips
                    // SetPosition entirely when it is 0 — a level="0" object silently stays out
                    // of world. So author levels stay 0-based everywhere in HouseLayout and get
                    // converted here, once.
                    int objectLevel = door.Level + 1;
                    // The door group straddles the wall: it anchors on the tile BEFORE the wall
                    // tile, so sub-object 0 sits west/north of the wall and sub-object 1 lands on
                    // the wall tile itself. Anchoring on the wall tile puts the group one boundary
                    // too far east/south and it silently refuses to place.
                    bool west = IsWestEdge(door.Edge);
                    int anchorX = west ? door.X - 1 : door.X;
                    int anchorY = west ? door.Y : door.Y - 1;
                    int dir = west ? 6 : 0; // 6 = WEST, 0 = NORTH
                    sb.Append(string.Format(CultureInfo.InvariantCulture,
                        "    <object guid=\"{0}\" level=\"{1}\" x=\"{2}\" y=\"{3}\" dir=\"{4}\" group=\"0\" />\n",
                        door.Guid, objectLevel, anchorX, anchorY, dir));
                }
                sb.Append("  </objects>\n");
            }

            sb.Append("  <sounds />\n");
            sb.Append("</house>\n");
            return sb.ToString();
        }

        /// <summary>
        /// Records a wall on a tile's low edge AND its mirror on the neighbour's high edge.
        ///
        /// Enclosure only needs the low edge — A1 proved that with a single-sided file. Doors need
        /// both, and the reason is worth writing down because the failure is silent. A door is a
        /// two-tile group straddling the boundary between its tiles: sub-object 0 requires a wall
        /// on its BottomRight (0x4), sub-object 1 requires one on its TopLeft (0x1) — the same
        /// physical wall, read from each side. VMArchitecture.GetWall returns raw stored data and
        /// never merges neighbours, so the BottomRight bit has to actually be stored on the west
        /// tile or the group fails WallChangeValid with MustBeAgainstWall, and VMWorldActivator
        /// discards that error and leaves the door out of world with nothing logged.
        /// </summary>
        private static void AddWall(Dictionary<(int, int, int), int> walls, int level, int x, int y, int bit)
        {
            AddSegment(walls, level, x, y, bit);
            if (bit == TopLeft) AddSegment(walls, level, x - 1, y, BottomRight);
            else AddSegment(walls, level, x, y - 1, BottomLeft);
        }

        private static bool IsWestEdge(string edge)
        {
            if (string.Equals(edge, "west", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(edge, "north", StringComparison.OrdinalIgnoreCase)) return false;
            throw new ArgumentException($"Door edge must be \"west\" or \"north\", not \"{edge}\".");
        }

        private static void AddSegment(Dictionary<(int, int, int), int> walls, int level, int x, int y, int bit)
        {
            var key = (level, x, y);
            walls[key] = walls.TryGetValue(key, out var existing) ? existing | bit : bit;
        }

        private static void Validate(HouseLayout layout)
        {
            if (layout.Rooms.Count == 0)
                throw new ArgumentException("Layout has no rooms.");

            foreach (var room in layout.Rooms)
            {
                var who = string.IsNullOrEmpty(room.Name) ? $"room at ({room.X},{room.Y})" : room.Name;

                if (room.Width < MinRoomDimension || room.Height < MinRoomDimension)
                    throw new ArgumentException(
                        $"{who} is {room.Width}x{room.Height}; minimum is {MinRoomDimension}x{MinRoomDimension} tiles " +
                        $"({MinRoomDimension} metres). Features smaller than this cannot be represented on the tile grid.");

                // East and south walls are written one tile beyond the room, so the room itself
                // has to stop two short of the grid edge.
                if (room.X < 1 || room.Y < 1 ||
                    room.X + room.Width > layout.Size - 1 || room.Y + room.Height > layout.Size - 1)
                    throw new ArgumentException(
                        $"{who} spans ({room.X},{room.Y})-({room.X + room.Width - 1},{room.Y + room.Height - 1}), " +
                        $"outside the usable grid of 1..{layout.Size - 2}.");
            }

            for (int i = 0; i < layout.Rooms.Count; i++)
                for (int j = i + 1; j < layout.Rooms.Count; j++)
                {
                    var a = layout.Rooms[i];
                    var b = layout.Rooms[j];
                    if (a.Level != b.Level) continue;
                    bool overlaps = a.X < b.X + b.Width && b.X < a.X + a.Width
                                 && a.Y < b.Y + b.Height && b.Y < a.Y + a.Height;
                    if (overlaps)
                        throw new ArgumentException($"Rooms '{a.Name}' and '{b.Name}' overlap on level {a.Level}.");
                }
        }
    }
}
