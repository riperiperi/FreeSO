using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using FSO.LotView.Model;
using FSO.SimAntics.Model.Routing;

namespace FSO.SimAntics.Model
{
    /// <summary>
    /// Generates and manages a room map for a specified level.
    /// </summary>
    public class VMRoomMap
    {
        public uint[] Map;
        public int Width;
        public int Height;

        private ushort ExpectedTile;

        /// <summary>
        /// Generates the room map for the specified walls array.
        /// </summary>
        public void GenerateMap(WallTile[] Walls, FloorTile[] Floors, int width, int height, List<VMRoom> rooms, sbyte floor, VMContext context) //for first floor gen, curRoom should be 1. For floors above, it should be the last genmap result
        {
            Map = new uint[width*height]; //although 0 is the base of the array, room 1 is known to simantics as room 0.
            //values of 0 indicate the room has not been chosen in that location yet.

            bool noFloorBad = (rooms.Count > 1);

            this.Width = width;
            this.Height = height;

            //flood fill recursively. Each time choose find and choose the first "0" as the base.
            //The first recursion (outside) cannot fill into diagonals.
            bool remaining = true;
            bool outside = true;
            int i = 0;
            while (remaining)
            {
                var spread = new Stack<SpreadItem>();
                remaining = false;
                while (i < Map.Length)
                {
                    remaining = true;

                    //get the wall on this tile. 
                    var wall = Walls[i];
                    var segs = wall.Segments;
                    var room = (uint)rooms.Count;
                    if (Map[i] == 0 && (segs & (WallSegments.AnyDiag)) == 0)
                    {
                        //normal tile - no diagonal
                        ExpectedTile = Floors[i].Pattern;
                        Map[i] = room | (room << 16);
                        spread.Push(new SpreadItem(new Point(i % width, i / width), WallSegments.AnyAdj));
                        break;
                    }
                    else if ((Map[i] & 0xFFFF) == 0)
                    {
                        //start spreading from this side of the diagonal
                        WallSegments validSpread;
                        if ((segs & WallSegments.HorizontalDiag) > 0)
                        {
                            validSpread = WallSegments.TopLeft | WallSegments.TopRight;
                            ExpectedTile = wall.TopLeftStyle;
                            Map[i] |= 0x80000000;
                        }
                        else
                        {
                            validSpread = WallSegments.TopRight | WallSegments.BottomRight;
                            ExpectedTile = wall.TopLeftPattern;
                        }

                        Map[i] |= room;
                        spread.Push(new SpreadItem(new Point(i % width, i / width), validSpread));
                        break;
                    }
                    else if ((Map[i] & 0x7FFF0000) == 0)
                    {
                        //start spreading the other side
                        WallSegments validSpread;
                        if ((segs & WallSegments.HorizontalDiag) > 0)
                        {
                            validSpread = WallSegments.BottomLeft | WallSegments.BottomRight;
                            ExpectedTile = wall.TopLeftPattern;
                            Map[i] |= 0x80000000;
                        }
                        else
                        {
                            validSpread = WallSegments.TopLeft | WallSegments.BottomLeft;
                            ExpectedTile = wall.TopLeftStyle;
                        }
                        Map[i] |= (room << 16);
                        spread.Push(new SpreadItem(new Point(i % width, i / width), validSpread));
                        i++;
                        break;
                    }
                    else remaining = false;
                    i++;
                }

                if (remaining)
                {
                    int rminX = spread.Peek().Pt.X;
                    int rmaxX = rminX;
                    int rminY = spread.Peek().Pt.Y;
                    int rmaxY = rminY;
                    var wallObs = new List<VMObstacle>();
                    var wallLines = (VM.UseWorld)?new VMWallLineBuilder():null;
                    var fenceLines = (VM.UseWorld) ? new VMWallLineBuilder() : null;
                    var adjRooms = new HashSet<ushort>();
                    ushort area = 0;
                    while (spread.Count > 0)
                    {
                        area++;
                        var itemT = spread.Pop();
                        var item = itemT.Pt;

                        if (item.X > rmaxX) rmaxX = item.X;
                        if (item.X < rminX) rminX = item.X;
                        if (item.Y > rmaxY) rmaxY = item.Y;
                        if (item.Y < rminY) rminY = item.Y;

                        var plusX = (item.X+1)%width;
                        var minX = (item.X + width - 1) % width;
                        var plusY = (item.Y+1)%height;
                        var minY = (item.Y + height - 1) % height;

                        var mainWalls = Walls[item.X + item.Y * width];

                        int obsX = item.X << 4;
                        int obsY = item.Y << 4;

                        if ((mainWalls.Segments & WallSegments.HorizontalDiag) > 0)
                        {
                            wallObs.Add(new VMObstacle(obsX + 11, obsY - 1, obsX + 17, obsY + 5));
                            wallObs.Add(new VMObstacle(obsX + 7, obsY + 3, obsX + 13, obsY + 9));
                            wallObs.Add(new VMObstacle(obsX + 3, obsY + 7, obsX + 9, obsY + 13));
                            wallObs.Add(new VMObstacle(obsX - 1, obsY + 11, obsX + 5, obsY + 17));
                            if (mainWalls.TopRightStyle == 1) wallLines?.AddLine(obsX, obsY, 2);
                            else fenceLines?.AddLine(obsX, obsY, 2);
                        }

                        if ((mainWalls.Segments & WallSegments.VerticalDiag) > 0)
                        {
                            wallObs.Add(new VMObstacle(obsX - 1, obsY - 1, obsX + 5, obsY + 5));
                            wallObs.Add(new VMObstacle(obsX + 3, obsY + 3, obsX + 9, obsY + 9));
                            wallObs.Add(new VMObstacle(obsX + 7, obsY + 7, obsX + 13, obsY + 13));
                            wallObs.Add(new VMObstacle(obsX + 11, obsY + 11, obsX + 17, obsY + 17));
                            if (mainWalls.TopRightStyle == 1) wallLines?.AddLine(obsX, obsY, 3);
                            else fenceLines?.AddLine(obsX, obsY, 3);
                        }

                        var PXWalls = Walls[plusX + item.Y * width];
                        var PYWalls = Walls[item.X + plusY * width];

                        if ((mainWalls.Segments & WallSegments.TopLeft) > 0)
                        {
                            if (!mainWalls.TopLeftDoor) wallObs.Add(new VMObstacle(obsX - 3, obsY - 3, obsX + 6, obsY + 19));
                            if (mainWalls.TopLeftThick) wallLines?.AddLine(obsX, obsY, 0);
                            else fenceLines?.AddLine(obsX, obsY, 0);
                        }
                        if ((mainWalls.Segments & WallSegments.TopRight) > 0)
                        {
                            if (!mainWalls.TopRightDoor) wallObs.Add(new VMObstacle(obsX - 3, obsY - 3, obsX + 19, obsY + 6));
                            if (mainWalls.TopRightThick) wallLines?.AddLine(obsX, obsY, 1);
                            else fenceLines?.AddLine(obsX, obsY, 1);
                        }
                        if ((mainWalls.Segments & WallSegments.BottomLeft) > 0)
                        {
                            if (!PYWalls.TopRightDoor) wallObs.Add(new VMObstacle(obsX - 3, obsY + 13, obsX + 19, obsY + 19));
                            if (PYWalls.TopRightThick) wallLines?.AddLine(obsX, obsY+16, 1);
                            else fenceLines?.AddLine(obsX, obsY + 16, 1);
                        }
                        if ((mainWalls.Segments & WallSegments.BottomRight) > 0)
                        {
                            if (!PXWalls.TopLeftDoor) wallObs.Add(new VMObstacle(obsX + 13, obsY - 3, obsX + 19, obsY + 19));
                            if (PXWalls.TopLeftThick) wallLines?.AddLine(obsX + 16, obsY, 0);
                            else fenceLines?.AddLine(obsX + 16, obsY, 0);
                        }

                        bool segAllow = ((PXWalls.Segments & WallSegments.TopLeft) == 0);
                        if ((segAllow || PXWalls.TopLeftStyle != 1) && ((itemT.Dir & WallSegments.BottomRight) > 0))
                            SpreadOnto(Walls, Floors, plusX, item.Y, 0, Map, width, height, spread, (ushort)rooms.Count, ExpectedTile, noFloorBad, adjRooms, !segAllow);

                        segAllow = ((mainWalls.Segments & WallSegments.TopLeft) == 0);
                        if ((segAllow || mainWalls.TopLeftStyle != 1) && ((itemT.Dir & WallSegments.TopLeft) > 0))
                            SpreadOnto(Walls, Floors, minX, item.Y, 2, Map, width, height, spread, (ushort)rooms.Count, ExpectedTile, noFloorBad, adjRooms, !segAllow);

                        segAllow = ((PYWalls.Segments & WallSegments.TopRight) == 0);
                        if ((segAllow || PYWalls.TopRightStyle != 1) && ((itemT.Dir & WallSegments.BottomLeft) > 0))
                            SpreadOnto(Walls, Floors, item.X, plusY, 1, Map, width, height, spread, (ushort)rooms.Count, ExpectedTile, noFloorBad, adjRooms, !segAllow);

                        segAllow = ((mainWalls.Segments & WallSegments.TopRight) == 0);
                        if ((segAllow || mainWalls.TopRightStyle != 1) && ((itemT.Dir & WallSegments.TopRight) > 0))
                            SpreadOnto(Walls, Floors, item.X, minY, 3, Map, width, height, spread, (ushort)rooms.Count, ExpectedTile, noFloorBad, adjRooms, !segAllow);
                    }

                    var bounds = new Rectangle(rminX, rminY, (rmaxX - rminX) + 1, (rmaxY - rminY) + 1);
                    var roomObs = GenerateRoomObs((ushort)rooms.Count, (sbyte)(floor+1), bounds, context);
                    OptimizeObstacles(wallObs);
                    OptimizeObstacles(roomObs);
                    var supportRooms = new List<ushort>();

                    ushort myRoom = (ushort)rooms.Count;
                    ushort minRoom = myRoom;
                    bool deferredLit = false;
                    foreach (var roomN in adjRooms)
                    {
                        var room = rooms[roomN];
                        if (minRoom > room.LightBaseRoom)
                        {
                            deferredLit = true;
                            minRoom = room.LightBaseRoom;
                        }
                        room.AdjRooms.Add(myRoom);
                        if (outside) MakeOutside(rooms, room);
                        else if (room.IsOutside) outside = true;
                    }

                    if (deferredLit)
                    {
                        rooms[minRoom].SupportRooms.Add(myRoom);
                    }
                    else supportRooms.Add(myRoom);

                    if (VM.UseWorld && minRoom != rooms.Count)
                    {
                        rooms[minRoom].WallLines.AddRange(wallLines.Lines);
                        rooms[minRoom].FenceLines.AddRange(fenceLines.Lines);
                    }

                    rooms.Add(new VMRoom
                    {
                        IsOutside = outside,
                        IsPool = ExpectedTile > 65533,
                        Bounds = bounds,
                        WallObs = wallObs,
                        RoomObs = roomObs,
                        RoutingObstacles = VMObstacleSet.RoughBalanced(wallObs.Concat(roomObs).ToList()),
                        WallLines = (deferredLit) ? null : wallLines?.Lines,
                        FenceLines = (deferredLit) ? null : fenceLines?.Lines,
                        AdjRooms = adjRooms,
                        RoomID = myRoom,
                        LightBaseRoom = minRoom,
                        Area = area,
                        Floor = floor,
                        SupportRooms = supportRooms
                    });

                    foreach (var roomN in adjRooms)
                    {
                        var room = rooms[roomN];
                        TrySwitchBaseRoom(rooms, room, minRoom);
                    }

                    outside = false;
                }
            }
        }

