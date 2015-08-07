/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.LotView.Model;
using FSO.SimAntics.Model;
using FSO.SimAntics.Utils;

namespace FSO.SimAntics
{
    public class VMArchitecture
    {
        public int Width;
        public int Height;
        public int Stories = 5;

        //public for quick access and iteration. 
        //Make sure that on modifications you signal so that the render updates.
        public WallTile[][] Walls;
        public WallTile[][] VisWalls;
        public List<int>[] WallsAt;

        public FloorTile[][] Floors;
        public FloorTile[][] VisFloors;

        public bool[][] ObjectSupport;
        public bool[][] Supported;

        public List<VMArchitectureCommand> Commands;

        public RoomMap[] Rooms;
        public List<BlueprintRoom> RoomData;
        public event ArchitectureEvent WallsChanged;

        public VMContext Context; //used for access to objects

        public delegate void ArchitectureEvent(VMArchitecture caller);

        private Blueprint WorldUI;

        private bool RealMode;

        private bool WallsDirty;
        private bool FloorsDirty;

        private bool Redraw;

        public VMArchitecture(int width, int height, Blueprint blueprint, VMContext context)
        {
            this.Context = context;
            this.Width = width;
            this.Height = height;

            var numTiles = width * height;
            this.WallsAt = new List<int>[Stories];
            this.Walls = new WallTile[Stories][];
            this.VisWalls = new WallTile[Stories][];

            this.Floors = new FloorTile[Stories][];
            this.VisFloors = new FloorTile[Stories][];

            this.ObjectSupport = new bool[Stories][]; //true if there's an object support in the specified position
            this.Supported = new bool[Stories-1][]; //no supported array for bottom floor. true if this tile is supported.
            if (blueprint != null) blueprint.Supported = Supported;

            this.Rooms = new RoomMap[Stories];

            for (int i = 0; i < Stories; i++)
            {
                this.WallsAt[i] = new List<int>();
                this.Walls[i] = new WallTile[numTiles];
                this.VisWalls[i] = new WallTile[numTiles];

                this.Floors[i] = new FloorTile[numTiles];
                this.VisFloors[i] = new FloorTile[numTiles];
                this.ObjectSupport[i] = new bool[numTiles];

                if (i<Stories-1) this.Supported[i] = new bool[numTiles];

                this.Rooms[i] = new RoomMap();
            }

            
            this.RoomData = new List<BlueprintRoom>();
            this.WorldUI = blueprint;

            this.Commands = new List<VMArchitectureCommand>();
            this.Commands = new List<VMArchitectureCommand>();

            WallsDirty = true;
            FloorsDirty = true;
            RealMode = true;
            Redraw = true;
        }

        public void SetObjectSupported(short x, short y, sbyte level, bool support)
        {
            ObjectSupport[level - 1][y * Width + x] = support;
            RegenerateSupported(level+1);
        }

        private Point[] SupportSpread =
        {
            new Point(0, 1),
            new Point(1, 0),
            new Point(0, -1),
            new Point(-1, 0)
        };

        public void RegenerateSupported(int level)
        {
            if (level < 2 || level > Stories) return;
            bool[] objSup = ObjectSupport[level - 2];
            bool[] sup = Supported[level - 2];
            FloorTile[] floors = Floors[level - 1];
            RoomMap rooms = Rooms[level - 2];
                
            int offset = 0;
            for (int y=0; y<Height; y++)
            {
                for (int x=0; x<Width; x++)
                {
                    //if we are an object support or are above a room that is not outside, we're supported.
                    if (objSup[offset] || !RoomData[rooms.Map[offset]].IsOutside) sup[offset] = true;
                    else
                    {
                        //if we are a floor tile or are next to the floor tile, do the full 5x5 check.
                        bool step1 = false;
                        for (int i = 0; i < SupportSpread.Length; i++)
                        {
                            int newX = x + SupportSpread[i].X;
                            int newY = y + SupportSpread[i].Y;
                            if (newX < 0 || newX >= Width || newY < 0 || newY >= Height) continue;
                            int newOff = newY * Width + newX;
                            if (floors[newOff].Pattern != 0)
                            {
                                step1 = true;
                                break;
                            }
                        }

                        if (step1)
                        {
                            bool step2 = false;
                            for (int y2 = -2; y2 < 3; y2++)
                            {
                                for (int x2 = -2; x2 < 3; x2++)
                                {
                                    int newX = x + x2;
                                    int newY = y + y2;
                                    if (newX < 0 || newX >= Width || newY < 0 || newY >= Height) continue;
                                    int newOff = newY * Width + newX;
                                    if (!RoomData[rooms.Map[newOff]].IsOutside || (objSup[newOff] && (Math.Abs(x2)<2 && Math.Abs(y2)<2)))
                                    {
                                        step2 = true;
                                        break;
                                    }
                                }
                                if (step2) break;
                            }
                            sup[offset] = step2;
                        }
                        else sup[offset] = false;
                    }
                    offset++;
                }
            }
        }

        public void SignalRedraw()
        {
            Redraw = true;
        }

