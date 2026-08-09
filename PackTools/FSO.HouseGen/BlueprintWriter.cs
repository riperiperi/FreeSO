using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FSO.HouseGen
{
    /// <summary>
    /// HouseLayout -> blueprint XML, deterministically. No AI in this path.
    ///
    /// Wall storage convention, taken from the file A1 proved rather than from the enum:
    /// every wall lives on the LOW edge of a tile. WallSegments has four adjacent bits
    /// (TopLeft=1, TopRight=2, BottomRight=4, BottomLeft=8), but blueprints only ever author
    /// the first two — the other two are what WallComponent produces when it rotates the view.
    /// So a wall between (x-1,y) and (x,y) is TopLeft on (x,y), and a wall between (x,y-1) and
    /// (x,y) is TopRight on (x,y). A room's east and south walls are therefore written on the
    /// tile row/column just OUTSIDE the room.
    ///
    /// examples/house-one-room.xml carries a comment claiming BottomRight/BottomLeft are used
    /// for east/south. The data in that same file does not do that, and the data is what loads.
    /// </summary>
    public static class BlueprintWriter
    {
        private const int TopLeft = 1;   // edge toward -x
        private const int TopRight = 2;  // edge toward -y

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
                    AddSegment(walls, room.Level, x0, y, TopLeft);      // west
                    AddSegment(walls, room.Level, x1 + 1, y, TopLeft);  // east
                }
                for (int x = x0; x <= x1; x++)
                {
                    AddSegment(walls, room.Level, x, y0, TopRight);      // north
                    AddSegment(walls, room.Level, x, y1 + 1, TopRight);  // south
                }
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
            sb.Append("  <objects />\n");
            sb.Append("  <sounds />\n");
            sb.Append("</house>\n");
            return sb.ToString();
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