        public void TrySwitchBaseRoom(List<VMRoom> rooms, VMRoom room, ushort newBase)
        {
            var newBaseR = rooms[newBase];
            if (room.LightBaseRoom != newBase)
            {
                //we joined in a new minimum room. cascade changes.
                var oldBaseR = rooms[room.LightBaseRoom];
                //move all rooms that think this is their base room to the new base
                foreach (var oRoom in oldBaseR.SupportRooms)
                {
                    newBaseR.SupportRooms.Add(oRoom);
                    var roomChange = rooms[oRoom];
                    roomChange.LightBaseRoom = newBase;
                    rooms[oRoom] = roomChange;
                }
                oldBaseR.SupportRooms.Clear();
                //and move all the wall lines (for advanced lighting)
                newBaseR.WallLines?.AddRange(oldBaseR.WallLines);
                oldBaseR.WallLines = null;
                //note there's a small bug here where wall lines can be on the same location
                //if both sides of the wall are found on adjacent rooms
                //...so a wall may have double the shadow strength. (most important for outdoors shadows)
            }
        }

        public void MakeOutside(List<VMRoom> rooms, VMRoom room)
        {
            room.IsOutside = true;
            rooms[room.RoomID] = room;
            var toVisit = new Queue<ushort>(room.AdjRooms);
            while (toVisit.Count > 0)
            {
                var visit = toVisit.Dequeue();
                if (visit >= rooms.Count) continue;
                var roomElem = rooms[visit];
                if (roomElem.IsOutside) continue;
                roomElem.IsOutside = true;
                rooms[visit] = roomElem;
                foreach (var adj in roomElem.AdjRooms)
                    toVisit.Enqueue(adj);
            }
        }

