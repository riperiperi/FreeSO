using FSO.Common.Rendering.Framework.Model;
using FSO.Server.Protocol.Electron.Model.CityEditCommands;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.InteropServices;

namespace FSO.Client.Rendering.City.Plugins.PainterModes
{
    internal class MapPainterPaint<T> : IMapPainterMode where T : unmanaged
    {
        private readonly MapPainterPlugin Painter;
        private readonly Color[] ModifierToColor;
        private readonly T[] ModifierToValue;
        private readonly CityEditPaintType Type;

        private Terrain City => Painter.City;

        private CityEditPaint Paint;
        public CityEditBase Command => Paint;
        private Vector2 LastPos;

        private bool AnySet;

        public MapPainterPaint(MapPainterPlugin painter, Color[] modifierToColor, T[] modifierToValue, CityEditPaintType type)
        {
            Painter = painter;
            ModifierToColor = modifierToColor;
            ModifierToValue = modifierToValue;
            Type = type;
        }

        public void Draw(SpriteBatch sb)
        {
            float iScale = (float)(1 / (City.GetIsoScale() * 2));

            Color selColor = Painter.SelectedModifier < 0 || Painter.SelectedModifier >= ModifierToColor.Length ?
                Color.White :
                ModifierToColor[Painter.SelectedModifier];

            IMapPainterMode.BrushFunc(Painter.BrushSize, (x, y, strength) =>
            {
                if (strength > 0) City.PathTile((int)LastPos.X + x, (int)LastPos.Y + y, iScale, new Color(selColor, 0.5f));
            });

            City.Draw2DPoly(false);
        }

        private void Submit()
        {
            if (Paint != null)
            {
                Painter.Commit(AnySet);

                Paint = null;
            }
        }

        private void NewPaint()
        {
            Submit();

            var valueAsByte = MemoryMarshal.Cast<T, byte>(ModifierToValue);

            Paint = new CityEditPaint()
            {
                Type = Type,
                Value = valueAsByte[Painter.SelectedModifier],
                Bitmap = new CityEditBitmap(City.MapData.Width, City.MapData.Height)
            };

            AnySet = false;
        }

        private void ApplyBrush(Point newPt)
        {
            IMapPainterMode.BrushFunc(Painter.BrushSize, (x, y, strength) =>
            {
                int targetX = newPt.X + x;
                int targetY = newPt.Y + y;

                if (targetX < 0 || targetX >= 512 || targetY < 0 || targetY >= 512)
                {
                    return;
                }

                if (strength > 0)
                {
                    Paint.Bitmap.Set(targetX, targetY);
                    AnySet = true;
                }
            });

            Painter.UpdateTemp();
        }

        public void TileHover(Vector2? tile)
        {
            if (Paint != null && tile != null)
            {
                var newPt = tile.Value.ToPoint();

                if (newPt != LastPos.ToPoint())
                {
                    ApplyBrush(newPt);
                }
            }

            if (tile != null) LastPos = tile.Value;
        }

        public void TileMouseDown(Vector2 tile)
        {
            NewPaint();

            ApplyBrush(tile.ToPoint());
        }

        public void TileMouseUp(Vector2? tile)
        {
            Submit();
        }

        public void Update(UpdateState state)
        {

        }
    }
}
