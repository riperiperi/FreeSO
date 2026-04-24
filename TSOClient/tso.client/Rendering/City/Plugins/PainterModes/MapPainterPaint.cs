using FSO.Client.UI.Model;
using FSO.Common;
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
        private readonly MapPainterSpraypaint Spray;
        public CityEditBase Command => Paint;
        private Vector2 LastPos;

        private bool AnySet;

        private readonly Dictionary<Point, float> SprayIntensities = [];
        private int SprayFrames;

        public MapPainterPaint(MapPainterPlugin painter, Color[] modifierToColor, T[] modifierToValue, CityEditPaintType type)
        {
            Painter = painter;
            ModifierToColor = modifierToColor;
            ModifierToValue = modifierToValue;
            Type = type;

            Spray = new MapPainterSpraypaint();
            Spray.NewSeed();
        }

        public void Draw(SpriteBatch sb)
        {
            float iScale = (float)(1 / (City.GetIsoScale() * 2));

            Color selColor = Painter.SelectedModifier < 0 || Painter.SelectedModifier >= ModifierToColor.Length ?
                Color.White :
                ModifierToColor[Painter.SelectedModifier];

            float intensity = Painter.BrushIntensity;
            float multiplier = Painter.Accelerate ? 2 : 1;
            var spray = Painter.SprayBrush;

            IMapPainterMode.BrushFunc(Painter.BrushSize, (x, y, strength) =>
            {
                if (spray)
                {
                    int ix = (int)LastPos.X + x;
                    int iy = (int)LastPos.Y + y;
                    if (ix >= 0 && iy >= 0 && ix < 512 && iy < 512)
                    {
                        var sprayIntensity = Spray.GetSpraypaint(iy * 512 + ix, strength);
                        City.PathTile(ix, iy, iScale, new Color(selColor, Math.Min(0.5f, sprayIntensity * (intensity + 0.2f) * multiplier * 0.4f)));
                    }
                }
                else
                {
                    if (strength > 0) City.PathTile((int)LastPos.X + x, (int)LastPos.Y + y, iScale, new Color(selColor, 0.5f));
                }
            });

            City.Draw2DPoly(false);
        }

        private void ResetSprayFrames()
        {
            var frameMul = FSOEnvironment.RefreshRate / 60f;
            SprayFrames = (int)(5 * frameMul);
        }

        private void Submit()
        {
            if (Paint != null)
            {
                HIT.HITVM.Get().PlaySoundEvent(UISounds.BuildDragToolUp);
                Paint.Trim();
                Painter.Commit(AnySet);

                Paint = null;
            }
        }

        private void NewPaint()
        {
            Submit();
            SprayIntensities.Clear();
            if (Painter.SprayBrush)
            {
                Spray.NewSeed();
            }
            SprayFrames = 0;

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
            var frameMul = 60f / FSOEnvironment.RefreshRate;
            var spray = Painter.SprayBrush;
            var intensity = Painter.BrushIntensity;
            IMapPainterMode.BrushFunc(Painter.BrushSize, (x, y, strength) =>
            {
                int targetX = newPt.X + x;
                int targetY = newPt.Y + y;

                if (targetX < 0 || targetX >= 512 || targetY < 0 || targetY >= 512)
                {
                    return;
                }

                if (spray)
                {
                    var key = new Point(targetX, targetY);
                    var sprayIntensity = Spray.GetSpraypaint(targetY * 512 + targetX, strength);

                    SprayIntensities.TryGetValue(key, out float acc);
                    acc += sprayIntensity * frameMul * intensity;
                    SprayIntensities[key] = acc;

                    if (acc > 4)
                    {
                        Paint.Bitmap.Set(targetX, targetY);
                        AnySet = true;
                    }
                }
                else
                {
                    if (strength > 0)
                    {
                        Paint.Bitmap.Set(targetX, targetY);
                        AnySet = true;
                    }
                }
            });

            if (!spray || --SprayFrames <= 0)
            {
                Painter.UpdateTemp();
                ResetSprayFrames();
            }
        }

        public void TileHover(Vector2? tile)
        {
            if (Paint != null && tile != null)
            {
                var newPt = tile.Value.ToPoint();
                var spray = Painter.SprayBrush;

                if (spray || newPt != LastPos.ToPoint())
                {
                    ApplyBrush(newPt);
                }
            }

            if (tile != null) LastPos = tile.Value;
        }

        public void TileMouseDown(Vector2 tile)
        {
            HIT.HITVM.Get().PlaySoundEvent(UISounds.BuildDragToolDown);
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
