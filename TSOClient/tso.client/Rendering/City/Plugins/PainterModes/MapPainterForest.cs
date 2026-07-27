using FSO.Client.UI.Model;
using FSO.Common;
using FSO.Common.Rendering.Framework.Model;
using FSO.Server.Protocol.Electron.Model.CityEditCommands;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.InteropServices;

namespace FSO.Client.Rendering.City.Plugins.PainterModes
{
    internal class MapPainterForest<T> : IMapPainterMode where T : unmanaged
    {
        private readonly float MaxIntensity = 4;
        private readonly MapPainterPlugin Painter;
        private readonly Color[] ModifierToColor;
        private readonly T[] ModifierToValue;
        private readonly CityEditPaintType Type;

        private Terrain City => Painter.City;

        private CityEditForest Forest;
        public CityEditBase Command => Forest;

        private Vector2 LastPos;
        private Dictionary<Point, float> ForestMod;
        private readonly MapPainterSpraypaint Spray;
        private int SprayFrames;

        private bool AnySet;

        public MapPainterForest(MapPainterPlugin painter, Color[] modifierToColor, T[] modifierToValue, CityEditPaintType type)
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
                        City.PathTile(ix, iy, iScale, new Color(selColor, Math.Min(0.3f, sprayIntensity * (intensity + 0.2f) * multiplier * 0.3f)));
                    }
                }
                else
                {
                    if (strength > 0) City.PathTile((int)LastPos.X + x, (int)LastPos.Y + y, iScale, new Color(selColor, 0.15f + 0.10f * intensity));
                }
            });

            City.Draw2DPoly(false);
        }

        private void ResetSprayFrames()
        {
            var frameMul = FSOEnvironment.RefreshRate / 60f;
            SprayFrames = (int)(5 * frameMul);
        }

        private void EnrichCommand()
        {
            if (Forest != null)
            {
                int width = City.MapData.Width;
                int height = City.MapData.Height;

                byte[] intensities = new byte[width * height];

                foreach (var mod in ForestMod)
                {
                    if (mod.Key.X < 0 || mod.Key.Y < 0 || mod.Key.X >= width || mod.Key.Y >= height) continue;
                    var index = mod.Key.X + mod.Key.Y * width;
                    // bitmap.Set(mod.Key.X, mod.Key.Y);
                    intensities[index] = (byte)Math.Clamp(Math.Round(mod.Value), 0, MaxIntensity);
                }

                Forest.Intensities = intensities;
            }
        }

        private void Submit()
        {
            if (Forest != null)
            {
                HIT.HITVM.Get().PlaySoundEvent(UISounds.BuildDragToolUp);
                Forest.Trim();

                Painter.Commit(AnySet);

                Forest = null;
            }
        }

        private void NewPaint()
        {
            Submit();
            Spray.NewSeed();

            var valueAsByte = MemoryMarshal.Cast<T, byte>(ModifierToValue);

            Forest = new CityEditForest()
            {
                Erasing = Painter.Erasing,
                ForestType = valueAsByte[Painter.SelectedModifier],
                Bitmap = new CityEditBitmap(City.MapData.Width, City.MapData.Height)
            };

            AnySet = false;
            ForestMod = [];
        }

        private void ApplyBrush(Point newPt)
        {
            var frameMul = 60f / FSOEnvironment.RefreshRate;
            var spray = Painter.SprayBrush;
            var multiplier = Painter.Accelerate ? 2f : 1f;
            var intensity = Painter.BrushIntensity * multiplier;

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
                    Forest.Bitmap.Set(targetX, targetY);

                    var loc = new Point(targetX, targetY);

                    if (spray)
                    {
                        var sprayIntensity = Spray.GetSpraypaint(targetY * 512 + targetX, strength);

                        ForestMod.TryGetValue(loc, out float acc);
                        acc += sprayIntensity * frameMul * intensity * 0.25f;
                        ForestMod[loc] = acc;
                    }
                    else
                    {
                        ForestMod[loc] = intensity;
                    }

                    AnySet = true;
                }
            });

            if (!spray || --SprayFrames <= 0)
            {
                EnrichCommand();
                Painter.UpdateTemp();
                ResetSprayFrames();
            }
        }

        public void TileHover(Vector2? tile)
        {
            if (Forest != null && tile != null)
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
            if (Forest != null && Forest.Erasing != Painter.Erasing)
            {
                Submit();
            }
        }
    }
}
