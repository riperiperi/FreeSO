using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Utils;
using FSO.Server.Protocol.Electron.Model.CityEditCommands;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.Client.Rendering.City.Plugins.PainterModes
{
    internal class MapPainterRoad : IMapPainterMode
    {
        private readonly MapPainterPlugin Painter;
        private Terrain City => Painter.City;
        private CityEditRoad Road;
        public CityEditBase Command => Road;

        private Point WallBase;
        private Point WallTarget;
        private Vector2 LastPos;

        public MapPainterRoad(MapPainterPlugin painter)
        {
            Painter = painter;
        }

        public void TileHover(Vector2? tile)
        {
            if (tile != null && Road != null)
            {
                var wallPos = new Point((int)Math.Round(tile.Value.X), (int)Math.Round(tile.Value.Y));
                var newPt = tile.Value.ToPoint();

                if (wallPos != WallTarget)
                {
                    WallTarget = wallPos;
                    var xd = (WallTarget.X - WallBase.X);
                    var yd = (WallTarget.Y - WallBase.Y);
                    Road.Length = (int)Math.Sqrt(xd * xd + yd * yd);
                    Road.Direction = (int)DirectionUtils.PosMod(Math.Round(Math.Atan2(yd, xd) / (Math.PI / 2)), 4);
                    Road.Delete = Painter.Erasing;

                    Painter.UpdateTemp();
                }
            }

            if (tile != null) LastPos = tile.Value;
        }

        public void TileMouseDown(Vector2 tile)
        {
            var wallPos = new Point((int)Math.Round(tile.X), (int)Math.Round(tile.Y));

            WallBase = wallPos;
            WallTarget = wallPos;

            Road = new CityEditRoad()
            {
                StartX = wallPos.X,
                StartY = wallPos.Y,
            };
        }

        public void TileMouseUp(Vector2? tile)
        {
            if (Road != null)
            {
                Painter.Commit(Road.Length > 0);

                Road = null;
            }
        }

        public void Update(UpdateState state)
        {
            
        }

        public void Draw(SpriteBatch sb)
        {
            if (Road != null)
            {
                var onScreen2 = City.Get2DFromTile(WallBase.X, WallBase.Y);
                City.DrawLine(TextureGenerator.GetPxWhite(sb.GraphicsDevice), onScreen2, onScreen2 + new Vector2(0, -50), sb, 5, 100);
            }

            var wallPos = new Point((int)Math.Round(LastPos.X), (int)Math.Round(LastPos.Y));
            var onScreen = City.Get2DFromTile(wallPos.X, wallPos.Y);
            City.DrawLine(TextureGenerator.GetPxWhite(sb.GraphicsDevice), onScreen, onScreen + new Vector2(0, -30), sb, 3, 100);
        }
    }
}
