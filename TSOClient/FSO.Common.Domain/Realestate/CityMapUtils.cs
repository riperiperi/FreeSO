using FSO.Content.Model;
using FSO.Server.Protocol.Electron.Model.CityEditCommands;
using Microsoft.Xna.Framework;
using System.Runtime.CompilerServices;

namespace FSO.Common.Domain.Realestate
{
    public enum RoadSegs : byte
    {
        BottomLeft = 1,
        BottomRight = 2,
        TopRight = 4,
        TopLeft = 8,

        Left = 16,
        Bottom = 32,
        Right = 64,
        Top = 128,

        AllCorners = Bottom | Right | Top | Left
    }

    public static class CityMapUtils
    {
        private static readonly Point[] WLStartOff = {
            
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

            new(0, 0),
            new(0, 0),
            new(-1, 0),
            new(0, -1),
        };

        private static readonly RoadSegs[] WLMainSeg =
        {
            RoadSegs.TopLeft,
            RoadSegs.BottomLeft,
            RoadSegs.TopLeft,
            RoadSegs.BottomLeft,
        };

        private static readonly Point[] WLSubOff =
        {
            new(0, -1),
            new(-1, 0),
            new(0, -1),
            new(-1, 0),
        };

        private static readonly RoadSegs[] WLSubSeg =
        {
            RoadSegs.BottomRight,
            RoadSegs.TopRight,
            RoadSegs.BottomRight,
            RoadSegs.TopRight,
        };


        private static readonly Point[] WLStep =
        {
            new(1, 0),
            new(0, 1),
            new(-1, 0),
            new(0, -1),
        };

