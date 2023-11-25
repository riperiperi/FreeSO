using FSO.Client.Controllers;
using FSO.Client.Controllers.Panels;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Client.Utils;
using FSO.Common;
using FSO.Common.DataService.Model;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.Client.UI.Panels.Neighborhoods
{
    public class UIFullRatingItem : UIContainer, IUIAbstractRating
    {
        public UITextEdit Body;
        public UILabel NameLabel;
        public UIRatingDisplay Rating;
        public UILabel StarLabel;
        public Binding<MayorRating> CurrentRating { get; set; }
        public UIButton DeleteButton;

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

            var ui = Content.Content.Get().CustomUI;
            var btnTex = ui.Get("chat_cat.png").Get(GameFacade.GraphicsDevice);

            var btnCaption = TextStyle.DefaultLabel.Clone();
            btnCaption.Size = 8;
            btnCaption.Shadow = true;

            if (GameFacade.EnableMod)
            {
                DeleteButton = new UIButton(btnTex);
                DeleteButton.Caption = "Delete";
                DeleteButton.CaptionStyle = btnCaption;
                DeleteButton.OnButtonClick += DeletePost;
                DeleteButton.Width = 64;
                DeleteButton.X = 135;
                DeleteButton.Y = 4;
                Add(DeleteButton);
            }

            Size = new Vector2(475, 70);
            PxWhite = TextureGenerator.GetPxWhite(GameFacade.GraphicsDevice);
        }

        private void NhoodGameplayBan(uint avatarID)
        {
            var controller = UIScreen.Current.FindController<FSO.Client.Controllers.CoreGameScreenController>();

            UIAlert.Prompt("Neighborhood Gameplay Ban", "Ban this user for how long? (in days, 0 for perma)", true, (result) =>
            {
                if (result == null) return;
                uint dayCount;
                if (!uint.TryParse(result, out dayCount))
                {
                    UIAlert.Alert("Neighborhood Gameplay Ban", "Invalid number of days. Please try again.", true);
                }
                uint untilDate = (dayCount == 0) ? uint.MaxValue : ClientEpoch.Now + dayCount * 60 * 60 * 24;

                UIAlert.Prompt("Neighborhood Gameplay Ban", "What message do you want to leave? (optional)", true, (result2) =>
                {
                    if (result2 == null) return;
                    if (controller != null)
                    {
                        controller.NeighborhoodProtocol.BanUser(avatarID, untilDate, result2, (code) =>
                        {
                            //response
                            if (code == Server.Protocol.Electron.Packets.NhoodResponseCode.SUCCESS)
                            {
                                UIAlert.Alert("Neighborhood Gameplay Ban", "Ban has been submitted. Note that if you ban someone twice your second ban will overwrite the first.", true);
                            }
                        });
                    }
                });
            });
        }

        private void DeletePost(UIElement btn)
        {
            UIAlert.YesNo("", "Are you sure you want to delete this rating?", true, (del) =>
            {
                if (del)
                {
                    var protocol = UIScreen.Current.FindController<CoreGameScreenController>()?.NeighborhoodProtocol;
                    if (protocol != null)
                    {
                        protocol.DeleteRate(RatingID, (code) =>
                        {
                            if (code == Server.Protocol.Electron.Packets.NhoodResponseCode.SUCCESS)
                            {
                                uint avaID = 0;
                                if (uint.TryParse(protocol.LastMessage, out avaID) && avaID != 0)
                                {
                                    var alert = UIAlert.YesNo("", "Rating deleted. Neighborhood ban the poster? (icon at top left of window)", false,
                                        (ban) =>
                                        {
                                            if (ban)
                                            {
                                                NhoodGameplayBan(avaID);
                                            }
                                        });
                                    alert.Add(new UIPersonButton() { AvatarId = avaID, FrameSize = UIPersonButtonSize.SMALL, Position = new Vector2(45, 3) });
                                }
                                else
                                {
                                    UIAlert.Alert("", "Rating deleted.", true);
                                }
                            }
                        });
                    }
                }
            });
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