        private static void SpreadOnto(WallTile[] walls, FloorTile[] floors, int x, int y, int inDir, uint[] map, int width, int height, 
            Stack<SpreadItem> spread, ushort room, ushort expectedTile, bool noAir, HashSet<ushort> adjRoom, bool forceAdj)
        {
            var index = x + y * width;
            var wall = walls[index];
            var floor = floors[index].Pattern;

            bool hasExpectation = (expectedTile == 0 && noAir) || (expectedTile > 65533);

            ushort targRoom;
            uint roomApply;
            ushort targFloor;
            WallSegments validSpread;
            if ((wall.Segments & WallSegments.HorizontalDiag) > 0)
            {
                if (inDir < 2)
                {
                    //top (bottom right pattern)
                    validSpread = WallSegments.TopLeft | WallSegments.TopRight;
                    targFloor = wall.TopLeftStyle;
                    targRoom = (ushort)map[index];
                    roomApply = (room) | 0x80000000;
                }
                else
                {
                    //bottom (bottom left pattern)
                    validSpread = WallSegments.BottomLeft | WallSegments.BottomRight;
                    targFloor = wall.TopLeftPattern;
                    targRoom = (ushort)((map[index] >> 16) & 0x7FFF);
                    roomApply = (uint)(room<< 16) | 0x80000000;
                }
            }
            else if ((wall.Segments & WallSegments.VerticalDiag) > 0)
            {
                if (inDir > 0 && inDir < 3)
                {
                    //right
                    validSpread = WallSegments.TopRight | WallSegments.BottomRight;
                    targFloor = wall.TopLeftPattern;
                    targRoom = (ushort)map[index];
                    roomApply = room;
                }
                else
                {
                    //left
                    validSpread = WallSegments.TopLeft | WallSegments.BottomLeft;
                    targFloor = wall.TopLeftStyle;
                    targRoom = (ushort)((map[index] >> 16)&0x7FFF);
                    roomApply = (uint)(room << 16);
                }
            }
            else
            {
                validSpread = WallSegments.AnyAdj;
                targRoom = (ushort)map[index];
                targFloor = floor;
                roomApply = (uint)(room | (room << 16));
            }

            bool cantSpread = (hasExpectation && expectedTile != targFloor) || (!hasExpectation && ((targFloor == 0 && noAir) || targFloor > 65533));

            if (forceAdj || targRoom > 0 || cantSpread)
            {
                //cannot spread onto this (sub)tile - we've either been here before or a non-wall is segmenting the space
                //eg. a pool, air, fence
                if (targRoom != room && targRoom > 0)
                {
                    //a non-wall is segmenting the space. targRoom contains the room we are adjacent to.
                    //this needs to be added to the other room too, as it will have failed to spread onto room 0, not ours.
                    adjRoom.Add(targRoom);
                }
                return;
            }
            else
            {
                map[index] |= roomApply;
            }

            spread.Push(new SpreadItem(new Point(x, y), validSpread));
        }

