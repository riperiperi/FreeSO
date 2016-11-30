using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FSO.Common.Rendering.Framework.Model;
using FSO.Client.UI.Framework;
using FSO.Common.Utils;
using Microsoft.Xna.Framework.Input;

namespace FSO.Client.Rendering.City.Plugins
{
    public class MapPainterPlugin : AbstractCityPlugin
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

        private static RoadSegs[] MainCorner =
        {
            RoadSegs.Right,
            RoadSegs.Left,
            RoadSegs.Bottom,
            RoadSegs.Bottom,
        };
        private static RoadSegs[] SubCorner =
        {
            RoadSegs.Top,
            RoadSegs.Top,
            RoadSegs.Left,
            RoadSegs.Right,
        };
        private static RoadSegs[] MainEndCorner =
        {
            RoadSegs.Bottom,
            RoadSegs.Bottom,
            RoadSegs.Right,
            RoadSegs.Left,
        };
        private static RoadSegs[] SubEndCorner =
        {
            RoadSegs.Left,
            RoadSegs.Right,
            RoadSegs.Top,
            RoadSegs.Top,
        };

        private static Dictionary<RoadSegs, RoadSegs> CornerRemovalEdges = new Dictionary<RoadSegs, RoadSegs>
        {
            { RoadSegs.Bottom, RoadSegs.TopLeft | RoadSegs.TopRight }, //i likely have the names for these wrong...
            { RoadSegs.Left, RoadSegs.TopLeft | RoadSegs.BottomLeft },
            { RoadSegs.Top, RoadSegs.BottomLeft | RoadSegs.BottomRight },
            { RoadSegs.Right, RoadSegs.BottomRight | RoadSegs.TopRight }
        };

        public Vector2 LastPos;
        public Point WallBase;
        public Point WallTarget;
        private int WallLength;
        private int WallDir;
        private bool Erasing;
        public byte[] OriginalData;

        private bool MouseDown;
        private bool MouseClicked;

        public Color[] TerrainTypes = new Color[] {
            new Color(0, 255, 0), //grass
            new Color(12, 0, 255), //water
            new Color(255, 0, 0), //rock
            new Color(255, 255, 255), //snow
            new Color(255, 255, 0) //sand
        };
        public string[] TerrainTypeNames = new string[] {
            "Grass",
            "Water",
            "Rock",
            "Snow",
            "Sand"
        };
        public int SelectedModifier;
        public int BrushSize;
        public PainterMode Mode;
        private Rectangle? ChangeBounds;

        private Dictionary<Point, float> ElevationMod;
        private int ElevationFrames = 0;

        public MapPainterPlugin(Terrain city) : base(city)
        {
        }

        private void AddChange(Point pos)
        {
            var rect = new Rectangle(pos, new Point(1, 1));
            AddChange(rect);
        }

        private void AddChange(Rectangle rect)
        {
            if (ChangeBounds == null) ChangeBounds = rect;
            else
            {
                ChangeBounds = Rectangle.Union(ChangeBounds.Value, rect);
            }
        }

