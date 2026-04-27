using FSO.Content.Model;
using FSO.Server.Protocol.Electron.Model.CityEditCommands;
using Microsoft.Xna.Framework;
using System.Runtime.CompilerServices;

namespace FSO.Common.Domain.Realestate
{
    public enum RoadSegs : byte
    {
        TopLeft = 1,
        BottomLeft = 2,
        BottomRight = 4,
        TopRight = 8,

        Right = 16,
        Bottom = 32,
        Left = 64,
        Top = 128,

        AllCorners = Bottom | Left | Top | Right
    }

    public static class CityMapUtils
    {
        private static Point[] WLStartOff = {
            
            // Look at this way up <----
            // Starting at % line, going cw. Middle is (0,0), and below it is the tile (0,0)..
            //
            //        /\
            //       /  \ +x
            //      /\  %\
            //     /  \%  \
            //     \  /\  /
            //      \/  \/
            //       \  / +y
            //        \/

            new Point(0, 0),
            new Point(0, 0),
            new Point(-1, 0),
            new Point(0, -1),
        };

        private static RoadSegs[] WLMainSeg =
        {
            RoadSegs.TopRight,
            RoadSegs.TopLeft,
            RoadSegs.TopRight,
            RoadSegs.TopLeft,
        };

        private static Point[] WLSubOff =
        {
            new Point(0, -1),
            new Point(-1, 0),
            new Point(0, -1),
            new Point(-1, 0),
        };

        private static RoadSegs[] WLSubSeg =
        {
            RoadSegs.BottomLeft,
            RoadSegs.BottomRight,
            RoadSegs.BottomLeft,
            RoadSegs.BottomRight,
        };


        private static Point[] WLStep =
        {
            new Point(1, 0),
            new Point(0, 1),
            new Point(-1, 0),
            new Point(0, -1),
        };

        private static ((RoadSegs line, RoadSegs corner), (RoadSegs line2, RoadSegs corner2))[] AdjEdgeToCorner =
        [
            ( // negative x?
                (RoadSegs.BottomLeft, RoadSegs.Left),
                (RoadSegs.TopRight, RoadSegs.Top)
            ),
            ( // positive y
                (RoadSegs.TopLeft, RoadSegs.Bottom),
                (RoadSegs.BottomRight, RoadSegs.Left)
            ),
            ( // positive x?
                (RoadSegs.TopRight, RoadSegs.Right),
                (RoadSegs.BottomLeft, RoadSegs.Bottom)
            ),
            ( // negative y
                (RoadSegs.BottomRight, RoadSegs.Top),
                (RoadSegs.TopLeft, RoadSegs.Right)
            )
        ];

        private const int RandomSeed = 123456789;

        [ThreadStatic]
        private static CityEditBitmap ReservedBitmap;
        private readonly static byte[] Noise;

        static CityMapUtils()
        {
            byte[] noise = new byte[512 * 512];

            var rand = new Random(RandomSeed);

            rand.NextBytes(noise);

            Noise = noise;
        }

        public static byte[] GetRawNoise()
        {
            return Noise;
        }

        public static void GetSpraypaintNoise(byte[] target, uint seed)
        {
            var index = (int)(seed % Noise.Length);

            if (index > 0)
            {
                var sliceSize = Noise.Length - index;
                (Noise.AsSpan(index)).CopyTo(target.AsSpan(0, sliceSize));
                (Noise.AsSpan(0, index)).CopyTo(target.AsSpan(sliceSize));
            }
            else
            {
                Noise.CopyTo(target, 0);
            }
        }

        private static CityEditBitmap GetReservedBitmapBase(CityMap map)
        {
            CityEditBitmap bitmap;
            if (ReservedBitmap == null || ReservedBitmap.Width != map.Width || ReservedBitmap.Height != map.Height)
            {
                bitmap = new CityEditBitmap(map.Width, map.Height);

                ReservedBitmap = bitmap;
            }
            else
            {
                bitmap = ReservedBitmap;
                bitmap.Clear();
            }

            return bitmap;
        }

