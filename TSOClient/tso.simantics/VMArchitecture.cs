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
using FSO.SimAntics.Marshals;
using FSO.SimAntics.NetPlay.Model;
using FSO.Content;

namespace FSO.SimAntics
{
    public class VMArchitecture
    {
        public int Width;
        public int Height;
        public int Stories = 5;

        public int LastTestCost;

        public bool DisableClip;
        public Rectangle BuildableArea;
        public int BuildableFloors;

        //public for quick access and iteration. 
        //Make sure that on modifications you signal so that the render updates.
        public WallTile[][] Walls;
        public WallTile[][] VisWalls;
        public List<int>[] WallsAt;

        public FloorTile[][] Floors;
        public FloorTile[][] VisFloors;

        public VMArchitectureTerrain Terrain;

        public bool[][] ObjectSupport;
        public bool[][] Supported;

        public List<VMArchitectureCommand> Commands;

        public VMRoomMap[] Rooms;
        public List<VMRoom> RoomData;
        public event ArchitectureEvent WallsChanged;

        public VMContext Context; //used for access to objects

        public delegate void ArchitectureEvent(VMArchitecture caller);

        private Blueprint WorldUI;

        private bool RealMode;

        private bool WallsDirty;
        private bool FloorsDirty;
        private bool TerrainDirty;

        private bool Redraw;

        private Color[] m_TimeColors = new Color[]
        {
            new Color(50, 70, 122)*1.5f,
            new Color(50, 70, 122)*1.5f,
            new Color(60, 80, 132)*1.5f,
            new Color(60, 80, 132)*1.5f,
            new Color(217, 109, 0),
            new Color(255, 255, 255),
            new Color(255, 255, 255),
            new Color(255, 255, 255),
            new Color(255, 255, 255),
            new Color(217, 109, 0),
            new Color(60, 80, 80)*1.5f,
            new Color(60, 80, 132)*1.5f,     
        };

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

            this.Rooms = new VMRoomMap[Stories];
            this.Terrain = new VMArchitectureTerrain(width, height);

            for (int i = 0; i < Stories; i++)
            {
                this.WallsAt[i] = new List<int>();
                this.Walls[i] = new WallTile[numTiles];
                this.VisWalls[i] = new WallTile[numTiles];

                this.Floors[i] = new FloorTile[numTiles];
                this.VisFloors[i] = new FloorTile[numTiles];
                this.ObjectSupport[i] = new bool[numTiles];

                if (i<Stories-1) this.Supported[i] = new bool[numTiles];

                this.Rooms[i] = new VMRoomMap();
            }
            
            this.RoomData = new List<VMRoom>();
            this.WorldUI = blueprint;

            this.Commands = new List<VMArchitectureCommand>();
            this.Commands = new List<VMArchitectureCommand>();

            WallsDirty = true;
            FloorsDirty = true;
            RealMode = true;
            Redraw = true;
        }

        public void SetTimeOfDay(double time)
        {
            Color col1 = m_TimeColors[(int)Math.Floor(time * (m_TimeColors.Length - 1))]; //first colour
            Color col2 = m_TimeColors[(int)Math.Floor(time * (m_TimeColors.Length - 1)) + 1]; //second colour
            double Progress = (time * (m_TimeColors.Length - 1)) % 1; //interpolation progress (mod 1)

            WorldUI.OutsideColor = Color.Lerp(col1, col2, (float)Progress); //linearly interpolate between the two colours for this specific time.
        }