        public override void Draw(SpriteBatch sb)
        {
            sb.Begin();
            sb.DrawString(TextStyle.DefaultLabel.Font.GetNearest(12).Font, Mode.ToString(), new Vector2(10, 10), Color.White);

            switch (Mode)
            {
                case PainterMode.ROAD:
                    if (MouseDown)
                    {
                        var onScreen2 = City.Get2DFromTile(WallBase.X, WallBase.Y);
                        City.DrawLine(TextureGenerator.GetPxWhite(sb.GraphicsDevice), onScreen2, onScreen2 + new Vector2(0, -50), sb, 5, 100);
                    }

                    var wallPos = new Point((int)Math.Round(LastPos.X), (int)Math.Round(LastPos.Y));
                    var onScreen = City.Get2DFromTile(wallPos.X, wallPos.Y);
                    City.DrawLine(TextureGenerator.GetPxWhite(sb.GraphicsDevice), onScreen, onScreen + new Vector2(0, -30), sb, 3, 100);
                    break;
                case PainterMode.TERRAINTYPE:
                    float iScale = (float)(1 / (City.GetIsoScale() * 2));

                    Color selColor = Color.White;

                    switch (Mode)
                    {
                        case PainterMode.TERRAINTYPE:
                            selColor = TerrainTypes[SelectedModifier]; break;
                    }

                    BrushFunc(BrushSize, (x, y, strength) =>
                    {
                        if (strength > 0) City.PathTile((int)LastPos.X + x, (int)LastPos.Y + y, iScale, new Color(selColor, 0.5f));
                    });
                    City.Draw2DPoly();
                    break;
                case PainterMode.ELEVATION_CIRCLE:
                    var ePos = new Point((int)Math.Round(LastPos.X), (int)Math.Round(LastPos.Y));
                    BrushFunc(BrushSize, (x, y, strength) =>
                    {
                        //if (strength <= 0) return;
                        var eOnScreen = City.Get2DFromTile(ePos.X + x, ePos.Y + y);
                        City.DrawLine(TextureGenerator.GetPxWhite(sb.GraphicsDevice), eOnScreen, eOnScreen + new Vector2(0, -50) * strength, sb, 3, 100);
                    });
                    break;
            }

            sb.End();
        }

        public override void TileHover(Vector2? tile)
        {
            if (tile != null && MouseDown) {
                var wallPos = new Point((int)Math.Round(tile.Value.X), (int)Math.Round(tile.Value.Y));
                switch (Mode)
                {
                    case PainterMode.ROAD:
                        if (wallPos != WallTarget && OriginalData != null)
                        {
                            WallTarget = wallPos;
                            var xd = (WallTarget.X - WallBase.X);
                            var yd = (WallTarget.Y - WallBase.Y);
                            WallLength = (int)Math.Sqrt(xd * xd + yd * yd);
                            WallDir = (int)DirectionUtils.PosMod(Math.Round(Math.Atan2(yd, xd) / (Math.PI / 2)), 4);

                            Array.Copy(OriginalData, City.MapData.RoadData, OriginalData.Length);
                            if (Erasing) EraseWall(City.MapData.RoadData, WallBase, WallLength, WallDir);
                            else DrawWall(City.MapData.RoadData, WallBase, WallLength, WallDir);

                            City.GenerateCityMesh(GameFacade.GraphicsDevice, ChangeBounds);
                        }
                        break;
                    case PainterMode.TERRAINTYPE:
                        var newPt = tile.Value.ToPoint();
                        if (MouseClicked || newPt != LastPos.ToPoint())
                        {
                            BrushFunc(BrushSize, (x, y, strength) =>
                            {
                                if (strength > 0) City.MapData.TerrainTypeColorData[newPt.X+x + (newPt.Y+y) * 512] = TerrainTypes[SelectedModifier];
                            });

                            AddChange(new Rectangle(newPt.X - (1+BrushSize), newPt.Y - (1+BrushSize), 3+BrushSize*2, 3+BrushSize*2));
                            City.GenerateCityMesh(GameFacade.GraphicsDevice, ChangeBounds);
                            MouseClicked = false;
                            break;
                        }
                        break;
                    case PainterMode.ELEVATION_CIRCLE:
                        if (OriginalData == null) return;
                        BrushFunc(BrushSize, (x, y, strength) =>
                        {
                            if (strength > 0) {
                                var loc = new Point(wallPos.X + x, wallPos.Y + y);
                                if (ElevationMod.ContainsKey(loc)) ElevationMod[loc] += ((Erasing)?-1:1)* strength / 5;
                                else ElevationMod[loc] = ((Erasing) ? -1 : 1) * strength / 5;
                            }
                        });
                        AddChange(new Rectangle(wallPos.X - (1 + BrushSize), wallPos.Y - (1 + BrushSize), 2 + BrushSize * 2, 2 + BrushSize * 2));
                        break;
                }
            }

            if (tile != null) LastPos = tile.Value;
        }

