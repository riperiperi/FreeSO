using FSO.Client.Controllers.Panels;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.Utils;
using FSO.Common.DataService.Model;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Panels.Neighborhoods
{
    public class UIFullRatingItem : UIContainer, IUIAbstractRating
    {
        public UITextEdit Body;
        public UILabel NameLabel;
        public UIRatingDisplay Rating;
        public UILabel StarLabel;
        public Binding<MayorRating> CurrentRating { get; set; }

        private Texture2D PxWhite;

        public string Message
        {
            get
            {
                return Body.CurrentText;
            }
            set
            {
                Body.CurrentText = GameFacade.Emojis.EmojiToBB(BBCodeParser.SanitizeBB(value));
            }
        }

        public string Name
        {
            get
            {
                return NameLabel.Caption;
            }
            set
            {
                NameLabel.Caption = "- " + value;
            }
        }

        public uint HalfStars
        {
            get
            {
                return (uint)Rating.HalfStars;
            }
            set
            {
                Rating.HalfStars = (int)value;
                StarLabel.Caption = Rating.Tooltip;
            }
        }

        public uint RatingID;

        public UIFullRatingItem(uint ratingID)
        {
            RatingID = ratingID;

            Body = new UITextEdit();
            Body.TextStyle = Body.TextStyle.Clone();
            Body.TextStyle.Size = 8;
            Body.TextStyle.Color = Color.White;
            Body.SetSize(454, 42);
            Body.MaxLines = 3;
            Body.Mode = UITextEditMode.ReadOnly;
            Body.Position = new Vector2(15, 20);
            Body.BBCodeEnabled = true;
            Body.RemoveMouseEvent();
            Add(Body);

            StarLabel = new UILabel();
            StarLabel.Position = new Vector2(75, 4);
            StarLabel.Alignment = TextAlignment.Left | TextAlignment.Top;
            StarLabel.Size = new Vector2(1, 1);
            Add(StarLabel);

            NameLabel = new UILabel();
            NameLabel.Position = new Vector2(465, 4);
            NameLabel.Alignment = TextAlignment.Right | TextAlignment.Top;
            NameLabel.Size = new Vector2(1, 1);
            Add(NameLabel);

            Rating = new UIRatingDisplay(true);
            Rating.Position = new Vector2(9, 7);
            Add(Rating);

            CurrentRating = new Binding<MayorRating>()
               .WithBinding(this, "Message", "MayorRating_Comment")
               .WithBinding(this, "HalfStars", "MayorRating_HalfStars")
               .WithBinding(this, "Name", "MayorRating_FromAvatar", (object id) =>
               {
                   return ((uint)id == 0) ? "Anon" : "unknown";
               });

            Size = new Vector2(475, 70);
            PxWhite = TextureGenerator.GetPxWhite(GameFacade.GraphicsDevice);
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);
            if (Controller == null)
            {
                var cont = ControllerUtils.BindController<RatingSummaryController>(this);
                cont.SetRating(RatingID);
            }
        }

        public override void Draw(UISpriteBatch batch)
        {
            base.Draw(batch);
            DrawLocalTexture(batch, PxWhite, null, new Vector2(37, 68), new Vector2(400, 1), new Color(47, 68, 93));
        }
    }
}
