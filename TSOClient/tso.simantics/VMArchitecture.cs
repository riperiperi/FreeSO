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
        public List<VMArchitectureCommand> Commands;
        public RoomMap Rooms;
        public BlueprintRoom[] RoomData;
        public event ArchitectureEvent WallsChanged;

        public delegate void ArchitectureEvent(VMArchitecture caller);

        private Blueprint WorldUI;

        private bool RealMode;
        private bool WallsDirty;
        private bool RedrawWalls;

        public VMArchitecture(int width, int height, Blueprint blueprint)
        {
            this.Width = width;
            this.Height = height;

            var numTiles = width * height;
            this.WallsAt = new List<int>();
            this.Walls = new WallTile[numTiles];
            this.VisWalls = new WallTile[numTiles];

            this.Rooms = new RoomMap();
            this.RoomData = new BlueprintRoom[0];
            this.WorldUI = blueprint;

            this.Commands = new List<VMArchitectureCommand>();

            WallsDirty = true;
            RealMode = true;
            RedrawWalls = true;
        }

        public void SignalRedraw()
        {
            RedrawWalls = true;
        }

        public void RegenRoomMap()
        {
            var count = Rooms.GenerateMap(Walls, Width, Height, 1); //todo, do for multiple floors
            RoomData = new BlueprintRoom[count];
        }

        public void Tick()
        {
            RedrawWalls = true;
            //Commands.Clear();
            //Commands.Add(new VMArchitectureCommand { Type = VMArchitectureCommandType.WALL_LINE, level = 1, pattern = 0, style = 1, x = 1, y = 1+new Random().Next(32), x2 = 20, y2 = 0 });

            if (WallsDirty)
            {
                RegenRoomMap();
                if (WallsChanged != null) WallsChanged(this);
            }
            if (RedrawWalls)
            {
                //reupload walls to blueprint. 
                if (Commands.Count == 0) 
                {
                    //direct copy, no changes to make
                    WorldUI.Walls = Walls;
                    WorldUI.WallsAt = WallsAt;
                }
                else
                {
                    RealMode = false;
                    var oldWalls = Walls;
                    var oldWallsAt = WallsAt;

                    Array.Copy(Walls, VisWalls, Walls.Length);
                    Walls = VisWalls;
                    WallsAt = new List<int>(WallsAt);
                    RunCommands(Commands);

                    WorldUI.Walls = Walls;
                    WorldUI.WallsAt = WallsAt;

                    Walls = oldWalls;
                    WallsAt = oldWallsAt;
                }
                WorldUI.SignalWallChange();
            }
            WallsDirty = false;
            RedrawWalls = false;
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
            RedrawWalls = true;
        }

        public WallTile GetWall(short tileX, short tileY, sbyte level)
        {
            return Walls[GetOffset(tileX, tileY)];
        }

        private ushort GetOffset(int tileX, int tileY)
        {
            return (ushort)((tileY * Width) + tileX);
        }

    }
}