        public override void TileMouseDown(Vector2 tile)
        {
            ChangeBounds = null;
            var wallPos = new Point((int)Math.Round(tile.X), (int)Math.Round(tile.Y));
            switch (Mode)
            {
                case PainterMode.ROAD:
                    OriginalData = new byte[City.MapData.RoadData.Length];
                    Array.Copy(City.MapData.RoadData, OriginalData, OriginalData.Length);

                    WallBase = wallPos;
                    WallTarget = wallPos;
                    WallLength = 0;
                    WallDir = 0;
                    break;
                case PainterMode.ELEVATION_CIRCLE:
                    OriginalData = new byte[City.MapData.ElevationData.Length];
                    Array.Copy(City.MapData.ElevationData, OriginalData, OriginalData.Length);

                    ElevationMod = new Dictionary<Point, float>();
                    ElevationFrames = 0;
                    break;
            }

            MouseDown = true;
            MouseClicked = true;
        }

        public override void TileMouseUp(Vector2? tile)
        {
            switch (Mode)
            {
                case PainterMode.ROAD:
                    if (WallLength != 0)
                    {
                        ChangeBounds = null;
                    }
                    else
                    {
                        RestoreOld();
                    }
                    break;
                case PainterMode.ELEVATION_CIRCLE:
                    ChangeBounds = null;
                    break;
            }
            MouseDown = false;
        }

        public void SwitchMode(PainterMode newMode)
        {
            if (Mode != newMode) TileMouseUp(null);
            Mode = newMode;
            OriginalData = null;
        }

        public override void Update(UpdateState state)
        {
            if (Mode == PainterMode.ELEVATION_CIRCLE && MouseDown && ChangeBounds != null && ElevationFrames-- <= 0)
            {
                Array.Copy(OriginalData, City.MapData.ElevationData, OriginalData.Length);
                foreach (var mod in ElevationMod)
                {
                    var index = mod.Key.X + mod.Key.Y * 512;
                    if (index < 0 || index > City.MapData.ElevationData.Length) continue;
                    City.MapData.ElevationData[index] = (byte)Math.Max(0, Math.Min(255, Math.Round(City.MapData.ElevationData[index]+mod.Value)));
                }
                City.GenerateCityMesh(GameFacade.GraphicsDevice, ChangeBounds);
                ElevationFrames = 5;
            }

            Erasing = state.KeyboardState.IsKeyDown(Keys.LeftControl);
            if (state.MouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Released)
            {
                if (MouseDown) RestoreOld();
                MouseDown = false;
            }
            var keys = state.KeyboardState;
            if (keys.IsKeyDown(Keys.R)) SwitchMode(PainterMode.ROAD);
            if (keys.IsKeyDown(Keys.T)) SwitchMode(PainterMode.TERRAINTYPE);
            if (keys.IsKeyDown(Keys.E)) SwitchMode(PainterMode.ELEVATION_CIRCLE);

            var oldS = SelectedModifier;
            if (keys.IsKeyDown(Keys.NumPad0)) SelectedModifier = 0;
            if (keys.IsKeyDown(Keys.NumPad1)) SelectedModifier = 1;
            if (keys.IsKeyDown(Keys.NumPad2)) SelectedModifier = 2;
            if (keys.IsKeyDown(Keys.NumPad3)) SelectedModifier = 3;
            if (keys.IsKeyDown(Keys.NumPad4)) SelectedModifier = 4;

            if (state.KeyboardState.IsKeyDown(Keys.LeftShift))
            {
                BrushSize = SelectedModifier;
                SelectedModifier = oldS;
            }
        }

        public void BrushFunc(int width, Callback<int, int, float> callback)
        {
            var boxWidth = width * 2 + 1;
            for (int y = 0; y < boxWidth; y++)
            {
                for (int x = 0; x < boxWidth; x++)
                {
                    var dist = Math.Sqrt((x-width)*(x-width) + (y-width)*(y-width)) / (width + 0.5);
                    callback(x-width, y-width, (float)Math.Max(0, Math.Cos(dist * Math.PI / 2)));
                }
            }
        }

