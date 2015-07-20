using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.world.model;
using TSO.Simantics.model;
using TSO.Simantics.utils;

namespace TSO.Simantics
{
    public class VMArchitecture
    {
        public int Width;
        public int Height;

        public WallTile[] Walls;
        public WallTile[] VisWalls;
        public List<int> WallsAt;

        public FloorTile[] Floors;
        public FloorTile[] VisFloors;

        public List<VMArchitectureCommand> Commands;

        public RoomMap Rooms;
        public BlueprintRoom[] RoomData;
        public event ArchitectureEvent WallsChanged;

        public delegate void ArchitectureEvent(VMArchitecture caller);

        private Blueprint WorldUI;

        private bool RealMode;

        private bool WallsDirty;
        private bool FloorsDirty;

        private bool Redraw;

        public VMArchitecture(int width, int height, Blueprint blueprint)
        {
            this.Width = width;
            this.Height = height;

            var numTiles = width * height;
            this.WallsAt = new List<int>();
            this.Walls = new WallTile[numTiles];
            this.VisWalls = new WallTile[numTiles];

            this.Floors = new FloorTile[numTiles];
            this.VisFloors = new FloorTile[numTiles];

            this.Rooms = new RoomMap();
            this.RoomData = new BlueprintRoom[0];
            this.WorldUI = blueprint;

            this.Commands = new List<VMArchitectureCommand>();
            this.Commands = new List<VMArchitectureCommand>();

            WallsDirty = true;
            FloorsDirty = true;
            RealMode = true;
            Redraw = true;
        }

        public void SignalRedraw()
        {
            Redraw = true;
        }

        public void RegenRoomMap()
        {
            var count = Rooms.GenerateMap(Walls, Width, Height, 1); //todo, do for multiple floors
            RoomData = new BlueprintRoom[count];
        }

        public void Tick()
        { 

            if (WallsDirty)
            {
                RegenRoomMap();
                if (WallsChanged != null) WallsChanged(this);
            }
            if (Redraw)
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

                    Array.Copy(Floors, VisFloors, Floors.Length);
                    Floors = VisFloors;

                    Array.Copy(Walls, VisWalls, Walls.Length);
                    Walls = VisWalls;
                    WallsAt = new List<int>(WallsAt);
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
                        VMArchitectureTools.DrawWall(this, new Point(com.x, com.y), com.x2, com.y2, com.pattern, com.style, com.level);
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
            Walls[off] = wall;
            WallsAt.Remove(off);
            if (wall.Segments > 0) WallsAt.Add(off);
            else
            {
                wall.ObjSetTLStyle = 0;
                wall.ObjSetTRStyle = 0;
                wall.OccupiedWalls = 0;
                wall.TopLeftDoor = false;
                wall.TopRightDoor = false;
                wall.TopRightStyle = 0;
                wall.TopLeftStyle = 0;
            }

            if (RealMode) WallsDirty = true;
            Redraw = true;
        }

        public WallTile GetWall(short tileX, short tileY, sbyte level)
        {
            return Walls[GetOffset(tileX, tileY)];
        }

        public FloorTile GetFloor(short tileX, short tileY, sbyte level)
        {
            var offset = GetOffset(tileX, tileY);
            return Floors[offset];
        }

        public void SetFloor(short tileX, short tileY, sbyte level, FloorTile floor)
        {
            var offset = GetOffset(tileX, tileY);
            Floors[offset] = floor;

            if (RealMode) FloorsDirty = true;
            Redraw = true;
        }

        private ushort GetOffset(int tileX, int tileY)
        {
            return (ushort)((tileY * Width) + tileX);
        }

    }
}