        public void UpdateBuildableArea(Rectangle area, int floors)
        {
            //notify the lotview this has changed too, so it can be drawn.
            BuildableArea = area;
            BuildableFloors = floors;
            if (VM.UseWorld)
            {
                WorldUI.BuildableArea = BuildableArea;
                WorldUI.Terrain.TerrainDirty = true;
            }
            FloorsDirty = true;
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
            VMRoomMap rooms = Rooms[level - 2];
                
            int offset = 0;
            for (int y=0; y<Height; y++)
            {
                for (int x=0; x<Width; x++)
                {
                    //if we are an object support or are above a room that is not outside, we're supported.
                    if (objSup[offset] || !RoomData[(ushort)(rooms.Map[offset])].IsOutside) sup[offset] = true;
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
                                    if (!RoomData[(ushort)rooms.Map[newOff]].IsOutside || (objSup[newOff] && (Math.Abs(x2)<2 && Math.Abs(y2)<2)))
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

        public void SignalTerrainRedraw()
        {
            TerrainDirty = true;
        }

        public void RegenRoomMap()
        {
            RoomData = new List<VMRoom>();
            RoomData.Add(new VMRoom()); //dummy at index 0
            for (int i=0; i<Stories; i++)
            {
                Rooms[i].GenerateMap(Walls[i], Floors[i], Width, Height, RoomData);
                if (VM.UseWorld) WorldUI.RoomMap[i] = Rooms[i].Map;
                RegenerateSupported(i + 1);
            }
        }

        public void Tick()
        { 
            if (WallsDirty || FloorsDirty)
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
                LastTestCost = SimulateCommands(Commands, true);
                WorldUI.SignalWallChange();
                WorldUI.SignalFloorChange();
            }

            if (TerrainDirty && VM.UseWorld)
            {
                WorldUI.Terrain.UpdateTerrain(Terrain.LightType, Terrain.DarkType, Terrain.Heights, Terrain.GrassState);
                TerrainDirty = false;
            }

            var clock = Context.Clock;
            SetTimeOfDay(clock.Hours/24.0 + clock.Minutes/(24.0*60) + clock.Seconds/(24.0*60*60));

            FloorsDirty = false;
            Redraw = false;
            WallsDirty = false;
        }

        public int SimulateCommands(List<VMArchitectureCommand> commands, bool visualChange)
        {
            int cost;
            if (commands.Count == 0)
            {
                if (visualChange)
                {
                    //direct copy, no changes to make
                    WorldUI.Walls = Walls;
                    WorldUI.WallsAt = WallsAt;
                    WorldUI.Floors = Floors;
                }
                return 0;
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
                cost = RunCommands(commands, visualChange);

                if (visualChange)
                {
                    //upload modified walls to blueprint
                    WorldUI.Walls = Walls;
                    WorldUI.WallsAt = WallsAt;
                    WorldUI.Floors = Floors;
                }

                Floors = oldFloors;
                Walls = oldWalls;
                WallsAt = oldWallsAt;
                RealMode = true;
            }
            return cost;
        }

        public int RunCommands(List<VMArchitectureCommand> commands, bool transient)
        {
            int cost = 0; //negative for sellback;
            int pdCount = 0;
            ushort pdVal = 0;
            VMAvatar lastAvatar = null;
            for (var i=0; i<commands.Count; i++)
            {
                var com = commands[i];
                var avaEnt = Context.VM.Entities.FirstOrDefault(x => x.PersistID == com.CallerUID);
                if ((avaEnt == null || avaEnt is VMGameObject) && !transient) return 0; //we need an avatar to run a command from net
                var avatar = (transient)? null : (VMAvatar)avaEnt;
                lastAvatar = avatar;
                var styleInd = -1;
                var walls = Content.Content.Get().WorldWalls;
                walls.WallStyleToIndex.TryGetValue(com.style, out styleInd);
                //if patterns are invalid, don't do anything.
                switch (com.Type)
                {
                    case VMArchitectureCommandType.WALL_LINE:
                        if (styleInd == -1) break; //MUST be purchasable style
                        var lstyle = walls.GetWallStyle(com.style);
                        var nwCount = VMArchitectureTools.DrawWall(this, new Point(com.x, com.y), com.x2, com.y2, com.pattern, com.style, com.level, false);
                        if (nwCount > 0)
                        {
                            cost += nwCount * lstyle.Price;
                            if (avatar != null)
                            Context.VM.SignalChatEvent(new VMChatEvent(avatar.PersistID, VMChatEventType.Arch,
                            avatar.Name,
                            Context.VM.GetUserIP(avatar.PersistID),
                            "placed " + nwCount + " walls."
                            ));
                        }
                        break;
                    case VMArchitectureCommandType.WALL_DELETE:
                        var dwCount = VMArchitectureTools.EraseWall(this, new Point(com.x, com.y), com.x2, com.y2, com.pattern, com.style, com.level);
                        if (dwCount > 0)
                        {
                            cost -= 7 * dwCount;
                            if (avatar != null)
                            Context.VM.SignalChatEvent(new VMChatEvent(avatar.PersistID, VMChatEventType.Arch,
                            avatar.Name,
                            Context.VM.GetUserIP(avatar.PersistID),
                            "erased " + dwCount + " walls."
                            ));
                        }
                        break;
                    case VMArchitectureCommandType.WALL_RECT:
                        if (styleInd == -1) break; //MUST be purchasable style
                        var rstyle = walls.GetWallStyle(com.style);
                        var rwCount = VMArchitectureTools.DrawWallRect(this, new Rectangle(com.x, com.y, com.x2, com.y2), com.pattern, com.style, com.level);
                        if (rwCount > 0)
                        {
                            cost += rwCount * rstyle.Price;
                            if (avatar != null)
                            Context.VM.SignalChatEvent(new VMChatEvent(avatar.PersistID, VMChatEventType.Arch,
                            avatar.Name,
                            Context.VM.GetUserIP(avatar.PersistID),
                            "placed " + rwCount + " walls (rect)."
                        ));
                        }
                        break;
                    case VMArchitectureCommandType.PATTERN_FILL:
                        var pattern = GetPatternRef(com.pattern);
                        if (pattern == null && com.pattern != 0) break;
                        var pfCount = VMArchitectureTools.WallPatternFill(this, new Point(com.x, com.y), com.pattern, com.level);
                        if (pfCount.Total > 0)
                        {
                            cost -= pfCount.Cost - pfCount.Cost / 5;
                            cost += (pattern == null) ? 0 : pattern.Price * pfCount.Total;
                            if (avatar != null)
                                Context.VM.SignalChatEvent(new VMChatEvent(avatar.PersistID, VMChatEventType.Arch,
                            avatar.Name,
                            Context.VM.GetUserIP(avatar.PersistID),
                            "pattern filled " + pfCount + " walls with pattern #" + com.pattern
                        ));
                        }
                        break;
                    case VMArchitectureCommandType.PATTERN_DOT:
                        var pdpattern = GetPatternRef(com.pattern);
                        if (pdpattern == null && com.pattern != 0) break;
                        var dot = VMArchitectureTools.WallPatternDot(this, new Point(com.x, com.y), com.pattern, com.x2, com.y2, com.level);
                        pdVal = com.pattern;
                        if (dot.Total > -1)
                        {
                            cost -= dot.Cost - dot.Cost / 5;
                            cost += (pdpattern == null) ? 0 : pdpattern.Price;
                            pdCount++;
                        }

                        break;
                    case VMArchitectureCommandType.FLOOR_FILL:
                        var ffpattern = GetFloorRef(com.pattern);
                        if (ffpattern == null && com.pattern != 0) break;
                        var ffCount = VMArchitectureTools.FloorPatternFill(this, new Point(com.x, com.y), com.pattern, com.level);
                        if (ffCount.Total > 0)
                        {
                            cost -= (ffCount.Cost - ffCount.Cost / 5)/2;
                            cost += (ffpattern == null) ? 0 : ffpattern.Price * ffCount.Total / 2;

                            if (avatar != null)
                            Context.VM.SignalChatEvent(new VMChatEvent(avatar.PersistID, VMChatEventType.Arch,
                            avatar.Name,
                            Context.VM.GetUserIP(avatar.PersistID),
                            "floor filled " + ffCount.Total / 2f + " with pattern #" + com.pattern
                            ));
                        }
                        break;
                    case VMArchitectureCommandType.FLOOR_RECT:
                        var frpattern = GetFloorRef(com.pattern);
                        if (frpattern == null && com.pattern != 0) break;
                        var frCount = VMArchitectureTools.FloorPatternRect(this, new Rectangle(com.x, com.y, com.x2, com.y2), com.style, com.pattern, com.level);
                        if (frCount.Total > 0)
                        {
                            cost -= (frCount.Cost - frCount.Cost / 5) / 2;
                            cost += (frpattern == null) ? 0 : frpattern.Price * frCount.Total / 2;

                            if (avatar != null)
                                Context.VM.SignalChatEvent(new VMChatEvent(avatar.PersistID, VMChatEventType.Arch,
                                avatar.Name,
                                Context.VM.GetUserIP(avatar.PersistID),
                                "placed " + frCount.Total / 2f + " tiles with pattern #" + com.pattern
                            ));
                        }
                        break;
                }
            }
            if (lastAvatar != null && pdCount > 0)
                Context.VM.SignalChatEvent(new VMChatEvent(lastAvatar.PersistID, VMChatEventType.Arch,
                lastAvatar.Name,
                Context.VM.GetUserIP(lastAvatar.PersistID),
                "pattern dotted " + pdCount + " walls with pattern #" + pdVal
            ));

            return cost;
        }

        private WallReference GetPatternRef(ushort id)
        {
            WallReference result = null;
            var wallEntries = Content.Content.Get().WorldWalls.Entries;
            wallEntries.TryGetValue(id, out result);
            return result;
        }


        private FloorReference GetFloorRef(ushort id)
        {
            FloorReference result = null;
            Content.Content.Get().WorldFloors.Entries.TryGetValue(id, out result);
            return result;
        }

        /// <summary>
        /// Checks if there is a wall between two (Full Tile) points.
        /// </summary>
        /// <param name="p1">Start Position (Full Tile Pos)</param>
        /// <param name="p2">End Position (Full Tile Pos)</param>
        /// <param name="level">Level for both points</param>
        /// <returns>True if wall is detected between tiles.</returns>
        public bool RaycastWall(Point p1, Point p2, sbyte level)
        {
            //Bresenham's line algorithm, modified to check walls
            //http://lifc.univ-fcomte.fr/home/~ededu/projects/bresenham/

            int i, ystep, xstep, error, errorprev, ddy, ddx,
                y = p1.Y,
                x = p1.X,
                dx = p2.X - x,
                dy = p2.Y - y;

            if (dy < 0)
            {
                ystep = -1;
                dy = -dy;
            }
            else
                ystep = 1;

            if (dx < 0)
            {
                xstep = -1;
                dx = -dx;
            }
            else
                xstep = 1;

            ddy = dy * 2;
            ddx = dx * 2;

            int xAOff = (xstep > 0) ? 1 : 0;
            int yAOff = (ystep > 0) ? 1 : 0;

            if (ddx >= ddy)
            {
                errorprev = error = dx;
                for (i = 0; i < dx; i++)
                {
                    int oldx = x;
                    int oldy = y;
                    x += xstep;
                    error += ddy;
                    if (error > ddx)
                    {
                        y += ystep;
                        error -= ddx;

                        //extra steps
                        if (error + errorprev < ddx)
                        {
                            //moved into x before y
                            if (GetWall((short)(oldx+xAOff), (short)(oldy), level).TopLeftSolid) return true;
                            if (GetWall((short)(x), (short)(oldy + yAOff), level).TopRightSolid) return true;
                        }
                        else
                        {
                            //moved into y before x
                            if (GetWall((short)(oldx), (short)(oldy + yAOff), level).TopRightSolid) return true;
                            if (GetWall((short)(oldx + xAOff), (short)(y), level).TopLeftSolid) return true;
                        }
                    }
                    else
                    {
                        //only move into x
                        if (GetWall((short)(oldx+xAOff), (short)(oldy), level).TopLeftSolid) return true;
                    }
                    errorprev = error;
                }
            }
            else
            {
                errorprev = error = dy;
                for (i = 0; i < dy; i++)
                {
                    int oldx = x;
                    int oldy = y;
                    y += ystep;
                    error += ddx;
                    if (error > ddy)
                    {
                        x += xstep;
                        error -= ddy;

                        //extra steps
                        if (error + errorprev < ddy)
                        {
                            //moved into y before x
                            if (GetWall((short)(oldx), (short)(oldy + yAOff), level).TopRightSolid) return true;
                            if (GetWall((short)(oldx+xAOff), (short)(y), level).TopLeftSolid) return true;
                        }
                        else
                        {
                            //moved into x before y
                            if (GetWall((short)(oldx+xAOff), (short)(oldy), level).TopLeftSolid) return true;
                            if (GetWall((short)(x), (short)(oldy + yAOff), level).TopRightSolid) return true;
                        }
                    }
                    else
                    {
                        //only move into y
                        if (GetWall((short)(oldx), (short)(oldy+yAOff), level).TopRightSolid) return true;
                    }
                    
                    errorprev = error;
                }
            }
            return false;
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

        public bool OutsideClip(short tileX, short tileY, sbyte level)
        {
            var area = BuildableArea;
            if (DisableClip)
                return (tileX < 0 || tileY < 0 || level < 1 || tileX >= Width || tileY >= Height || level > Stories);
            else
                return (tileX < area.X || tileY < area.Y || level < 1 || tileX >= area.Right || tileY >= area.Bottom || level > BuildableFloors);
        }

        public bool SetFloor(short tileX, short tileY, sbyte level, FloorTile floor, bool force)
        {
            //returns false on failure
            var offset = GetOffset(tileX, tileY);

            if (!force)
            {
                //first check if we're supported
                if (floor.Pattern > 65533 && level > 1 && RoomData[(int)Rooms[level - 2].Map[offset]&0xFFFF].IsOutside) return false;
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


        #region VM Marshalling Functions
        public virtual VMArchitectureMarshal Save()
        {
            return new VMArchitectureMarshal
            {
                Width = Width,
                Height = Height,
                Stories = Stories,
                Terrain = Terrain,
        
                Walls = Walls,
                Floors = Floors,

                WallsDirty = WallsDirty,
                FloorsDirty = FloorsDirty
            };
        }

        public virtual void Load(VMArchitectureMarshal input)
        {
            Width = input.Width;
            Height = input.Height;
            Stories = input.Stories;
            Terrain = input.Terrain;

            Walls = input.Walls;
            Floors = input.Floors;

            RegenWallsAt();
            SignalTerrainRedraw();
        }

        public void WallDirtyState(VMArchitectureMarshal input)
        {
            WallsDirty = input.WallsDirty;
            FloorsDirty = input.FloorsDirty;
            Redraw = true;
        }

        public void RegenWallsAt()
        {
            WallsAt = new List<int>[Stories];
            for (int i=0; i<Stories; i++)
            {
                var list = new List<int>();

                var wIt = Walls[i];
                for (int j=0; j<wIt.Length; j++)
                {
                    if (wIt[j].Segments > 0) list.Add(j);
                }
                WallsAt[i] = list;
            }
        }

        public VMArchitecture(VMArchitectureMarshal input, VMContext context, Blueprint blueprint) : this(input.Width, input.Height, blueprint, context)
        {
            Load(input);
        }
        #endregion
    }
}