        public void RestoreOld ()
        {
            if (OriginalData != null)
            {
                if (Mode == PainterMode.ROAD) City.MapData.RoadData = OriginalData;
                else if (Mode == PainterMode.ELEVATION_CIRCLE) City.MapData.ElevationData = OriginalData;
            }
            OriginalData = null;
            City.GenerateCityMesh(GameFacade.GraphicsDevice, ChangeBounds);
            ChangeBounds = null;
        }

        public void TryAddCorner(byte[] map, int index, RoadSegs seg)
        {
            if ((map[index] & (byte)CornerRemovalEdges[seg]) == 0)
            {
                map[index] |= (byte)seg;
            }
        }

        public void DrawWall(byte[] map, Point pos, int length, int direction)
        {
            pos += WLStartOff[direction];

            var endPos = pos - WLStep[direction];
            TryAddCorner(map, endPos.X + endPos.Y * 512, MainCorner[direction]);
            AddChange(endPos);

            endPos += WLSubOff[direction];
            TryAddCorner(map, endPos.X + endPos.Y * 512, SubCorner[direction]);
            AddChange(endPos);

            for (int i = 0; i < length; i++)
            {
                map[pos.X + pos.Y * 512] |= (byte)WLMainSeg[direction];
                map[pos.X + pos.Y * 512] &= (byte)(~MainCorner[direction]);
                map[pos.X + pos.Y * 512] &= (byte)(~MainEndCorner[direction]);
                AddChange(pos);
                var tPos = pos + WLSubOff[direction];
                map[tPos.X + tPos.Y * 512] |= (byte)WLSubSeg[direction];
                map[tPos.X + tPos.Y * 512] &= (byte)(~SubCorner[direction]);
                map[tPos.X + tPos.Y * 512] &= (byte)(~SubEndCorner[direction]);
                AddChange(tPos);
                pos += WLStep[direction];
            }

            endPos = pos;
            AddChange(endPos);
            TryAddCorner(map, endPos.X + endPos.Y * 512, MainEndCorner[direction]);
            endPos += WLSubOff[direction];
            AddChange(endPos);
            TryAddCorner(map, endPos.X + endPos.Y * 512, SubEndCorner[direction]);
        }

        public void EraseWall(byte[] map, Point pos, int length, int direction)
        {
            pos += WLStartOff[direction];

            var endPos = pos - WLStep[direction];
            map[endPos.X + endPos.Y * 512] &= (byte)(~MainCorner[direction]);
            AddChange(endPos);
            endPos += WLSubOff[direction];
            map[endPos.X + endPos.Y * 512] &= (byte)(~SubCorner[direction]);
            AddChange(endPos);

            for (int i = 0; i < length; i++)
            {
                map[pos.X + pos.Y * 512] &= (byte)~WLMainSeg[direction];
                AddChange(pos);
                var tPos = pos + WLSubOff[direction];
                map[tPos.X + tPos.Y * 512] &= (byte)~WLSubSeg[direction];
                AddChange(tPos);

                pos += WLStep[direction];
            }

            endPos = pos;
            map[endPos.X + endPos.Y * 512] &= (byte)(~MainEndCorner[direction]);
            AddChange(endPos);
            endPos += WLSubOff[direction];
            map[endPos.X + endPos.Y * 512] &= (byte)(~SubEndCorner[direction]);
            AddChange(endPos);
        }
    }

    public enum PainterMode
    {
        ROAD,
        TERRAINTYPE,
        ELEVATION_CIRCLE,
        ELEVATION_FLAT,
        FORESTTYPE,
        FORESTDENSITY
    }

    public enum RoadSegs : byte
    {
        TopLeft = 1,
        BottomLeft = 2,
        BottomRight = 4,
        TopRight = 8,

        Bottom = 16,
        Left = 32,
        Top = 64,
        Right = 128
    }
}
