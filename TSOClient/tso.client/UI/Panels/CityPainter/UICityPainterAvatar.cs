using FSO.Client.Rendering.City;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Common;
using FSO.Common.DataService.Model;
using FSO.Common.Rendering.Framework.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.Client.UI.Panels.CityPainter
{
    internal class UICityPainterAvatarLayer : UIContainer
    {
        private readonly Terrain City;
        private readonly Dictionary<uint, UICityPainterAvatar> AvatarById = [];

        public UICityPainterAvatarLayer(Terrain city)
        {
            City = city;
        }

        public void RegisterModification(CityModification mod)
        {
            if (!AvatarById.TryGetValue(mod.AvatarId, out var avatar))
            {
                avatar = new UICityPainterAvatar(City, mod.AvatarId, City.Content.PainterCursor, City.Content.PainterCursorActive);
                AvatarById[mod.AvatarId] = avatar;
                Add(avatar);
            }

            avatar.RegisterModification(mod);
        }
    }

    internal class UICityPainterAvatar : UIContainer
    {
        private readonly Terrain City;
        private readonly UIImage Background;
        private readonly UIPersonButton Person;

        private readonly Texture2D BaseTexture;
        private readonly Texture2D ActiveTexture;

        private CityModification LastModification;

        public UICityPainterAvatar(Terrain city, uint avatarId, Texture2D baseTexture, Texture2D activeTexture)
        {
            City = city;
            Background = new UIImage(baseTexture)
            {
                Size = new Vector2(baseTexture.Width / 2, baseTexture.Height / 2),
                Position = new Vector2(baseTexture.Width / -4, baseTexture.Height / -2)
            };

            float personScale = 0.65f;
            Person = new UIPersonButton()
            {
                AvatarId = avatarId,
                FrameSize = UIPersonButtonSize.LARGE,
                ScaleX = personScale,
                ScaleY = personScale
            };

            var personButtonSize = Person.Size;

            Person.Position = new Vector2(Background.Position.X + Background.Size.X / 2, Background.Position.Y + Background.Size.X / 2) - personScale * personButtonSize / 2;

            Add(Background);
            Add(Person);
            Person.SetButtonVisible(false);
        }

        public override void Update(UpdateState state)
        {
            if (LastModification.Timer < CityModification.EdgeDuration)
            {
                var tex = (Person.ButtonFrame == 1 || Person.ButtonFrame == 2) ? ActiveTexture : BaseTexture;

                if (tex != Background.Texture)
                {
                    Background.Texture = tex;
                }

                var bmp = LastModification.Bitmap;
                var pos = new Vector2(bmp.X + bmp.Width / 2f, bmp.Y + bmp.Height / 2f);

                var proj = City.transformSpr4(new Vector3(pos.X, City.InterpElevationAt(pos) + 2f, pos.Y));

                Position = new Vector2(proj.X, proj.Y) / FSOEnvironment.DPIScaleFactor;
                Visible = (proj.Z > 0);

                if (Visible)
                {
                    float alpha = LastModification.GetArrowAlpha();

                    Background.Opacity = alpha;
                    Person.Opacity = alpha;
                }
            }
            else if (Visible)
            {
                Visible = false;
            }

            base.Update(state);
        }

        public void RegisterModification(CityModification mod)
        {
            LastModification = mod;
        }
    }
}
