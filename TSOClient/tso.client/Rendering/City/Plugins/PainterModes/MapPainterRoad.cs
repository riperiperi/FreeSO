using FSO.Client.UI.Model;
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

        private static Point[] WLStep =
        {
            new Point(1, 0),
            new Point(0, 1),
            new Point(-1, 0),
            new Point(0, -1),
        };

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

            HIT.HITVM.Get().PlaySoundEvent(UISounds.BuildDragToolDown);
        }

        public void TileMouseUp(Vector2? tile)
        {
            if (Road != null)
            {
                HIT.HITVM.Get().PlaySoundEvent(UISounds.BuildDragToolUp);

                Painter.Commit(Road.Length > 0);

                Road = null;
            }
        }

        public void Update(UpdateState state)
        {
            
        }

        private Point GetWallEnd()
        {
            var pos = new Point(Road.StartX, Road.StartY);
            var step = WLStep[Road.Direction];

            pos += new Point(step.X * Road.Length, step.Y * Road.Length);

            return pos;
        }

        public void Draw(SpriteBatch sb)
        {
            float cursorScale = 8;
            if (Road != null)
            {
                var anchor = City.Content.PainterCursorAnchor;

                City.DrawLocal3D(sb, anchor, WallBase.ToVector2(), new Vector2(-anchor.Width / 2f, -anchor.Height), cursorScale, Vector2.One, Color.White);
            }

            var wallPos = Road == null ? new Point((int)Math.Round(LastPos.X), (int)Math.Round(LastPos.Y)) : GetWallEnd();

            var cursor = Road == null ? City.Content.PainterCursor : City.Content.PainterCursorActive;
            City.DrawLocal3D(sb, cursor, wallPos.ToVector2(), new Vector2(-cursor.Width / 2f, -cursor.Height), cursorScale, Vector2.One, Color.White);

            var iconColor = Road == null ? new Color(203, 231, 225, 255) : new Color(253, 246, 153, 255);
            var scale = Road == null ? 1f : (62f / 58f);
            var icon = Painter.Erasing ? City.Content.PainterRoadDel : City.Content.PainterRoadIcon;
            City.DrawLocal3D(
                sb,
                icon,
                wallPos.ToVector2(),
                new Vector2(-(icon.Width * scale) / 2f, (-cursor.Height + cursor.Width / 2) - (icon.Height * scale) / 2f),
                cursorScale,
                new Vector2(scale), iconColor);
        }
    }
}
