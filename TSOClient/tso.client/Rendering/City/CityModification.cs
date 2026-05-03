using FSO.Common.Domain.Realestate;
using FSO.Content.Model;
using FSO.Server.Protocol.Electron.Model.CityEditCommands;
using Microsoft.Xna.Framework;

namespace FSO.Client.Rendering.City
{
    internal class CityModification(CityEditBitmap bitmap, Color color, uint avatarId)
    {
        public const float FlashDuration = 0.25f;
        public const float VisibleDuration = 1.5f;
        public const float EdgeDuration = 2f;
        public const float FadeTime = 0.5f;
        public const float FillIntensity = 0.25f;
        public const float EdgeIntensity = 0.80f;

        public readonly CityEditBitmap Bitmap = bitmap;
        public readonly Color Color = color;
        public readonly uint AvatarId = avatarId;

        public float Timer;

        public (Color edgeColor, Color fillColor) GetColors()
        {
            float fillAlpha = FillIntensity;
            float edgeAlpha = EdgeIntensity;

            if (Timer < FlashDuration)
            {
                fillAlpha += (1 - fillAlpha) * ((FlashDuration - Timer) / FlashDuration);
                edgeAlpha += (1 - edgeAlpha) * ((FlashDuration - Timer) / FlashDuration);
            }

            if (Timer > VisibleDuration - FadeTime)
            {
                fillAlpha *= (1 - (VisibleDuration - Timer) / FadeTime);
            }

            if (Timer > EdgeDuration - FadeTime)
            {
                edgeAlpha *= (1 - (EdgeDuration - Timer) / FadeTime);
            }

            return (Color * edgeAlpha, Color * fillAlpha);
        }

        public static CityModification FromBitmap(CityEditBitmap bmp, Color color, uint avatarId)
        {
            if (bmp == null) return null;

            return new CityModification(bmp, color, avatarId);
        }

        public static CityModification FromBounds(Rectangle? rectOpt, Color color, uint avatarId)
        {
            if (rectOpt == null) return null;

            var rect = rectOpt.Value;

            if (rect.IsEmpty) return null;

            var bmp = new CityEditBitmap(rect.X, rect.Y, rect.Width, rect.Height);

            bmp.Set(0, 0, rect.Width * rect.Height);

            return new CityModification(bmp, color, avatarId);
        }

        public static CityModification FromCommand(CityMap map, CityEditBase cmd)
        {
            var color = new Color(cmd.Color);
            var avatarId = cmd.AvatarId;

            if (cmd is CityEditAltitude alt)
            {
                return FromBitmap(alt.Bitmap, color, avatarId);
            }
            else if (cmd is CityEditPaint paint)
            {
                return FromBitmap(paint.Bitmap, color, avatarId);
            }
            else if (cmd is CityEditForest forest)
            {
                return FromBitmap(forest.Bitmap, color, avatarId);
            }
            else
            {
                return FromBounds(CityMapUtils.GetBounds(map, cmd), color, avatarId);
            }
        }
    }
}