        private static readonly ((RoadSegs line, RoadSegs corner), (RoadSegs line2, RoadSegs corner2))[] AdjEdgeToCorner =
        [
            ( // positive x
                (RoadSegs.BottomRight, RoadSegs.Right),
                (RoadSegs.TopLeft, RoadSegs.Top)
            ),
            ( // positive y
                (RoadSegs.BottomLeft, RoadSegs.Bottom),
                (RoadSegs.TopRight, RoadSegs.Right)
            ),
            ( // negative x
                (RoadSegs.TopLeft, RoadSegs.Left),
                (RoadSegs.BottomRight, RoadSegs.Bottom)
            ),
            ( // negative y
                (RoadSegs.TopRight, RoadSegs.Top),
                (RoadSegs.BottomLeft, RoadSegs.Left)
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

        public static bool ApplyCommand(CityMap map, CityEditBase command, HashSet<uint> reservedTiles = null, HashSet<uint> toUpdate = null, bool forUndo = false)
        {
            return command switch
            {
                CityEditRoad road => ApplyRoad(map, road, reservedTiles, toUpdate),
                CityEditPaint paint => ApplyPaint(map, paint, reservedTiles, toUpdate, forUndo),
                CityEditAltitude alt => ApplyAltitude(map, alt, reservedTiles, toUpdate, forUndo),
                CityEditForest forest => ApplyForest(map, forest), // Forest doesn't update its tiles.
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
            if (pt.X + 1 > maxX) maxX = pt.X + 1;
            if (pt.Y + 1 > maxY) maxY = pt.Y + 1;
        }

        private static Rectangle GetRoadBounds(CityEditRoad road, bool corners)
        {
            bool xDir = (road.Direction % 2) == 0;
            var direction = road.Direction;

            Point step = WLStep[direction];
            Point start = new(road.StartX, road.StartY);
            int length = road.Length;

            Point subOff = WLSubOff[direction]; // Direction to place the sub segment of the wall

            if (corners)
            {
                start -= step;
                length += 2;
            }

            start += WLStartOff[direction];

            Point end = start + new Point(step.X * length, step.Y * length);

            int minX = start.X;
            int minY = start.Y;
            int maxX = start.X + 1;
            int maxY = start.Y + 1;

            UpdateMinMax(start + subOff, ref minX, ref minY, ref maxX, ref maxY);

            UpdateMinMax(end, ref minX, ref minY, ref maxX, ref maxY);
            UpdateMinMax(end + subOff, ref minX, ref minY, ref maxX, ref maxY);

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

            // Finally, do some cleanup for invalid corners

            var roadSegs = (RoadSegs)road;

            if (roadSegs.HasFlag(RoadSegs.BottomRight))
            {
                road &= (byte)~(RoadSegs.Bottom | RoadSegs.Right);
            }

            if (roadSegs.HasFlag(RoadSegs.TopRight))
            {
                road &= (byte)~(RoadSegs.Top | RoadSegs.Right);
            }

            if (roadSegs.HasFlag(RoadSegs.BottomLeft))
            {
                road &= (byte)~(RoadSegs.Bottom | RoadSegs.Left);
            }

            if (roadSegs.HasFlag(RoadSegs.TopLeft))
            {
                road &= (byte)~(RoadSegs.Top | RoadSegs.Left);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint GetMapCoord(Point pos)
        {
            return MapCoordinates.Pack((ushort)pos.X, (ushort)pos.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RegisterUpdate(HashSet<uint> reservedTiles, HashSet<uint> toUpdate, uint id)
        {
            if (reservedTiles.Contains(id))
            {
                toUpdate.Add(id);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RegisterRoadUpdates(HashSet<uint> reservedTiles, HashSet<uint> toUpdate, Point pos)
        {
            // Road modifications only update the lot they're on top of.

            if (reservedTiles != null)
            {
                uint id = GetMapCoord(pos);

                RegisterUpdate(reservedTiles, toUpdate, id);
            }
        }

        public static bool ApplyRoad(CityMap map, CityEditRoad road, HashSet<uint> reservedTiles, HashSet<uint> toUpdate)
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

                // These still trigger updates even when the road isn't updated
                RegisterRoadUpdates(reservedTiles, toUpdate, cornerPos);
                RegisterRoadUpdates(reservedTiles, toUpdate, subPos);

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RegisterTerrainUpdates(HashSet<uint> reservedTiles, HashSet<uint> toUpdate, Point pos, bool forUndo)
        {
            // Terrain modifications update all adjacent tiles, as it could affect the blend colour of the lot.

            if (reservedTiles != null)
            {
                uint id = GetMapCoord(pos);
                uint skip = 1u << 16;

                RegisterUpdate(reservedTiles, toUpdate, id);

                if (!forUndo)
                {
                    RegisterUpdate(reservedTiles, toUpdate, id - 1);
                    RegisterUpdate(reservedTiles, toUpdate, id + 1);

                    RegisterUpdate(reservedTiles, toUpdate, (id - 1) - skip);
                    RegisterUpdate(reservedTiles, toUpdate, id - skip);
                    RegisterUpdate(reservedTiles, toUpdate, id + 1 - skip);

                    RegisterUpdate(reservedTiles, toUpdate, (id - 1) + skip);
                    RegisterUpdate(reservedTiles, toUpdate, id + skip);
                    RegisterUpdate(reservedTiles, toUpdate, id + 1 + skip);
                }
            }
        }

        public static bool ApplyPaint(CityMap map, CityEditPaint paint, HashSet<uint> reservedTiles, HashSet<uint> toUpdate, bool forUndo)
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
                    if (!reserved.IsSet(x, y))
                    {
                        ref var existing = ref aspect[mapIndex];

                        if (value != existing)
                        {
                            anyChanged = true;
                            if (isTerrainType)
                            {
                                RegisterTerrainUpdates(reservedTiles, toUpdate, new Point(x, y), forUndo);

                                if (value == (byte)TerrainType.WATER)
                                {
                                    forestType[mapIndex] = ForestType.NULL;
                                    forestDensity[mapIndex] = 0;
                                }
                            }

                            existing = value;
                        }
                    }

                    mapIndex++;
                    x++;
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
                        ref var existingDensity = ref forestDensity[mapIndex];
                        var newDensity = (byte)Math.Min(Math.Min(maxDensity, intensity[deltaIndex]) * 64, 255);

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

                    deltaIndex++;
                    mapIndex++;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void RegisterAltitudeUpdates(HashSet<uint> reservedTiles, HashSet<uint> toUpdate, Point pos)
        {
            // Altitude modifications happen on the top left vertex of a lot,
            // So they also affect the lots up and to the left (including diagonal

            if (reservedTiles != null)
            {
                uint id = GetMapCoord(pos);
                uint skip = 1u << 16;

                RegisterUpdate(reservedTiles, toUpdate, id - 1);
                RegisterUpdate(reservedTiles, toUpdate, id);

                RegisterUpdate(reservedTiles, toUpdate, (id - 1) - skip);
                RegisterUpdate(reservedTiles, toUpdate, id - skip);
            }
        }

        public static bool ApplyAltitude(CityMap map, CityEditAltitude altEdit, HashSet<uint> reservedTiles, HashSet<uint> toUpdate, bool forUndo)
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
                        if (!reserved.IsSet(x, y))
                        {
                            RegisterAltitudeUpdates(reservedTiles, toUpdate, new Point(x, y));
                            anyData = true;
                            ref var alt = ref altitudes[mapIndex];
                            alt = (byte)Math.Clamp(alt + deltas[deltaIndex], 0, 255);
                        }

                        mapIndex++;
                        deltaIndex++;
                        x++;
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
                    anyData = false;

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

                                ref TerrainType existingType = ref type[mapIndex];

                                TerrainType tileType;

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

                                if (existingType != tileType)
                                {
                                    anyData = true;
                                    RegisterTerrainUpdates(reservedTiles, toUpdate, new Point(x, y), forUndo);
                                    existingType = tileType;
                                }
                            }

                            mapIndex++;
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