        public void RegenRoomMap()
        {
            RoomData = new List<BlueprintRoom>();
            RoomData.Add(new BlueprintRoom()); //dummy at index 0
            for (int i=0; i<Stories; i++)
            {
                Rooms[i].GenerateMap(Walls[i], Floors[i], Width, Height, RoomData);
                RegenerateSupported(i + 1);
            }
        }

        public void Tick()
        { 

            if (WallsDirty)
            {
                RegenRoomMap();
                if (WallsChanged != null) WallsChanged(this);
            }

            if (FloorsDirty)
            {
                for (int i = 1; i < Stories; i++)
                    RegenerateSupported(i + 1);
            }
            if (VM.UseWorld && Redraw)
            {
                //reupload walls to blueprint. 
                if (Commands.Count == 0) 
                {
                    //direct copy, no changes to make
                    WorldUI.Walls = Walls;
                    WorldUI.WallsAt = WallsAt;
                    WorldUI.Floors = Floors;
                }
                else
                {
                    RealMode = false;
                    var oldWalls = Walls;
                    var oldWallsAt = WallsAt;

                    var oldFloors = Floors;

                    WallsAt = new List<int>[Stories];
                    for (int i = 0; i < Stories; i++)
                    {         
                        Array.Copy(Floors[i], VisFloors[i], Floors[i].Length);
                        Array.Copy(Walls[i], VisWalls[i], Walls[i].Length);
                        WallsAt[i] = new List<int>(oldWallsAt[i]);
                    }
                    Floors = VisFloors;
                    Walls = VisWalls;
                    RunCommands(Commands);

                    WorldUI.Walls = Walls;
                    WorldUI.WallsAt = WallsAt;
                    WorldUI.Floors = Floors;

                    Floors = oldFloors;
                    Walls = oldWalls;
                    WallsAt = oldWallsAt;
                }
                WorldUI.SignalWallChange();
                WorldUI.SignalFloorChange();
            }

            FloorsDirty = false;
            Redraw = false;
            WallsDirty = false;
            RealMode = true;
        }

        public void RunCommands(List<VMArchitectureCommand> commands)
        {
            for (var i=0; i<commands.Count; i++)
            {
                var com = commands[i];
                switch (com.Type)
                {
                    case VMArchitectureCommandType.WALL_LINE:
                        VMArchitectureTools.DrawWall(this, new Point(com.x, com.y), com.x2, com.y2, com.pattern, com.style, com.level, false);
                        break;
                    case VMArchitectureCommandType.WALL_DELETE:
                        VMArchitectureTools.EraseWall(this, new Point(com.x, com.y), com.x2, com.y2, com.pattern, com.style, com.level);
                        break;
                    case VMArchitectureCommandType.WALL_RECT:
                        VMArchitectureTools.DrawWallRect(this, new Rectangle(com.x, com.y, com.x2, com.y2), com.pattern, com.style, com.level);
                        break;

                    case VMArchitectureCommandType.PATTERN_FILL:
                        VMArchitectureTools.WallPatternFill(this, new Point(com.x, com.y), com.pattern, com.level);
                        break;
                    case VMArchitectureCommandType.PATTERN_DOT:
                        VMArchitectureTools.WallPatternDot(this, new Point(com.x, com.y), com.pattern, com.x2, com.y2, com.level);
                        break;

                    case VMArchitectureCommandType.FLOOR_FILL:
                        VMArchitectureTools.FloorPatternFill(this, new Point(com.x, com.y), com.pattern, com.level);
                        break;

                    case VMArchitectureCommandType.FLOOR_RECT:
                        VMArchitectureTools.FloorPatternRect(this, new Rectangle(com.x, com.y, com.x2, com.y2), com.style, com.pattern, com.level);
                        break;
                }
            }
        }

        public void SetWall(short tileX, short tileY, sbyte level, WallTile wall)
        {
            var off = GetOffset(tileX, tileY);

            WallsAt[level-1].Remove(off);
            if (wall.Segments > 0) {
                Walls[level - 1][off] = wall;
                WallsAt[level - 1].Add(off);
            }
            else
            {
                Walls[level - 1][off] = new WallTile();
            }

            if (RealMode) WallsDirty = true;
            Redraw = true;
        }

        public WallTile GetWall(short tileX, short tileY, sbyte level)
        {
            return Walls[level-1][GetOffset(tileX, tileY)];
        }

        public FloorTile GetFloor(short tileX, short tileY, sbyte level)
        {
            var offset = GetOffset(tileX, tileY);
            return Floors[level-1][offset];
        }

        public bool SetFloor(short tileX, short tileY, sbyte level, FloorTile floor, bool force)
        {
            //returns false on failure
            var offset = GetOffset(tileX, tileY);

            if (!force)
            {
                //first check if we're supported
                if (level > 1 && !Supported[level - 2][offset]) return false;
                //check if objects need/don't need floors
                if (!Context.CheckFloorValid(LotTilePos.FromBigTile((short)tileX, (short)tileY, level), floor)) return false;
            }

            Floors[level-1][offset] = floor;

            if (RealMode) FloorsDirty = true;
            Redraw = true;
            return true;
        }

        public ushort GetOffset(int tileX, int tileY)
        {
            return (ushort)((tileY * Width) + tileX);
        }

    }
}