        public List<VMObstacle> GenerateRoomObs(ushort room, sbyte level, Rectangle bounds, VMContext context)
        {
            var result = new List<VMObstacle>();
            if (room == 0)
            {
                bounds = new Rectangle(1, 1, Width - 2, Height - 2);
            }
            var x1 = Math.Max(0, bounds.X - 1);
            var x2 = Math.Min(Width, bounds.Right + 1);
            var y1 = Math.Max(0, bounds.Y - 1);
            var y2 = Math.Min(Height, bounds.Bottom + 1);

            for (int y = y1; y < y2; y++)
            {
                VMObstacle next = null;
                for (int x = x1; x < x2; x++)
                {
                    uint tRoom = Map[x + y * Width];
                    if ((ushort)tRoom != room && ((tRoom>>16)&0x7FFF) != room)
                    {
                        //is there a door on this tile?
                        var door = (context.ObjectQueries.GetObjectsAt(LotTilePos.FromBigTile((short)x, (short)y, level))?.FirstOrDefault(
                            o => ((VMEntityFlags2)(o.GetValue(VMStackObjectVariable.FlagField2)) & VMEntityFlags2.ArchitectualDoor) > 0)
                        );
                        if (door != null)
                        {
                            //ok... is is a portal to this room? block all sides that are not a portal to this room
                            var otherSide = door.MultitileGroup.Objects.FirstOrDefault(o => context.GetObjectRoom(o) == room && o.Portal);
                            if (otherSide != null)
                            {
                                //make a hole for this door
                                if (next != null) next = null;
                                // note: the sims 1 stops here. this creates issues where sims can walk through doors in some circumstance
                                // eg. two doors back to back into the same room. The sim will not perform a room route to the middle room, they will just walk through the door.
                                // like, through it. This also works for pools but some additional rules prevent you from doing anything too silly.
                                // we want to create 1 unit thick walls blocking each non-portal side.
                                
                                // todo: fix for this
                                continue;
                            }
                        }

                        if (next != null) next.x2 += 16;
                        else
                        {
                            next = new VMObstacle((x << 4) - 3, (y << 4) - 3, (x << 4) + 19, (y << 4) + 19);
                            result.Add(next);
                        }
                    }
                    else
                    {
                        if (next != null) next = null;
                    }
                }
            }
            OptimizeObstacles(result);
            return result;
        }

