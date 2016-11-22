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
        public byte[] BaseRoad;

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
        public PainterMode Mode;
        private Rectangle? ChangeBounds;

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

            if (Mode == PainterMode.ROAD)
            {
                if (MouseDown)
                {
                    var onScreen2 = City.Get2DFromTile(WallBase.X, WallBase.Y);
                    City.DrawLine(TextureGenerator.GetPxWhite(sb.GraphicsDevice), onScreen2, onScreen2 + new Vector2(0, -50), sb, 5, 100);
                }

                var wallPos = new Point((int)Math.Round(LastPos.X), (int)Math.Round(LastPos.Y));
                var onScreen = City.Get2DFromTile(wallPos.X, wallPos.Y);
                City.DrawLine(TextureGenerator.GetPxWhite(sb.GraphicsDevice), onScreen, onScreen + new Vector2(0, -30), sb, 3, 100);
            }
            else
            {
                float iScale = (float)(1 / (City.GetIsoScale() * 2));

                Color selColor = Color.White;

                switch (Mode)
                {
                    case PainterMode.TERRAINTYPE:
                        selColor = TerrainTypes[SelectedModifier]; break;
                }
                City.PathTile((int)LastPos.X, (int)LastPos.Y, iScale, new Color(selColor, 0.5f));
                City.Draw2DPoly();
            }

            sb.End();
        }

        public override void TileHover(Vector2? tile)
        {
            if (tile != null && MouseDown) {
                switch (Mode)
                {
                    case PainterMode.ROAD:
                        var wallPos = new Point((int)Math.Round(tile.Value.X), (int)Math.Round(tile.Value.Y));
                        if (wallPos != WallTarget)
                        {
                            WallTarget = wallPos;
                            var xd = (WallTarget.X - WallBase.X);
                            var yd = (WallTarget.Y - WallBase.Y);
                            WallLength = (int)Math.Sqrt(xd * xd + yd * yd);
                            WallDir = (int)DirectionUtils.PosMod(Math.Round(Math.Atan2(yd, xd) / (Math.PI / 2)), 4);

                            Array.Copy(BaseRoad, City.MapData.RoadData, BaseRoad.Length);
                            if (Erasing) EraseWall(City.MapData.RoadData, WallBase, WallLength, WallDir);
                            else DrawWall(City.MapData.RoadData, WallBase, WallLength, WallDir);

                            City.GenerateCityMesh(GameFacade.GraphicsDevice, ChangeBounds);
                        }
                        break;
                    case PainterMode.TERRAINTYPE:
                        var newPt = tile.Value.ToPoint();
                        if (MouseClicked || newPt != LastPos.ToPoint())
                        {
                            City.MapData.TerrainTypeColorData[newPt.X + newPt.Y * 512] = TerrainTypes[SelectedModifier];
                            AddChange(new Rectangle(newPt.X - 1, newPt.Y - 1, 3, 3));
                            City.GenerateCityMesh(GameFacade.GraphicsDevice, ChangeBounds);
                            MouseClicked = false;
                            break;
                        }
                        break;
                }
            }

            if (tile != null) LastPos = tile.Value;
        }

        public override void TileMouseDown(Vector2 tile)
        {
            switch (Mode)
            {
                case PainterMode.ROAD:
                    var wallPos = new Point((int)Math.Round(tile.X), (int)Math.Round(tile.Y));
                    BaseRoad = new byte[City.MapData.RoadData.Length];
                    Array.Copy(City.MapData.RoadData, BaseRoad, BaseRoad.Length);

                    WallBase = wallPos;
                    WallTarget = wallPos;
                    WallLength = 0;
                    WallDir = 0;
                    break;
            }

            MouseDown = true;
            MouseClicked = true;
        }

        public override void TileMouseUp(Vector2? tile)
        {
            if (Mode == PainterMode.ROAD)
            {
                if (WallLength != 0)
                {
                    ChangeBounds = null;
                }
                else
                {
                    RestoreOld();
                }
            }
            MouseDown = false;
        }

        public override void Update(UpdateState state)
        {
            Erasing = state.KeyboardState.IsKeyDown(Keys.LeftControl);
            if (state.MouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Released)
            {
                if (MouseDown) RestoreOld();
                MouseDown = false;
            }
            var keys = state.KeyboardState;
            if (keys.IsKeyDown(Keys.R)) Mode = PainterMode.ROAD;
            if (keys.IsKeyDown(Keys.T)) Mode = PainterMode.TERRAINTYPE;

            if (keys.IsKeyDown(Keys.NumPad0)) SelectedModifier = 0;
            if (keys.IsKeyDown(Keys.NumPad1)) SelectedModifier = 1;
            if (keys.IsKeyDown(Keys.NumPad2)) SelectedModifier = 2;
            if (keys.IsKeyDown(Keys.NumPad3)) SelectedModifier = 3;
            if (keys.IsKeyDown(Keys.NumPad4)) SelectedModifier = 4;
        }

        public void RestoreOld ()
        {
            if (BaseRoad != null) City.MapData.RoadData = BaseRoad;
            BaseRoad = null;
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