        private static CityEditBitmap GetReservedBitmap(CityMap map, CityEditBase command)
        {
            CityEditBitmap bitmap = GetReservedBitmapBase(map);
            
            if (command.ReservedLocations != null)
            {
                foreach (var location in command.ReservedLocations)
                {
                    var pt = ReservedLocationToPoint(location);

                    if (pt.X >= 0 && pt.Y >= 0 && pt.X < map.Width && pt.Y < map.Height)
                    {
                        bitmap.Set(pt.X, pt.Y);
                    }
                }
            }

            return bitmap;
        }

        private static CityEditBitmap GetReservedBitmapAlt(CityMap map, CityEditBase command)
        {
            CityEditBitmap bitmap = GetReservedBitmapBase(map);

            if (command.ReservedLocations != null)
            {
                foreach (var location in command.ReservedLocations)
                {
                    var pt = ReservedLocationToPoint(location);

                    if (pt.X >= 0 && pt.Y >= 0 && pt.X < map.Width && pt.Y < map.Height)
                    {
                        bitmap.Set(pt.X, pt.Y);

                        // Reserved tiles lock all four corners.

                        if (pt.X + 1 < map.Width)
                        {
                            bitmap.Set(pt.X + 1, pt.Y);

                            if (pt.Y + 1 < map.Height)
                            {
                                bitmap.Set(pt.X + 1, pt.Y + 1);
                            }
                        }

                        if (pt.Y + 1 < map.Height)
                        {
                            bitmap.Set(pt.X, pt.Y + 1);
                        }
                    }
                }
            }

            return bitmap;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool InBounds(Point tile)
        {
            // This will break when the coords are past short range, but that can't happen with these commands.
            return MapCoordinates.InBounds((ushort)tile.X, (ushort)tile.Y, 0);
        }

        public static bool ValidateCommand(CityMap map, CityEditBase command)
        {
            return command switch
            {
                CityEditRoad road => ValidateRoad(map, road),
                _ => true
            };
        }

        public static bool ApplyCommand(CityMap map, CityEditBase command)
        {
            return command switch
            {
                CityEditRoad road => ApplyRoad(map, road),
                CityEditPaint paint => ApplyPaint(map, paint),
                CityEditAltitude alt => ApplyAltitude(map, alt),
                CityEditForest forest => ApplyForest(map, forest),
                _ => false
            };
        }

        public static Rectangle? GetBounds(CityMap map, CityEditBase command)
        {
            return command switch
            {
                CityEditRoad road => GetRoadBounds(road, true),
                CityEditPaint paint => GetBitmapBounds(map, paint.Bitmap),
                CityEditAltitude alt => GetBitmapBounds(map, alt.Bitmap),
                CityEditForest forest => GetBitmapBounds(map, forest.Bitmap),
                _ => null
            };
        }

        private static Rectangle? GetBitmapBounds(CityMap map, CityEditBitmap bitmap)
        {
            if (bitmap == null)
            {
                return null;
            }

            CityEditBitmap trimmed = (bitmap.Width == map.Width && bitmap.Height == map.Height) ? bitmap.Trim() : bitmap;

            if (trimmed == null)
            {
                return null;
            }

            return new Rectangle(trimmed.X, trimmed.Y, trimmed.Width, trimmed.Height);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Point ReservedLocationToPoint(uint location)
        {
            var coords = MapCoordinates.Unpack(location);

            return new Point(coords.X, coords.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void UpdateMinMax(Point pt, ref int minX, ref int minY, ref int maxX, ref int maxY)
        {
            if (pt.X < minX) minX = pt.X;
            if (pt.Y < minY) minY = pt.Y;
            if (pt.X > maxX) maxX = pt.X;
            if (pt.Y > maxY) maxY = pt.Y;
        }

        private static Rectangle GetRoadBounds(CityEditRoad road, bool corners)
        {
            bool xDir = (road.Direction % 2) == 0;

            Point step = WLStep[road.Direction];
            Point start = new Point(road.StartX, road.StartY);
            int length = road.Length;
            
            if (corners)
            {
                start -= step;
                length += 2;
            }

            Point end = start + new Point(step.X * length, step.Y * length);

            int minX = start.X - 1;
            int minY = start.Y - 1;
            int maxX = start.X + 1;
            int maxY = start.Y + 1;

            UpdateMinMax(end - new Point(1), ref minX, ref minY, ref maxX, ref maxY);
            UpdateMinMax(end + new Point(1), ref minX, ref minY, ref maxX, ref maxY);

            return new Rectangle(minX, minY, maxX - minX, maxY - minY);
        }

        public static bool ValidateRoad(CityMap map, CityEditRoad road)
        {
            // Does the bound go outside the map?
            var innerBounds = GetRoadBounds(road, false);

            if (innerBounds.X < 0 || innerBounds.Y < 0 || innerBounds.Bottom > map.Height || innerBounds.Right > map.Width)
            {
                return false;
            }


            // Is any reserved tile overlapping the road bounds?
            var bounds = GetRoadBounds(road, true);

            if (road.ReservedLocations != null)
            {
                foreach (uint location in road.ReservedLocations)
                {
                    if (bounds.Contains(ReservedLocationToPoint(location)))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetOffset(CityMap map, Point tile)
        {
            return tile.Y * map.Width + tile.X;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ApplyCornerRule(in (RoadSegs line, RoadSegs corner) rule, in byte adjRoad, ref byte road)
        {
            byte lineB = (byte)rule.line;

            if ((road & lineB) == 0 && (adjRoad & lineB) != 0)
            {
                road |= (byte)rule.corner;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RecalculateCorner(CityMap map, byte[] roads, Point tile)
        {
            // Corner presence is dictated by the presence of a road segment on an adjacent tile
            // and its subsequent absence on this tile. Only segments perpendicular to the tile edge create corners.

            if (!InBounds(tile)) return;

            ref byte road = ref roads[GetOffset(map, tile)];

            // Clear corners, add them on top as we find them.
            road &= (byte)~RoadSegs.AllCorners;

            for (int i = 0; i < 4; i++)
            {
                var adj = tile + WLStep[i];
                if (InBounds(adj))
                {
                    byte adjRoad = roads[GetOffset(map, adj)];

                    var (rule1, rule2) = AdjEdgeToCorner[i];

                    ApplyCornerRule(in rule1, in adjRoad, ref road);
                    ApplyCornerRule(in rule2, in adjRoad, ref road);
                }
            }
        }

        public static bool ApplyRoad(CityMap map, CityEditRoad road)
        {
            byte[] roads = map.GetRawRoads();

            // Step 1: place edges

            int direction = road.Direction;
            Console.WriteLine(direction);
            int length = road.Length;
            Point startPos = new Point(road.StartX, road.StartY);

            Point step = WLStep[direction]; // Direction to move each length unit.
            Point subOff = WLSubOff[direction]; // Direction to place the sub segment of the wall

            byte mainSeg = (byte)WLMainSeg[direction];
            byte subSeg = (byte)WLSubSeg[direction];

            bool erase = road.Delete;

            Point pos = startPos + WLStartOff[direction];

            for (int i = 0; i < length; i++)
            {
                Point subPos = pos + subOff;

                if (erase)
                {
                    roads[GetOffset(map, pos)] &= (byte)~mainSeg;
                    roads[GetOffset(map, subPos)] &= (byte)~subSeg;
                }
                else
                {
                    roads[GetOffset(map, pos)] |= mainSeg;
                    roads[GetOffset(map, subPos)] |= subSeg;
                }

                pos += step;
            }

            // Step 2: recalculate corners (extends 1 further out into the road direction on both sides)
            int cornerLength = length + 2;
            Point cornerPos = startPos + WLStartOff[direction] - step;

            for (int i = 0; i < cornerLength; i++)
            {
                Point subPos = cornerPos + subOff;

                RecalculateCorner(map, roads, cornerPos);
                RecalculateCorner(map, roads, subPos);

                cornerPos += step;
            }

            map.SetDirty(CityMapAspects.Road);

            return true;
        }

        private static Span<byte> GetPaintAspect(CityMap map, CityEditPaintType type)
        {
            return type switch
            {
                CityEditPaintType.TerrainType => System.Runtime.InteropServices.MemoryMarshal.Cast<TerrainType, byte>(map.TerrainType),
                CityEditPaintType.ForestType => System.Runtime.InteropServices.MemoryMarshal.Cast<ForestType, byte>(map.ForestTypeData),
                CityEditPaintType.ForestDensity => map.ForestDensityData,
                _ => null
            };
        }

        private static CityMapAspects GetPaintDirtyAspect(CityEditPaintType type)
        {
            return type switch
            {
                CityEditPaintType.TerrainType => CityMapAspects.TerrainType,
                CityEditPaintType.ForestType => CityMapAspects.Forest,
                CityEditPaintType.ForestDensity => CityMapAspects.Forest,
                _ => CityMapAspects.None
            };
        }

        public static bool ApplyPaint(CityMap map, CityEditPaint paint)
        {
            var reserved = GetReservedBitmap(map, paint);
            var bitmap = paint.Bitmap;
            var value = paint.Value;
            Span<byte> aspect = GetPaintAspect(map, paint.Type);

            bool isTerrainType = paint.Type == CityEditPaintType.TerrainType;
            ForestType[] forestType = map.GetRawForestType();
            byte[] forestDensity = map.GetRawForestDensity();

            bool anyChanged = false;
            foreach (var line in bitmap.GetSetLines())
            {
                int x = line.x + bitmap.X;
                int y = line.y + bitmap.Y;
                int mapIndex = (y * map.Width) + x;

                for (int i = 0; i < line.count; i++)
                {
                    if (!reserved.IsSet(x++, y))
                    {
                        anyChanged = true;
                        if (isTerrainType && value == (byte)TerrainType.WATER)
                        {
                            forestType[mapIndex] = ForestType.NULL;
                            forestDensity[mapIndex] = 0;
                        }

                        aspect[mapIndex++] = value;
                    }
                }
            }

            if (anyChanged)
            {
                map.SetDirty(GetPaintDirtyAspect(paint.Type));
            }

            return true;
        }

        public static bool ApplyForest(CityMap map, CityEditForest paint)
        {
            var reserved = GetReservedBitmap(map, paint);
            var erasing = paint.Erasing;
            var bitmap = paint.Bitmap;
            var intensity = paint.Intensities;
            var newType = (ForestType)paint.ForestType;

            var forestType = map.ForestTypeData;
            var forestDensity = map.ForestDensityData;
            var terrainType = map.TerrainType;

            byte maxDensity = 4;

            bool anyChanged = false;
            foreach (var line in bitmap.GetSetLines())
            {
                int deltaIndex = (line.y * bitmap.Width) + line.x;

                int x = line.x + bitmap.X;
                int y = line.y + bitmap.Y;
                int mapIndex = (y * map.Width) + x;

                for (int i = 0; i < line.count; i++)
                {
                    if (!reserved.IsSet(x++, y))
                    {
                        ref var existingTerrain = ref terrainType[mapIndex];
                        ref var existingType = ref forestType[mapIndex];
                        ref var existingDensity = ref forestDensity[mapIndex++];
                        var newDensity = (byte)Math.Min(Math.Min(maxDensity, intensity[deltaIndex++]) * 64, 255);

                        if (erasing)
                        {
                            anyChanged = true;
                            if (newDensity >= existingDensity)
                            {
                                existingDensity = 0;
                                existingType = ForestType.NULL;
                            }
                            else
                            {
                                existingDensity -= newDensity;
                            }
                        }
                        else
                        {
                            if (newDensity >= existingDensity && existingTerrain != TerrainType.WATER)
                            {
                                anyChanged = true;
                                existingDensity = newDensity;
                                existingType = newType;
                            }
                        }
                    }
                }
            }

            if (anyChanged)
            {
                map.SetDirty(CityMapAspects.Forest);
            }

            return true;
        }

        private static bool OverThreshold(int value, int min, int blend, int index)
        {
            if (value >= min)
            {
                if (value < min + blend)
                {
                    // Use the noise to determine if the threshold is met.
                    var tileNoise = Noise[index];
                    var pct = ((value - min) * 255) / blend;

                    return pct > tileNoise;
                }
                else
                {
                    return true;
                }
            }

            return false;
        }

        public static bool ApplyAltitude(CityMap map, CityEditAltitude altEdit)
        {
            var reserved = GetReservedBitmapAlt(map, altEdit);
            var bitmap = altEdit.Bitmap;
            var deltas = altEdit.AltitudeDeltas;
            var auto = altEdit.AutoTerrainType;
            byte[] altitudes = map.GetRawElevation();

            if (bitmap != null)
            {
                bool anyData = false;
                int height = bitmap.Height;
                foreach (var line in bitmap.GetSetLines())
                {
                    int deltaIndex = (line.y * bitmap.Width) + line.x;

                    int x = line.x + bitmap.X;
                    int y = line.y + bitmap.Y;
                    int mapIndex = (y * map.Width) + x;

                    for (int i = 0; i < line.count; i++)
                    {
                        if (!reserved.IsSet(x++, y))
                        {
                            anyData = true;
                            ref var alt = ref altitudes[mapIndex++];
                            alt = (byte)Math.Clamp(alt + deltas[deltaIndex++], 0, 255);
                        }
                    }
                }

                if (anyData)
                {
                    map.SetDirty(CityMapAspects.Elevation);
                }

                if (auto)
                {
                    int AltScale = 4;

                    // Any modified tile is changed to non-water.
                    // Starting with sand, at each minimum height we start selecting another tile.
                    int AutoGrassMin = 2 * AltScale - 2;
                    int AutoRockMin = 100 * AltScale;
                    int AutoSnowMin = 190 * AltScale;

                    // The blend region introduces some dithering when transitioning between terrain type regions.
                    // For example, after AutoRockMin we gradually introduce more rock until AutoRockMin + AutoRockBlend, where it becomes all rock.
                    int AutoGrassBlend = 6;
                    int AutoRockBlend = 50 * AltScale;
                    int AutoSnowBlend = 20 * AltScale;

                    // When a tile is too steep, it automatically becomes rock.
                    int AutoRockSteepness = 8;
                    int AutoRockSteepnessBlend = 2;

                    TerrainType[] type = map.GetRawTerrain();

                    foreach (var line in bitmap.GetSetLines())
                    {
                        int deltaIndex = (line.y * bitmap.Width) + line.x;

                        int x = line.x + bitmap.X;
                        int y = line.y + bitmap.Y;
                        int mapIndex = (y * map.Width) + x;

                        int lineX = line.x;
                        int nextLineY = line.y + 1;
                        bool hasNextLine = line.y + 1 < height;

                        for (int i = 0; i < line.count; i++)
                        {
                            if (i < line.count - 1 && hasNextLine && bitmap.IsSet(lineX + 1, nextLineY) && !reserved.IsSet(x, y))
                            {
                                var alt1 = altitudes[mapIndex];
                                var alt2 = altitudes[mapIndex + map.Width];
                                var alt3 = altitudes[mapIndex + 1];
                                var alt4 = altitudes[mapIndex + 1 + map.Width];

                                var avg4 = alt1 + alt2 + alt3 + alt4;
                                var min = Math.Min(alt1, Math.Min(alt2, Math.Min(alt3, alt4)));
                                var max = Math.Max(alt1, Math.Max(alt2, Math.Max(alt3, alt4)));

                                var delta = max - min;

                                ref TerrainType tileType = ref type[mapIndex];

                                if (OverThreshold(delta, AutoRockSteepness, AutoRockSteepnessBlend, mapIndex))
                                {
                                    tileType = TerrainType.ROCK;
                                }
                                else
                                {
                                    if (OverThreshold(avg4, AutoGrassMin, AutoGrassBlend, mapIndex))
                                    {
                                        avg4 += delta * 8;
                                        if (OverThreshold(avg4, AutoRockMin, AutoRockBlend, mapIndex))
                                        {
                                            if (OverThreshold(avg4, AutoSnowMin, AutoSnowBlend, mapIndex))
                                            {
                                                tileType = TerrainType.SNOW;
                                            }
                                            else
                                            {
                                                tileType = TerrainType.ROCK;
                                            }
                                        }
                                        else
                                        {
                                            tileType = TerrainType.GRASS;
                                        }
                                    }
                                    else
                                    {
                                        tileType = TerrainType.SAND;
                                    }
                                }

                                mapIndex++;
                            }

                            lineX++;
                            x++;
                        }
                    }

                    if (anyData)
                    {
                        map.SetDirty(CityMapAspects.TerrainType);
                    }
                }
            }

            return true;
        }
    }
}