        public void OptimizeObstacles(List<VMObstacle> obs)
        {
            // collapses axis aligned collision bounds into single rectangles (useful for walls and rooms)
            // i have totally verified that this is reliable and optimal in every percievable way
            // O(n^2) probably
            // if you find a better way please replace, this is just sort of helpful.

            var changed = new LinkedList<VMObstacle>(obs);

            var item = changed.First;
            while (item != null)
            {
                var r1 = item.Value;
                //ok, so for each rectangle we need to find rectangles we can join with.
                var targ = changed.First;
                while (targ != null)
                {
                    var r2 = targ.Value;
                    var nextT = targ.Next;
                    if (r1 != r2)
                    {
                        if (r1.x1 == r2.x1 && r1.x2 == r2.x2 && !(r1.y1 > r2.y2 || r1.y2 < r2.y1))
                        {
                            //intersects... combine em
                            r1.y1 = Math.Min(r1.y1, r2.y1);
                            r1.y2 = Math.Max(r1.y2, r2.y2);
                            changed.Remove(targ);
                            obs.Remove(r2);
                        }
                        else if (r1.y1 == r2.y1 && r1.y2 == r2.y2 && !(r1.x1 > r2.x2 || r1.x2 < r2.x1))
                        {
                            r1.x1 = Math.Min(r1.x1, r2.x1);
                            r1.x2 = Math.Max(r1.x2, r2.x2);
                            changed.Remove(targ);
                            obs.Remove(r2);
                        }
                    }

                    targ = nextT;
                }
                item = item.Next;
            }
        }

        public void PrintRoomMap()
        {
            int off = 0;
            for (int y = 0; y < Height; y++)
            {
                StringBuilder sb = new StringBuilder();
                for (int x = 0; x < Width; x++)
                {
                    sb.Append(Map[off++]);
                    if (Map[off - 1] < 10) sb.Append(" ");
                    sb.Append(" ");
                }
                System.Diagnostics.Debug.WriteLine(sb.ToString());
            }
        }
    }

    public struct SpreadItem
    {
        public Point Pt;
        public WallSegments Dir;

        public SpreadItem (Point pt, WallSegments dir)
        {
            Pt = pt;
            Dir = dir;
        }
    }
}
